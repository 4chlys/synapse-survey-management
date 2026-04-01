using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace SynapseEnqueteManagement.BL.AiIdeaSupport;

/// <summary>
/// Idea-support chat backed by Google Vertex AI (Gemini).
///
/// The conversation is reconstructed on every call from the history supplied
/// by the client — no server-side session state is maintained.
///
/// A "suggested draft" extraction pass runs after every assistant reply:
/// if Gemini embeds a ready-to-submit draft between [DRAFT]…[/DRAFT] markers,
/// it is stripped from the visible message and returned separately so the
/// front-end can offer a one-click "use this idea" button.
/// </summary>
public sealed class GoogleVertexAiIdeaSupportService(
    IChatClient chatClient,
    ILogger<GoogleVertexAiIdeaSupportService> logger,
    AiIdeaSupportConfig config)
    : IAiIdeaSupportService
{
    private const string DraftOpenTag = "[DRAFT]";
    private const string DraftCloseTag = "[/DRAFT]";

    private static readonly string FallbackMessage =
        "Sorry, ik kan je nu even niet helpen. Probeer je idee zelf te verwoorden " +
        "en stuur het in — je kunt het altijd later aanpassen.";

    // Chat

    public async Task<IdeaSupportChatReply> ChatAsync(
        IdeaSupportChatRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var messages = BuildMessages(request);
            var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
            var raw = response.Messages.LastOrDefault()?.Text.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(raw))
                return new IdeaSupportChatReply { AssistantMessage = FallbackMessage };

            return ExtractDraft(raw);
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

            return new IdeaSupportChatReply { AssistantMessage = FallbackMessage };
        }
    }

    // Refine

    public async Task<IdeaSupportRefineResult> RefineAsync(
        string draftText,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(draftText))
            return new IdeaSupportRefineResult { RefinedText = null };

        try
        {
            var prompt = config.RefinePrompt.Replace("{draft}", draftText, StringComparison.OrdinalIgnoreCase);
            var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };
            var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
            var refined = response.Messages.LastOrDefault()?.Text.Trim();

            if (string.IsNullOrWhiteSpace(refined) || refined.Equals("NULL", StringComparison.OrdinalIgnoreCase))
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

    // Helpers

    /// <summary>
    /// Builds the full message list that is sent to the model:
    ///   1. System prompt (platform persona).
    ///   2. Optional context message (category + first-turn primer).
    ///   3. Trimmed conversation history.
    ///   4. Latest user message.
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
                .Replace("{category_name}", request.CategoryName, StringComparison.OrdinalIgnoreCase)
                .Replace("{category_description}", request.CategoryDescription, StringComparison.OrdinalIgnoreCase);

            messages.Add(new ChatMessage(ChatRole.System, ctx));
        }

        var trimmedHistory = request.History.Count > config.MaxHistoryTurns * 2
            ? request.History.Skip(request.History.Count - config.MaxHistoryTurns * 2).ToList()
            : request.History;

        foreach (var turn in trimmedHistory)
        {
            var role = turn.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
                ? ChatRole.Assistant
                : ChatRole.User;

            messages.Add(new ChatMessage(role, turn.Content));
        }

        messages.Add(new ChatMessage(ChatRole.User, request.UserMessage));

        return messages;
    }

    /// <summary>
    /// Looks for an optional [DRAFT]…[/DRAFT] block in the model reply.
    /// When found, the block is removed from the visible message and its
    /// contents are returned as <see cref="IdeaSupportChatReply.SuggestedDraft"/>.
    /// </summary>
    private static IdeaSupportChatReply ExtractDraft(string raw)
    {
        var openIdx = raw.IndexOf(DraftOpenTag, StringComparison.OrdinalIgnoreCase);
        var closeIdx = raw.IndexOf(DraftCloseTag, StringComparison.OrdinalIgnoreCase);

        if (openIdx < 0 || closeIdx < 0 || closeIdx <= openIdx)
            return new IdeaSupportChatReply { AssistantMessage = raw };

        var draft = raw.Substring(openIdx + DraftOpenTag.Length, closeIdx - openIdx - DraftOpenTag.Length).Trim();
        var visibleMessage = (raw[..openIdx] + raw[(closeIdx + DraftCloseTag.Length)..]).Trim();

        return new IdeaSupportChatReply
        {
            AssistantMessage = string.IsNullOrWhiteSpace(visibleMessage)
                ? "Hier is een voorstel voor je idee:"
                : visibleMessage,
            SuggestedDraft = string.IsNullOrWhiteSpace(draft) ? null : draft,
        };
    }
}