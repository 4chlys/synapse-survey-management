using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;

namespace SynapseEnqueteManagement.BL.AiSupport;

public abstract class ChatClientAiSupportBase<TRequest, TReply>(ILogger logger, IAiUsageManager usageManager)
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

    protected abstract AiServiceKind Service { get; }

    public async Task<TReply> ChatAsync(TRequest request, ResolvedPlatformAiConfig platformConfig, int projectId, CancellationToken ct = default)
    {
        try
        {
            var messages = BuildMessages(request, platformConfig);
            var raw      = await SendSingleAsync(messages, platformConfig.ChatClient, platformConfig.SelectedModelId, projectId, ct);

            if (string.IsNullOrWhiteSpace(raw))
                return FallbackReply(platformConfig);

            return ParseRawReply(raw, request, platformConfig);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[{Domain}] Chat call failed; returning fallback.", GetType().Name);
            return FallbackReply(platformConfig);
        }
    }

    protected abstract List<ChatMessage> BuildMessages(TRequest request, ResolvedPlatformAiConfig platformConfig);
    protected abstract TReply ParseRawReply(string raw, TRequest request, ResolvedPlatformAiConfig platformConfig);
    protected abstract TReply FallbackReply(ResolvedPlatformAiConfig platformConfig);

    protected async Task<string> SendSingleAsync(List<ChatMessage> messages, IChatClient client, string modelId, int projectId, CancellationToken ct)
    {
        var response = await client.GetResponseAsync(messages, cancellationToken: ct);
        try { await usageManager.LogChatAsync(projectId, Service, modelId, response.Usage, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "[{Domain}] Failed to record AI usage.", GetType().Name); }
        return response.Messages.LastOrDefault()?.Text?.Trim() ?? string.Empty;
    }

    protected static string StripCodeFences(string raw) =>
        CodeFenceRe.Replace(raw, string.Empty).Trim();

    protected static string ExtractJson(string cleaned)
    {
        var start = cleaned.IndexOf('{');
        var end   = cleaned.LastIndexOf('}');
        return start >= 0 && end > start ? cleaned[start..(end + 1)] : cleaned;
    }
}