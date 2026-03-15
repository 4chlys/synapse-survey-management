using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// A local-only moderation service that requires no API keys or external configuration.
/// Both the regex check and the "AI" check run the same regex + fuzzy-match pipeline,
/// so moderation still works in development and CI without Vertex AI credentials.
/// Rewrite suggestions are not supported and always return null.
/// </summary>
public sealed class MockModerationService : IAiModerationService
{
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

    public MockModerationService(AiConfig config, BannedWordList bannedWords, RegexModerationPatterns patterns)
    {
        _config = config;
        _bannedWords = bannedWords;
        _patterns = patterns;

        var f = config.Fuzzy;
        _minSkeletonLength = f.MinSkeletonLength;
        _t0 = f.LevenshteinThresholds.MaxLengthFor0Edits;
        _t1 = f.LevenshteinThresholds.MaxLengthFor1Edit;
        _t2 = f.LevenshteinThresholds.MaxLengthFor2Edits;
    }

    public ModerationResult CheckWithRegex(string text, string contentType = "idea")
    {
        if (string.IsNullOrWhiteSpace(text))
            return ModerationResult.Acceptable();

        var raw = RunRegexAndFuzzy(text, contentType);
        if (!raw.IsAcceptable) return raw;

        string normalized = ModerationUtility.NormalizeForModeration(text);
        var norm = RunRegexAndFuzzy(normalized, contentType);
        if (!norm.IsAcceptable) return norm;

        string normalizedNoSpace = ModerationUtility.NormalizeForModeration(text, stripWhitespace: true);
        var normNoSpace = RunRegexAndFuzzy(normalizedNoSpace, contentType);
        if (!normNoSpace.IsAcceptable) return normNoSpace;

        string collapsed = ModerationUtility.CollapseRepeats(normalized);
        string collapsedNoSpace = ModerationUtility.CollapseRepeats(normalizedNoSpace);

        var collapsedResult = RunRegexAndFuzzy(collapsed, contentType);
        if (!collapsedResult.IsAcceptable) return collapsedResult;

        return RunRegexAndFuzzy(collapsedNoSpace, contentType);
    }

    /// <summary>
    /// Runs the same regex pipeline as <see cref="CheckWithRegex"/>.
    /// No AI call is made; the "FlaggedBy" value will always be "Regex".
    /// </summary>
    public Task<ModerationResult> CheckWithAiAsync(string text, CancellationToken ct = default, string contentType = "idea")
        => Task.FromResult(CheckWithRegex(text, contentType));

    /// <summary>
    /// Rewrite suggestions require an AI model and are not available in mock mode.
    /// Always returns null so the caller falls back to the original text.
    /// </summary>
    public Task<string> SuggestRewriteAsync(string text, CancellationToken ct = default)
        => Task.FromResult<string>(null);

    private ModerationResult RunRegexAndFuzzy(string candidate, string contentType)
    {
        foreach (var (pattern, reason) in _patterns.GetPatterns(contentType))
            if (pattern.IsMatch(candidate))
                return ModerationResult.RejectedByRegex(reason);

        foreach (var (word, reason) in _bannedWords.All)
            if (ModerationUtility.FuzzyContainsBannedWord(
                    candidate, word,
                    _minSkeletonLength, _t0, _t1, _t2))
                return ModerationResult.RejectedByRegex(reason);

        return ModerationResult.Acceptable();
    }
}