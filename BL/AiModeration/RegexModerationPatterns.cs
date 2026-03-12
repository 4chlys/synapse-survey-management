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

    private readonly IReadOnlyList<(Regex Pattern, string Reason)> _patterns;

    public RegexModerationPatterns(AiConfig config, BannedWordList bannedWordList)
    {
        var list = new List<(Regex, string)>();

        foreach (var group in bannedWordList.All.GroupBy(e => e.Reason))
        {
            var alternation = string.Join("|", group.Select(e => Regex.Escape(e.Word)));
            list.Add((new Regex($@"\b({alternation})\b", Opts), group.Key));
        }

        foreach (var entry in config.Moderation.RegexPatterns)
        {
            if (string.IsNullOrWhiteSpace(entry.Pattern)) continue;

            try
            {
                list.Add((new Regex(entry.Pattern, Opts), entry.Reason));
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine(
                    $"[RegexModerationPatterns] Invalid pattern skipped: {entry.Pattern} — {ex.Message}");
            }
        }

        _patterns = list.AsReadOnly();
    }

    /// <summary>Each entry: (compiled pattern, human-readable rejection reason).</summary>
    public IReadOnlyList<(Regex Pattern, string Reason)> All => _patterns;
}