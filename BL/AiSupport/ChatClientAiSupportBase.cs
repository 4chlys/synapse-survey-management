using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SynapseEnqueteManagement.BL.Ai;

namespace SynapseEnqueteManagement.BL.AiSupport;

public abstract class ChatClientAiSupportBase<TRequest, TReply>(ILogger logger)
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

    public async Task<TReply> ChatAsync(TRequest request, ResolvedPlatformAiConfig platformConfig, CancellationToken ct = default)
    {
        try
        {
            var messages = BuildMessages(request, platformConfig);
            var raw      = await SendSingleAsync(messages, platformConfig.ChatClient, ct);

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
            logger.LogWarning(ex, "[{Domain}] Chat call failed; returning fallback.", GetType().Name);
            return FallbackReply();
        }
    }

    protected abstract List<ChatMessage> BuildMessages(TRequest request, ResolvedPlatformAiConfig platformConfig);
    protected abstract TReply ParseRawReply(string raw, TRequest request);
    protected abstract TReply FallbackReply();

    protected static async Task<string> SendSingleAsync(List<ChatMessage> messages, IChatClient client, CancellationToken ct)
    {
        var response = await client.GetResponseAsync(messages, cancellationToken: ct);
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
