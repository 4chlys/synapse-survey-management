using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SynapseEnqueteManagement.BL.AiIdeaSupport;

public sealed class GoogleVertexAiIdeaSupportService(
    IChatClient chatClient,
    ILogger<GoogleVertexAiIdeaSupportService> logger,
    AiIdeaSupportConfig config)
    : IAiIdeaSupportService
{
    private static readonly Regex CodeFenceRe =
        new(@"^```[a-zA-Z]*\s*|\s*```$",
            RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly string FallbackMessage =
        "Sorry, ik kan je nu even niet helpen. Probeer je idee zelf te verwoorden " +
        "en stuur het in — je kunt het altijd later aanpassen.";

    // ── Chat ─────────────────────────────────────────────────────────────

    public async Task<IdeaSupportChatReply> ChatAsync(
        IdeaSupportChatRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var messages = BuildMessages(request);
            var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
            var raw      = response.Messages.LastOrDefault()?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(raw))
                return FallbackReply();

            return ParseAiReply(raw);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "[IdeaSupport] Chat call failed; returning fallback. Category: {Category}",
                request.CategoryName);

            return FallbackReply();
        }
    }

    // ── Refine ────────────────────────────────────────────────────────────

    public async Task<IdeaSupportRefineResult> RefineAsync(
        string draftText,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(draftText))
            return new IdeaSupportRefineResult { RefinedText = null };

        try
        {
            var prompt   = config.RefinePrompt.Replace("{draft}", draftText,
                               StringComparison.OrdinalIgnoreCase);
            var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };
            var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
            var refined  = response.Messages.LastOrDefault()?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(refined) ||
                refined.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                return new IdeaSupportRefineResult { RefinedText = null };

            return new IdeaSupportRefineResult { RefinedText = refined };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[IdeaSupport] Refine call failed.");
            return new IdeaSupportRefineResult { RefinedText = null };
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the message list sent to the LLM:
    ///   1. System prompt  (persona + JSON output rules)
    ///   2. Context prompt (category primer) — first turn only
    ///   3. Trimmed conversation history
    ///   4. Latest user message — OMITTED when empty (opening turn)
    ///
    /// Omitting the user message on the opening turn is intentional: the
    /// LLM receives only the system + context prompt and responds with the
    /// greeting.  Adding an empty user turn would confuse models that
    /// require strict user/assistant alternation.
    /// </summary>
    private List<ChatMessage> BuildMessages(IdeaSupportChatRequest request)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, config.SystemPrompt)
        };

        if (request.IsFirstTurn && !string.IsNullOrWhiteSpace(request.CategoryName))
        {
            var ctx = config.ContextPrompt
                .Replace("{project_name}",         request.ProjectName,
                    StringComparison.OrdinalIgnoreCase)
                .Replace("{project_introduction}", request.ProjectIntroduction,
                    StringComparison.OrdinalIgnoreCase)
                .Replace("{category_name}",        request.CategoryName,
                    StringComparison.OrdinalIgnoreCase)
                .Replace("{category_description}", request.CategoryDescription,
                    StringComparison.OrdinalIgnoreCase);

            messages.Add(new ChatMessage(ChatRole.System, ctx));
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

        return messages;
    }

    /// <summary>
    /// Parses the structured JSON the system prompt instructs the AI to return:
    /// <c>{"phase":"INTRO","message":"…","draft":null}</c>
    ///
    /// Falls back gracefully when the model returns plain text instead of JSON
    /// (treats the raw text as an IDEATION-phase message so the conversation
    /// continues without crashing).
    /// </summary>
    private static IdeaSupportChatReply ParseAiReply(string raw)
    {
        var json = CodeFenceRe.Replace(raw, string.Empty).Trim();

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
            // Model returned plain text — show it as-is in Ideation phase.
            return new IdeaSupportChatReply
            {
                AssistantMessage = raw,
                Phase            = IdeaSupportPhase.Ideation,
            };
        }
    }

    private static IdeaSupportChatReply FallbackReply() =>
        new() { AssistantMessage = FallbackMessage, Phase = IdeaSupportPhase.Error };
}
