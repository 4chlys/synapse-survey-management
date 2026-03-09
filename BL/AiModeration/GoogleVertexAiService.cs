using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SynapseEnqueteManagement.BL.AiModeration;

public sealed class GoogleVertexAiService(
    IChatClient chatClient,
    ILogger<GoogleVertexAiService> logger)
    : IAiModerationService
{
    private const string ModerationPrompt =
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

    /// <summary>
    /// Instructs the model to rewrite flagged content constructively while
    /// preserving the original intent. Returns the literal string "NULL" when
    /// no meaningful rewrite is possible (pure spam, abuse, etc.).
    /// </summary>
    private const string RewritePrompt =
        "Je bent een schrijfassistent voor een schoolwelzijnsplatform in Belgie. " +
        "Een leerling heeft een bericht geschreven dat ongepaste taal of toon bevat. " +
        "Herschrijf het bericht zodat het respectvol, constructief en geschikt is voor het platform, " +
        "maar behoud de kern van wat de leerling wil zeggen.\n\n" +
        "Regels:\n" +
        "- Verwijder scheldwoorden, beledigingen en agressieve taal\n" +
        "- Behoud de originele betekenis en intentie zo veel mogelijk\n" +
        "- Schrijf in dezelfde taal als het originele bericht\n" +
        "- Als het bericht absoluut niet herschreven kan worden (bv. puur spam of misbruik), " +
        "  antwoord dan met exact: NULL\n\n" +
        "Geef ALLEEN de herschreven tekst terug, geen uitleg, geen aanhalingstekens, geen extra tekst.";

    private static readonly Regex CodeFenceRe =
        new(@"^```[a-zA-Z]*\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    };

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

        return RunRegexAndFuzzy(collapsedNoSpace);
    }

    public async Task<ModerationResult> CheckWithAiAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ModerationResult.Acceptable();

        var regexResult = CheckWithRegex(text);
        if (!regexResult.IsAcceptable)
            return regexResult;

        try
        {
            return await CallModerationAsync(text, ct);
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

    /// <inheritdoc/>
    public async Task<string> SuggestRewriteAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, RewritePrompt),
                new(ChatRole.User,   text),
            };

            var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
            var suggestion = response.Messages.LastOrDefault()?.Text.Trim();

            if (string.IsNullOrWhiteSpace(suggestion) ||
                suggestion.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return suggestion;
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

    private async Task<ModerationResult> CallModerationAsync(string text, CancellationToken ct)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, ModerationPrompt),
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
        [JsonPropertyName("acceptable")]
        public bool    Acceptable { get; set; }

        [JsonPropertyName("reason")]
        public string Reason     { get; set; }
    }
}