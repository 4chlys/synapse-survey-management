namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Provides the list of banned words sourced from the loaded <see cref="AiConfig"/>.
///
/// Previously this was a static list of hardcoded strings. It is now a thin adapter
/// so the rest of the moderation pipeline (<see cref="GoogleVertexAiService"/>,
/// <see cref="RegexModerationPatterns"/>) can continue to call the same shape
/// while the data comes from ai-config.json.
/// </summary>
public sealed class BannedWordList
{
    private readonly IReadOnlyList<(string Word, string Reason)> _entries;

    public BannedWordList(AiConfig config)
    {
        _entries = config.Moderation.BannedWords
            .Where(e => !string.IsNullOrWhiteSpace(e.Word))
            .Select(e => (e.Word.ToLowerInvariant().Trim(), e.Reason))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>(canonicalWord, rejectionReason) pairs — lowercase, single tokens.</summary>
    public IReadOnlyList<(string Word, string Reason)> All => _entries;
}