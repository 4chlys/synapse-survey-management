using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using SynapseEnqueteManagement.BL.AiSupport;

namespace SynapseEnqueteManagement.BL.AiIdeaSupport;

public sealed class AiIdeaSupportService(
    IChatClient chatClient,
    ILogger<AiIdeaSupportService> logger,
    AiIdeaSupportConfig config)
    : ChatClientAiSupportBase<IdeaSupportChatRequest, IdeaSupportChatReply>(chatClient, logger),
      IAiIdeaSupportService
{
    protected override List<ChatMessage> BuildMessages(IdeaSupportChatRequest request)
    {
        var ageRange     = string.IsNullOrWhiteSpace(request.AgeRange) ? config.DefaultAgeRange : request.AgeRange;
        var langName     = GetLanguageName(request.Language);
        var systemPrompt = config.SystemPrompt
            .Replace("{age_range}", ageRange,         StringComparison.OrdinalIgnoreCase)
            .Replace("{language}",  request.Language, StringComparison.OrdinalIgnoreCase);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            // Explicit language instruction using the full name so it is unambiguous
            // regardless of what language the system prompt itself is written in.
            new(ChatRole.System,
                $"You MUST respond in {langName} for every message in this conversation. " +
                "Do not use any other language."),
        };

        if (request.IsFirstTurn && !string.IsNullOrWhiteSpace(request.CategoryName))
        {
            var ctx = config.ContextPrompt
                .Replace("{project_name}",         request.ProjectName,         StringComparison.OrdinalIgnoreCase)
                .Replace("{project_introduction}", request.ProjectIntroduction, StringComparison.OrdinalIgnoreCase)
                .Replace("{category_name}",        request.CategoryName,        StringComparison.OrdinalIgnoreCase)
                .Replace("{category_description}", request.CategoryDescription, StringComparison.OrdinalIgnoreCase)
                .Replace("{language}",             request.Language,            StringComparison.OrdinalIgnoreCase);
            messages.Add(new ChatMessage(ChatRole.System, ctx));
        }
        else if (!string.IsNullOrWhiteSpace(request.CategoryName))
        {
            messages.Add(new ChatMessage(ChatRole.System,
                $"Reminder: the category for this session is '{request.CategoryName}'. " +
                "Always use this exact name — NEVER invent a different category name."));
        }

        var history = request.History.Count > config.MaxHistoryTurns * 2
            ? request.History.Skip(request.History.Count - config.MaxHistoryTurns * 2).ToList()
            : request.History;

        var trimmedHistory = history
            .SkipWhile(m => !m.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var turn in trimmedHistory)
        {
            var role = turn.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
                ? ChatRole.Assistant
                : ChatRole.User;
            messages.Add(new ChatMessage(role, turn.Content));
        }

        if (!string.IsNullOrWhiteSpace(request.UserMessage))
            messages.Add(new ChatMessage(ChatRole.User, request.UserMessage));
        else if (request.IsFirstTurn)
            messages.Add(new ChatMessage(ChatRole.User, "start"));

        return messages;
    }

    protected override IdeaSupportChatReply ParseRawReply(string raw, IdeaSupportChatRequest _)
    {
        var cleaned = StripCodeFences(raw);
        var json    = ExtractJson(cleaned);

        try
        {
            var aiReply = JsonSerializer.Deserialize<IdeaSupportAiReply>(json, JsonOpts);

            if (aiReply is null || string.IsNullOrWhiteSpace(aiReply.Message))
                return FallbackReply();

            var phase = Enum.TryParse<IdeaSupportPhase>(aiReply.Phase, ignoreCase: true, out var p)
                ? p
                : IdeaSupportPhase.Ideation;

            return new IdeaSupportChatReply
            {
                AssistantMessage = aiReply.Message,
                SuggestedDraft   = string.IsNullOrWhiteSpace(aiReply.Draft) ? null : aiReply.Draft,
                Phase            = phase,
            };
        }
        catch (JsonException)
        {
            return new IdeaSupportChatReply { AssistantMessage = raw, Phase = IdeaSupportPhase.Ideation };
        }
    }

    protected override IdeaSupportChatReply FallbackReply() =>
        new() { AssistantMessage = config.FallbackMessage, Phase = IdeaSupportPhase.Error };

    public async Task<IdeaSupportRefineResult> RefineAsync(string draftText, string language, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(draftText))
            return new IdeaSupportRefineResult { RefinedText = null };

        try
        {
            var prompt   = config.RefinePrompt
                .Replace("{draft}",    draftText, StringComparison.OrdinalIgnoreCase)
                .Replace("{language}", language,  StringComparison.OrdinalIgnoreCase);
            var refined  = await SendSingleAsync([new(ChatRole.User, prompt)], ct);

            if (string.IsNullOrWhiteSpace(refined) || refined.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                return new IdeaSupportRefineResult { RefinedText = null };

            return new IdeaSupportRefineResult { RefinedText = refined };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[IdeaSupport] Refine call failed.");
            return new IdeaSupportRefineResult { RefinedText = null };
        }
    }

    private static string GetLanguageName(string langCode)
    {
        try { return System.Globalization.CultureInfo.GetCultureInfo(langCode).EnglishName; }
        catch { return langCode; }
    }
}
