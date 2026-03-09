using Microsoft.Extensions.AI;
using Google.Cloud.AIPlatform.V1;
using Google.Cloud.VertexAI.Extensions;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Implements <see cref="IAiModerationService"/> using Google Vertex AI (Gemini)
/// via the <see cref="IChatClient"/> abstraction from Microsoft.Extensions.AI.
///
/// Authentication uses Application Default Credentials (ADC):
///   Local dev:   run  gcloud auth application-default login
///   Production:  Workload Identity / service account — no secrets needed
///
/// Configuration (appsettings.json):
/// "VertexAi": {
///   "ProjectId": "your-gcp-project",
///   "Location":  "europe-west1",
///   "Model":     "gemini-2.5-flash-lite",
///   "Endpoint":  "europe-west1-aiplatform.googleapis.com"
/// }
/// </summary>
public sealed class GoogleVertexAiService(
    IChatClient chatClient,
    ILogger<GoogleVertexAiService> logger)
    : IAiModerationService
{
    private const string SystemPrompt =
        "Je bent een inhoudsmoderatie-assistent voor een schoolwelzijnsplatform in Belgie. " +
        "Beoordeel of de gebruikersinvoer acceptabel is.\n\n" +
        "Afwijzen als de tekst:\n" +
        "- Haatdragende taal, scheldwoorden of discriminatie bevat\n" +
        "- Bedreigingen of oproepen tot geweld bevat\n" +
        "- Zelfbeschading of zelfmoord bespreekt op een directe of gevaarlijke manier\n" +
        "- Persoonlijke gegevens deelt (namen, telefoonnummers, e-mailadressen)\n" +
        "- Duidelijke spam of reclame is\n\n" +
        "Reageer ALLEEN met een geldig JSON-object, geen extra tekst:\n" +
        "{ \"acceptable\": true }\n" +
        "of\n" +
        "{ \"acceptable\": false, \"reason\": \"<korte Nederlandse reden>\" }";
    
    private static readonly Regex CodeFenceRe =
        new(@"^```[a-zA-Z]*\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <inheritdoc/>
    public ModerationResult CheckWithRegex(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ModerationResult.Acceptable();

        var rawResult = RunRegexAndFuzzy(text);
        if (!rawResult.IsAcceptable) return rawResult;

        string normalized = ModerationUtility.NormalizeForModeration(text);
        var normResult = RunRegexAndFuzzy(normalized);
        if (!normResult.IsAcceptable) return normResult;

        string normalizedNoSpace = ModerationUtility.NormalizeForModeration(text, stripWhitespace: true);
        var normNoSpaceResult = RunRegexAndFuzzy(normalizedNoSpace);
        if (!normNoSpaceResult.IsAcceptable) return normNoSpaceResult;

        string collapsed        = ModerationUtility.CollapseRepeats(normalized);
        string collapsedNoSpace = ModerationUtility.CollapseRepeats(normalizedNoSpace);

        var collapsedResult = RunRegexAndFuzzy(collapsed);
        if (!collapsedResult.IsAcceptable) return collapsedResult;

        var collapsedNoSpaceResult = RunRegexAndFuzzy(collapsedNoSpace);
        if (!collapsedNoSpaceResult.IsAcceptable) return collapsedNoSpaceResult;

        return ModerationResult.Acceptable();
    }

    /// <inheritdoc/>
    public async Task<ModerationResult> CheckWithAiAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ModerationResult.Acceptable();

        var regexResult = CheckWithRegex(text);
        if (!regexResult.IsAcceptable)
            return regexResult;

        try
        {
            return await CallAiAsync(text, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Vertex AI moderation call failed; failing open. Snippet: {Snippet}",
                text.Length > 80 ? text[..80] + "…" : text);

            return ModerationResult.Acceptable();
        }
    }

    /// <summary>
    /// Runs compiled regex patterns AND fuzzy word matching against
    /// <paramref name="candidate"/>. Extracted so every normalisation variant
    /// can be checked without code duplication.
    /// </summary>
    private static ModerationResult RunRegexAndFuzzy(string candidate)
    {
        foreach (var (pattern, reason) in RegexModerationPatterns.All)
        {
            if (pattern.IsMatch(candidate))
                return ModerationResult.RejectedByRegex(reason);
        }

        foreach (var (word, reason) in BannedWordList.All)
        {
            if (ModerationUtility.FuzzyContainsBannedWord(candidate, word))
                return ModerationResult.RejectedByRegex(reason);
        }

        return ModerationResult.Acceptable();
    }

    /// <summary>
    /// Sends the text to Gemini and parses the JSON verdict.
    /// Only called after the regex layer has already passed.
    /// </summary>
    private async Task<ModerationResult> CallAiAsync(string text, CancellationToken ct)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User,   text),
        };
        
        var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);

        string rawJson = response.Messages.LastOrDefault()?.Text ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            logger.LogWarning("Vertex AI returned an empty response.");
            return ModerationResult.Acceptable();
        }

        rawJson = CodeFenceRe.Replace(rawJson, string.Empty).Trim();

        ModerationVerdict verdict;
        try
        {
            verdict = JsonSerializer.Deserialize<ModerationVerdict>(rawJson, (JsonSerializerOptions)JsonOpts)
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
        [JsonPropertyName("acceptable")]
        public bool    Acceptable { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason     { get; set; }
    }
}