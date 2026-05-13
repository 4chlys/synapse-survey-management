using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;

namespace SynapseEnqueteManagement.BL.AiSupport;

public abstract class ChatClientAiSupportBase<TRequest, TReply>(
    IChatClient chatClient,
    ILogger logger,
    IAiUsageManager usageManager)
    : IAiSupportService<TRequest, TReply>
{
    protected static readonly Regex CodeFenceRe =
        new(@"^```[a-zA-Z]*\s*|\s*```$",
            RegexOptions.Compiled | RegexOptions.Multiline);

    protected static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Token-usage attribution: identifies which AI feature this service represents.</summary>
    protected abstract AiServiceKind Service { get; }

    /// <summary>The Vertex AI chat model name as it appears in pricing config.</summary>
    protected abstract string Model { get; }

    /// <summary>Maximum tokens the model may generate per response. Override per service as needed.</summary>
    protected virtual int MaxOutputTokens => 1024;

    public async Task<TReply> ChatAsync(TRequest request, int projectId, CancellationToken ct = default)
    {
        try
        {
            var messages = BuildMessages(request);
            var response = await SendSingleAsync(messages, projectId, ct);
            var raw      = ExtractText(response);

            if (string.IsNullOrWhiteSpace(raw))
                return FallbackReply();

            return ParseRawReply(raw, request);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[{Domain}] Chat call failed; returning fallback.",
                GetType().Name);
            return FallbackReply();
        }
    }

    protected abstract List<ChatMessage> BuildMessages(TRequest request);
    protected abstract TReply ParseRawReply(string raw, TRequest request);
    protected abstract TReply FallbackReply();

    /// <summary>
    /// Sends one chat round-trip and records the resulting token usage against
    /// <paramref name="projectId"/>. Logging failures are caught so they cannot
    /// surface as a chat failure.
    /// </summary>
    protected async Task<ChatResponse> SendSingleAsync(List<ChatMessage> messages, int projectId, CancellationToken ct)
    {
        var options  = new ChatOptions { MaxOutputTokens = MaxOutputTokens };
        var response = await chatClient.GetResponseAsync(messages, options, ct);
        try
        {
            await usageManager.LogChatAsync(projectId, Service, Model, response.Usage, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[{Domain}] Failed to record AI usage.", GetType().Name);
        }
        return response;
    }

    protected static string ExtractText(ChatResponse response) =>
        response.Messages.LastOrDefault()?.Text?.Trim() ?? string.Empty;

    protected static string StripCodeFences(string raw) =>
        CodeFenceRe.Replace(raw, string.Empty).Trim();

    protected static string ExtractJson(string cleaned)
    {
        var start = cleaned.IndexOf('{');
        var end   = cleaned.LastIndexOf('}');
        return start >= 0 && end > start ? cleaned[start..(end + 1)] : cleaned;
    }
}
