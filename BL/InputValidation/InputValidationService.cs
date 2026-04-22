using SynapseEnqueteManagement.BL.AiModeration;

namespace SynapseEnqueteManagement.BL.InputValidation;

/// <summary>
/// Pure regex + fuzzy-match validation extracted from the moderation pipeline.
///
/// Depends only on the already-registered singletons <see cref="RegexModerationPatterns"/>
/// and <see cref="BannedWordList"/>; has no AI dependency and is safe to call
/// on every request without token cost.
/// </summary>
public sealed class InputValidationService(
    RegexModerationPatterns patterns,
    BannedWordList bannedWords,
    AiConfig config) : IInputValidationService
{
    private readonly int _minSkeletonLength = config.Fuzzy.MinSkeletonLength;
    private readonly int _t0 = config.Fuzzy.LevenshteinThresholds.MaxLengthFor0Edits;
    private readonly int _t1 = config.Fuzzy.LevenshteinThresholds.MaxLengthFor1Edit;
    private readonly int _t2 = config.Fuzzy.LevenshteinThresholds.MaxLengthFor2Edits;

    public InputValidationResult Validate(string text, string contentType = "idea")
    {
        if (string.IsNullOrWhiteSpace(text)) return InputValidationResult.Valid();

        // Run all normalisation variants — same strategy as the moderation service.
        var candidates = new[]
        {
            text,
            ModerationUtility.NormalizeForModeration(text),
            ModerationUtility.NormalizeForModeration(text, stripWhitespace: true),
        };

        foreach (var candidate in candidates)
        {
            var collapsed = ModerationUtility.CollapseRepeats(candidate);

            foreach (var toCheck in new[] { candidate, collapsed })
            {
                var result = RunChecks(toCheck, contentType);
                if (!result.IsValid) return result;
            }
        }

        return InputValidationResult.Valid();
    }

    private InputValidationResult RunChecks(string candidate, string contentType)
    {
        foreach (var (pattern, reason) in patterns.GetPatterns(contentType))
            if (pattern.IsMatch(candidate))
                return InputValidationResult.Invalid(reason);

        foreach (var (word, reason) in bannedWords.All)
            if (ModerationUtility.FuzzyContainsBannedWord(
                    candidate, word, _minSkeletonLength, _t0, _t1, _t2))
                return InputValidationResult.Invalid(reason);

        return InputValidationResult.Valid();
    }
}
