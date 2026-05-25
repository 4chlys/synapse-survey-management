using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Builds the compiled regex patterns for the fast (exact) moderation pass.
/// Word-level slur patterns are generated from <see cref="BannedWordList"/>.
/// Structural patterns come from the <c>regex_patterns</c> array in ai-config.json.
/// </summary>
public sealed class RegexModerationPatterns
{
    private static readonly RegexOptions Opts =
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    private readonly IReadOnlyList<(Regex Pattern, string Reason)> _sharedPatterns;
    private readonly IReadOnlyList<(Regex Pattern, string Reason)> _ideaOnlyPatterns;

    public RegexModerationPatterns(
        ModerationConfig config,
        BannedWordList bannedWordList,
        ILogger<RegexModerationPatterns> logger)
    {
        var shared   = new List<(Regex, string)>();
        var ideaOnly = new List<(Regex, string)>();

        foreach (var group in bannedWordList.All.GroupBy(e => e.Reason))
        {
            var alternation = string.Join("|", group.Select(e => Regex.Escape(e.Word)));
            shared.Add((new Regex($@"\b({alternation})\b", Opts), group.Key));
        }

        foreach (var entry in config.RegexPatterns)
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
                logger.LogWarning(ex, "Invalid regex pattern skipped: {Pattern}", entry.Pattern);
            }
        }

        _sharedPatterns   = shared.AsReadOnly();
        _ideaOnlyPatterns = ideaOnly.AsReadOnly();
    }

    public IReadOnlyList<(Regex Pattern, string Reason)> GetPatterns(string contentType) =>
        contentType == "comment"
            ? _sharedPatterns
            : _sharedPatterns.Concat(_ideaOnlyPatterns).ToList().AsReadOnly();
}
