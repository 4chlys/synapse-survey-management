using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Two-layer content moderation backed by Google Vertex AI (Gemini).
///
/// All prompts, banned words, regex patterns and fuzzy-matching thresholds are
/// loaded from ai-config.json via the injected <see cref="AiConfig"/> singleton —
/// nothing is hardcoded in this class anymore.
/// </summary>
public sealed class GoogleVertexAiService : IAiModerationService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<GoogleVertexAiService> _logger;
    private readonly AiConfig _config;
    private readonly BannedWordList _bannedWords;
    private readonly RegexModerationPatterns _patterns;

    private readonly int _minSkeletonLength;
    private readonly int _t0, _t1, _t2;

    private static readonly Regex CodeFenceRe =
        new(@"^```[a-zA-Z]*\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public GoogleVertexAiService(
        IChatClient chatClient,
        ILogger<GoogleVertexAiService> logger,
        AiConfig config,
        BannedWordList bannedWords,
        RegexModerationPatterns patterns)
    {
        _chatClient = chatClient;
        _logger = logger;
        _config = config;
        _bannedWords = bannedWords;
        _patterns = patterns;

        var f = config.Fuzzy;
        _minSkeletonLength = f.MinSkeletonLength;
        _t0 = f.LevenshteinThresholds.MaxLengthFor0Edits;
        _t1 = f.LevenshteinThresholds.MaxLengthFor1Edit;
        _t2 = f.LevenshteinThresholds.MaxLengthFor2Edits;
    }

    public ModerationResult CheckWithRegex(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ModerationResult.Acceptable();

        var raw = RunRegexAndFuzzy(text);
        if (!raw.IsAcceptable) return raw;

        string norm = ModerationUtility.NormalizeForModeration(text);
        string normNoSpace = ModerationUtility.NormalizeForModeration(text, stripWhitespace: true);
        string collapsed = ModerationUtility.CollapseRepeats(norm);
        string collapsedNS = ModerationUtility.CollapseRepeats(normNoSpace);

        return RunRegexAndFuzzy(norm) is { IsAcceptable: false } r1 ? r1
            : RunRegexAndFuzzy(normNoSpace) is { IsAcceptable: false } r2 ? r2
            : RunRegexAndFuzzy(collapsed) is { IsAcceptable: false } r3 ? r3
            : RunRegexAndFuzzy(collapsedNS);
    }

    public async Task<ModerationResult> CheckWithAiAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ModerationResult.Acceptable();

        var regexResult = CheckWithRegex(text);
        if (!regexResult.IsAcceptable) return regexResult;

        if (!_config.Moderation.UseAi) return ModerationResult.Acceptable();

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
            _logger.LogError(ex,
                "Vertex AI moderation call failed; failing closed. Snippet: {Snippet}",
                text.Length > 80 ? text[..80] + "…" : text);
            return ModerationResult.ServiceUnavailable();
        }
    }

    public async Task<string> SuggestRewriteAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, _config.Moderation.RewritePrompt),
                new(ChatRole.User, text),
            };

            var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);
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
            _logger.LogWarning(ex,
                "Vertex AI rewrite suggestion failed. Snippet: {Snippet}",
                text.Length > 80 ? text[..80] + "…" : text);
            return null;
        }
    }

    private ModerationResult RunRegexAndFuzzy(string candidate)
    {
        foreach (var (pattern, reason) in _patterns.All)
            if (pattern.IsMatch(candidate))
                return ModerationResult.RejectedByRegex(reason);

        foreach (var (word, reason) in _bannedWords.All)
            if (ModerationUtility.FuzzyContainsBannedWord(
                    candidate, word,
                    _minSkeletonLength, _t0, _t1, _t2))
                return ModerationResult.RejectedByRegex(reason);

        return ModerationResult.Acceptable();
    }

    private async Task<ModerationResult> CallModerationAsync(string text, CancellationToken ct)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, _config.Moderation.ModerationPrompt),
            new(ChatRole.User, text),
        };

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);
        string rawJson = response.Messages.LastOrDefault()?.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            _logger.LogWarning("Vertex AI returned an empty response.");
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
            _logger.LogWarning(ex, "Vertex AI returned non-JSON; failing open. Raw: {Raw}",
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
        [JsonPropertyName("acceptable")] public bool Acceptable { get; set; }

        [JsonPropertyName("reason")] public string Reason { get; set; }
    }
}