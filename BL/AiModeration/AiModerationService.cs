using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SynapseEnqueteManagement.BL.Ai;

namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// AI-backed content moderation using <see cref="IChatClient"/>.
/// Regex pre-filtering is delegated to <see cref="IRegexModerationService"/> so this
/// class contains only AI-specific logic.
/// </summary>
public sealed class AiModerationService(
    ILogger<AiModerationService> logger,
    IRegexModerationService regex) : IAiModerationService
{
    private static readonly Regex CodeFenceRe =
        new(@"^```[a-zA-Z]*\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<ModerationResult> CheckWithAiAsync(
        string text,
        ResolvedPlatformAiConfig platformConfig,
        CancellationToken ct = default,
        string contentType = "idea",
        string respondInLanguage = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ModerationResult.Acceptable();

        var regexResult = regex.Check(text, contentType);
        if (!regexResult.IsAcceptable) return regexResult;

        if (!platformConfig.Moderation.UseAi) return ModerationResult.Acceptable();

        try
        {
            return await CallModerationAsync(text, contentType, respondInLanguage, platformConfig, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Vertex AI moderation call failed; failing closed. Snippet: {Snippet}",
                text.Length > 80 ? text[..80] + "…" : text);
            return ModerationResult.ServiceUnavailable();
        }
    }

    public async Task<ModerationResult> CheckImageWithAiAsync(byte[] imageBytes, string mimeType, ResolvedPlatformAiConfig platformConfig, CancellationToken ct = default, string respondInLanguage = null)
    {
        if (!platformConfig.Moderation.UseAi) return ModerationResult.Acceptable();

        try
        {
            var imagePrompt = platformConfig.Moderation.ImageModerationPrompt
                .Replace("{response_language}", respondInLanguage ?? platformConfig.Moderation.DefaultLanguage, StringComparison.OrdinalIgnoreCase);

            var imageContent = new DataContent(new ReadOnlyMemory<byte>(imageBytes), mimeType);
            var message = new ChatMessage(ChatRole.User,
            [
                imageContent,
                new TextContent(imagePrompt),
            ]);

            var response = await platformConfig.ChatClient.GetResponseAsync([message], cancellationToken: ct);
            var rawJson  = response.Messages.LastOrDefault()?.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                logger.LogWarning("[ImageModeration] Vertex AI returned empty response; failing closed.");
                return ModerationResult.RejectedByAi("De afbeelding kon niet worden gecontroleerd, probeer het later opnieuw.");
            }

            rawJson = CodeFenceRe.Replace(rawJson, string.Empty).Trim();

            ModerationVerdict verdict;
            try
            {
                verdict = JsonSerializer.Deserialize<ModerationVerdict>(rawJson, JsonOpts)
                          ?? throw new JsonException("Null deserialization");
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "[ImageModeration] Non-JSON response; failing closed. Raw: {Raw}",
                    rawJson.Length > 200 ? rawJson[..200] : rawJson);
                return ModerationResult.RejectedByAi("De afbeelding kon niet worden gecontroleerd, probeer het later opnieuw.");
            }

            return verdict.Acceptable
                ? ModerationResult.Acceptable()
                : ModerationResult.RejectedByAi(
                    string.IsNullOrWhiteSpace(verdict.Reason)
                        ? "De afbeelding is niet toegestaan op dit platform."
                        : verdict.Reason);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[ImageModeration] Vertex AI call failed; failing closed.");
            return ModerationResult.RejectedByAi("De afbeelding kon niet worden gecontroleerd, probeer het later opnieuw.");
        }
    }

    public async Task<string> SuggestRewriteAsync(string text, ResolvedPlatformAiConfig platformConfig, CancellationToken ct = default, string respondInLanguage = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        try
        {
            var rewritePrompt = platformConfig.Moderation.RewritePrompt
                .Replace("{response_language}", respondInLanguage ?? platformConfig.Moderation.DefaultLanguage, StringComparison.OrdinalIgnoreCase);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, rewritePrompt),
                new(ChatRole.User,   text),
            };

            var response   = await platformConfig.ChatClient.GetResponseAsync(messages, cancellationToken: ct);
            var suggestion = response.Messages.LastOrDefault()?.Text.Trim();

            return string.IsNullOrWhiteSpace(suggestion)
                   || suggestion.Equals("NULL", StringComparison.OrdinalIgnoreCase)
                ? null
                : suggestion;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Vertex AI rewrite suggestion failed. Snippet: {Snippet}",
                text.Length > 80 ? text[..80] + "…" : text);
            return null;
        }
    }

    private async Task<ModerationResult> CallModerationAsync(
        string text,
        string contentType,
        string respondInLanguage,
        ResolvedPlatformAiConfig platformConfig,
        CancellationToken ct)
    {
        var rawPrompt = contentType == "comment"
            ? platformConfig.Moderation.CommentModerationPrompt
            : platformConfig.Moderation.ModerationPrompt;

        var prompt = rawPrompt
            .Replace("{response_language}", respondInLanguage ?? platformConfig.Moderation.DefaultLanguage, StringComparison.OrdinalIgnoreCase);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, prompt),
            new(ChatRole.User,   text),
        };

        var response = await platformConfig.ChatClient.GetResponseAsync(messages, cancellationToken: ct);
        var rawJson  = response.Messages.LastOrDefault()?.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            logger.LogWarning("Vertex AI returned an empty response.");
            return ModerationResult.Acceptable();
        }

        rawJson = CodeFenceRe.Replace(rawJson, string.Empty).Trim();

        ModerationVerdict verdict;
        try
        {
            verdict = JsonSerializer.Deserialize<ModerationVerdict>(rawJson, JsonOpts)
                      ?? new ModerationVerdict { Acceptable = true };
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Vertex AI returned non-JSON; failing open. Raw: {Raw}",
                rawJson.Length > 200 ? rawJson[..200] : rawJson);
            return ModerationResult.Acceptable();
        }

        return verdict.Acceptable
            ? ModerationResult.Acceptable()
            : ModerationResult.RejectedByAi(
                string.IsNullOrWhiteSpace(verdict.Reason)
                    ? "De inhoud is niet toegestaan op dit platform."
                    : verdict.Reason);
    }

    private sealed class ModerationVerdict
    {
        [JsonPropertyName("acceptable")] public bool   Acceptable { get; set; }
        [JsonPropertyName("reason")]     public string Reason     { get; set; }
    }
}