using System.Text.RegularExpressions;

namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Builds the compiled regex patterns for the fast (exact) moderation pass.
///
/// Word-level slur patterns are generated from <see cref="BannedWordList"/>.
/// Structural patterns (threats, self-harm, PII, spam, URLs) come from the
/// <c>regex_patterns</c> array in ai-config.json.
///
/// Previously this was a static class with hardcoded patterns.
/// It is now an instance class built from <see cref="AiConfig"/> at startup.
/// </summary>
public sealed class RegexModerationPatterns
{
    private static readonly RegexOptions Opts =
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    private readonly IReadOnlyList<(Regex Pattern, string Reason)> _sharedPatterns;
    private readonly IReadOnlyList<(Regex Pattern, string Reason)> _ideaOnlyPatterns;

    public RegexModerationPatterns(AiConfig config, BannedWordList bannedWordList)
    {
        var shared = new List<(Regex, string)>();
        var ideaOnly = new List<(Regex, string)>();

        // Banned words apply to everything
        foreach (var group in bannedWordList.All.GroupBy(e => e.Reason))
        {
            var alternation = string.Join("|", group.Select(e => Regex.Escape(e.Word)));
            shared.Add((new Regex($@"\b({alternation})\b", Opts), group.Key));
        }

        // Structural patterns from config — tag idea-only ones via a naming convention
        foreach (var entry in config.Moderation.RegexPatterns)
        {
            if (string.IsNullOrWhiteSpace(entry.Pattern)) continue;
            try
            {
                var compiled = new Regex(entry.Pattern, Opts);
                if (entry.IdeaOnly)
                    ideaOnly.Add((compiled, entry.Reason));
                else
                    shared.Add((compiled, entry.Reason));
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine(
                    $"[RegexModerationPatterns] Invalid pattern skipped: {entry.Pattern} — {ex.Message}");
            }
        }

        _sharedPatterns = shared.AsReadOnly();
        _ideaOnlyPatterns = ideaOnly.AsReadOnly();
    }

    public IReadOnlyList<(Regex Pattern, string Reason)> GetPatterns(string contentType) =>
        contentType == "comment"
            ? _sharedPatterns
            : _sharedPatterns.Concat(_ideaOnlyPatterns).ToList().AsReadOnly();
}