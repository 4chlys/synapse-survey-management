using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Implements <see cref="IAiModerationService"/> using Google Vertex AI (Gemini)
/// via the <see cref="IChatClient"/> abstraction from Microsoft.Extensions.AI.
///
/// The <see cref="IChatClient"/> is created in Program.cs using
/// Google.Cloud.VertexAI.Extensions and registered via
/// builder.Services.AddChatClient(...). This service simply injects it.
///
/// Authentication uses Application Default Credentials (ADC):
///   Local dev:   run  gcloud auth application-default login
///   Production:  Workload Identity / service account - no secrets needed
///
/// Configuration (appsettings.json):
/// "VertexAi": {
///   "ProjectId": "your-gcp-project",
///   "Location":  "europe-west1",
///   "Model":     "gemini-2.5-flash-lite",
///   "Endpoint":  "europe-west1-aiplatform.googleapis.com"
/// }
/// </summary>
public sealed class GoogleVertexAiService : IAiModerationService
{
    public sealed class Options
    {
        public const string Section = "VertexAi";

        public string ProjectId { get; set; } = "";
        public string Location  { get; set; } = "europe-west1";
        public string Model     { get; set; } = "gemini-2.5-flash-lite";

        /// <summary>
        /// Vertex AI REST endpoint hostname.
        /// Override in appsettings if your GCP project is in a different region.
        /// </summary>
        public string Endpoint  { get; set; } = "europe-west1-aiplatform.googleapis.com";
    }


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

    private readonly IChatClient                    _chatClient;
    private readonly ILogger<GoogleVertexAiService> _log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    };

    public GoogleVertexAiService(
        IChatClient                    chatClient,
        ILogger<GoogleVertexAiService> logger)
    {
        _chatClient = chatClient;
        _log        = logger;
    }

    /// <inheritdoc/>
    public ModerationResult CheckWithRegex(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ModerationResult.Acceptable();

        foreach (var (pattern, reason) in RegexModerationPatterns.All)
        {
            if (pattern.IsMatch(text))
                return ModerationResult.RejectedByRegex(reason);
        }
        
        string normalized        = ModerationUtility.NormalizeForModeration(text);
        string normalizedNoSpace = ModerationUtility.NormalizeForModeration(text, stripWhitespace: true);
        string collapsed         = ModerationUtility.CollapseRepeats(normalized);
        string collapsedNoSpace  = ModerationUtility.CollapseRepeats(normalizedNoSpace);

        foreach (var (pattern, reason) in RegexModerationPatterns.All)
        {
            if (pattern.IsMatch(collapsed) || pattern.IsMatch(collapsedNoSpace))
                return ModerationResult.RejectedByRegex(reason);
        }
        
        foreach (var (word, reason) in BannedWordList.All)
        {
            if (ModerationUtility.FuzzyContainsBannedWord(collapsed, word) ||
                ModerationUtility.FuzzyContainsBannedWord(collapsedNoSpace, word))
            {
                return ModerationResult.RejectedByRegex(reason);
            }
        }

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
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Vertex AI moderation call failed; failing open for snippet: {Snippet}",
                text.Length > 80 ? text[..80] + "..." : text);
            return ModerationResult.Acceptable();
        }
    }

    private async Task<ModerationResult> CallAiAsync(string text, CancellationToken ct)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User,   text),
        };

        var response = await _chatClient.CompleteAsync(messages, cancellationToken: ct);

        var rawJson = response.Message.Text;

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            _log.LogWarning("Vertex AI returned an empty response.");
            return ModerationResult.Acceptable();
        }

        rawJson = rawJson.Trim();
        if (rawJson.StartsWith("```"))
            rawJson = string.Join('\n', rawJson.Split('\n').Skip(1));
        if (rawJson.EndsWith("```"))
            rawJson = rawJson[..rawJson.LastIndexOf("```")];
        rawJson = rawJson.Trim();

        var verdict = JsonSerializer.Deserialize<ModerationVerdict>(rawJson, JsonOpts);

        return verdict?.Acceptable == true
            ? ModerationResult.Acceptable()
            : ModerationResult.RejectedByAi(
                string.IsNullOrWhiteSpace(verdict?.Reason)
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