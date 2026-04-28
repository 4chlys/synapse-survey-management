using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SynapseEnqueteManagement.BL.AiCommentSupport;

public sealed class GoogleVertexAiCommentSupportService(
    IChatClient chatClient,
    ILogger<GoogleVertexAiCommentSupportService> logger,
    AiCommentSupportConfig config)
    : IAiCommentSupportService
{
    private static readonly Regex CodeFenceRe =
        new(@"^```[a-zA-Z]*\s*|\s*```$",
            RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<CommentSupportReply> ChatAsync(
        CommentSupportRequest request,
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
                "[CommentSupport] Chat call failed; returning fallback. Category: {Category}",
                request.CategoryName);

            return FallbackReply();
        }
    }

    private List<ChatMessage> BuildMessages(CommentSupportRequest request)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, config.SystemPrompt)
        };

        if (request.IsFirstTurn)
        {
            var ctx = config.ContextPrompt
                .Replace("{idea_text}",     request.IdeaText,     StringComparison.OrdinalIgnoreCase)
                .Replace("{category_name}", request.CategoryName, StringComparison.OrdinalIgnoreCase);

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

    private CommentSupportReply ParseAiReply(string raw)
    {
        var json = CodeFenceRe.Replace(raw, string.Empty).Trim();

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
            return new CommentSupportReply
            {
                AssistantMessage = raw,
                IsFinal          = false,
            };
        }
    }

    private CommentSupportReply FallbackReply() =>
        new() { AssistantMessage = config.FallbackMessage, IsFinal = true };
}
