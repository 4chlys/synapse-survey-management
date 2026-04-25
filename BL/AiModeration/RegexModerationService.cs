namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Deterministic content moderation using compiled regex patterns and
/// fuzzy banned-word matching. No AI dependency — safe for every request.
///
/// The check runs against four text variants to catch obfuscation attempts:
///   raw → normalized → normalized+no-spaces → collapsed-repeats variants.
/// </summary>
public sealed class RegexModerationService(
    RegexModerationPatterns patterns,
    BannedWordList bannedWords,
    AiConfig config) : IRegexModerationService
{
    private readonly int _minSkeletonLength = config.Fuzzy.MinSkeletonLength;
    private readonly int _t0 = config.Fuzzy.LevenshteinThresholds.MaxLengthFor0Edits;
    private readonly int _t1 = config.Fuzzy.LevenshteinThresholds.MaxLengthFor1Edit;
    private readonly int _t2 = config.Fuzzy.LevenshteinThresholds.MaxLengthFor2Edits;

    public ModerationResult Check(string text, string contentType = "idea")
    {
        if (string.IsNullOrWhiteSpace(text)) return ModerationResult.Acceptable();

        var raw = RunChecks(text, contentType);
        if (!raw.IsAcceptable) return raw;

        var norm        = ModerationUtility.NormalizeForModeration(text);
        var normNoSpace = ModerationUtility.NormalizeForModeration(text, stripWhitespace: true);
        var collapsed   = ModerationUtility.CollapseRepeats(norm);
        var collapsedNS = ModerationUtility.CollapseRepeats(normNoSpace);

        return RunChecks(norm,        contentType) is { IsAcceptable: false } r1 ? r1
             : RunChecks(normNoSpace, contentType) is { IsAcceptable: false } r2 ? r2
             : RunChecks(collapsed,   contentType) is { IsAcceptable: false } r3 ? r3
             : RunChecks(collapsedNS, contentType);
    }

    private ModerationResult RunChecks(string candidate, string contentType)
    {
        foreach (var (pattern, reason) in patterns.GetPatterns(contentType))
            if (pattern.IsMatch(candidate))
                return ModerationResult.RejectedByRegex(reason);

        foreach (var (word, reason) in bannedWords.All)
            if (ModerationUtility.FuzzyContainsBannedWord(candidate, word, _minSkeletonLength, _t0, _t1, _t2))
                return ModerationResult.RejectedByRegex(reason);

        return ModerationResult.Acceptable();
    }
}
