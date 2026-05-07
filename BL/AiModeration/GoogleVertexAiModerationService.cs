using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// AI-backed content moderation using Google Vertex AI (Gemini).
/// Regex pre-filtering is delegated to <see cref="IRegexModerationService"/> so this
/// class contains only AI-specific logic.
/// </summary>
public sealed class GoogleVertexAiModerationService(
    IChatClient chatClient,
    ILogger<GoogleVertexAiModerationService> logger,
    ModerationConfig config,
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
        CancellationToken ct = default,
        string contentType = "idea",
        string respondInLanguage = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ModerationResult.Acceptable();

        var regexResult = regex.Check(text, contentType);
        if (!regexResult.IsAcceptable) return regexResult;

        if (!config.UseAi) return ModerationResult.Acceptable();

        try
        {
            return await CallModerationAsync(text, contentType, respondInLanguage, ct);
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

    public async Task<ModerationResult> CheckImageWithAiAsync(byte[] imageBytes, string mimeType, CancellationToken ct = default, string respondInLanguage = null)
    {
        if (!config.UseAi) return ModerationResult.Acceptable();

        try
        {
            var imagePrompt = config.ImageModerationPrompt
                .Replace("{response_language}", respondInLanguage ?? config.DefaultLanguage, StringComparison.OrdinalIgnoreCase);

            var imageContent = new DataContent(new ReadOnlyMemory<byte>(imageBytes), mimeType);
            var message = new ChatMessage(ChatRole.User,
            [
                imageContent,
                new TextContent(imagePrompt),
            ]);

            var response = await chatClient.GetResponseAsync([message], cancellationToken: ct);
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

    public async Task<string> SuggestRewriteAsync(string text, CancellationToken ct = default, string respondInLanguage = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        try
        {
            var rewritePrompt = config.RewritePrompt
                .Replace("{response_language}", respondInLanguage ?? config.DefaultLanguage, StringComparison.OrdinalIgnoreCase);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, rewritePrompt),
                new(ChatRole.User,   text),
            };

            var response   = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
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
        CancellationToken ct)
    {
        var rawPrompt = contentType == "comment"
            ? config.CommentModerationPrompt
            : config.ModerationPrompt;

        var prompt = rawPrompt
            .Replace("{response_language}", respondInLanguage ?? config.DefaultLanguage, StringComparison.OrdinalIgnoreCase);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, prompt),
            new(ChatRole.User,   text),
        };

        var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
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
