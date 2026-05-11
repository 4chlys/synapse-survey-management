using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.AiSupport;

namespace SynapseEnqueteManagement.BL.AiCommentSupport;

public sealed class AiCommentSupportService(
    ILogger<AiCommentSupportService> logger)
    : ChatClientAiSupportBase<CommentSupportRequest, CommentSupportReply>(logger),
      IAiCommentSupportService
{
    protected override List<ChatMessage> BuildMessages(CommentSupportRequest request, ResolvedPlatformAiConfig platformConfig)
    {
        var config       = platformConfig.CommentSupport;
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

        if (request.IsFirstTurn)
        {
            var ctx = config.ContextPrompt
                .Replace("{idea_text}",     request.IdeaText,     StringComparison.OrdinalIgnoreCase)
                .Replace("{category_name}", request.CategoryName, StringComparison.OrdinalIgnoreCase)
                .Replace("{language}",      request.Language,     StringComparison.OrdinalIgnoreCase);
            messages.Add(new ChatMessage(ChatRole.System, ctx));
        }

        foreach (var turn in request.History)
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

    protected override CommentSupportReply ParseRawReply(string raw, CommentSupportRequest _)
    {
        var cleaned = StripCodeFences(raw);
        var json    = ExtractJson(cleaned);

        try
        {
            var aiReply = JsonSerializer.Deserialize<CommentSupportAiReply>(json, JsonOpts);

            if (aiReply is null || string.IsNullOrWhiteSpace(aiReply.Message))
                return FallbackReply();

            return new CommentSupportReply
            {
                AssistantMessage = aiReply.Message,
                Suggestion       = string.IsNullOrWhiteSpace(aiReply.Suggestion) ? null : aiReply.Suggestion,
                IsFinal          = aiReply.IsFinal,
            };
        }
        catch (JsonException)
        {
            return new CommentSupportReply { AssistantMessage = raw, IsFinal = false };
        }
    }

    protected override CommentSupportReply FallbackReply() =>
        new() { AssistantMessage = "Er ging iets mis. Probeer het opnieuw.", IsFinal = true };

    private static string GetLanguageName(string langCode)
    {
        try { return System.Globalization.CultureInfo.GetCultureInfo(langCode).EnglishName; }
        catch { return langCode; }
    }
}