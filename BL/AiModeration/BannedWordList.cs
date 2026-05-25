namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Provides the list of banned words sourced from <see cref="ModerationConfig"/>.
/// </summary>
public sealed class BannedWordList
{
    private readonly IReadOnlyList<(string Word, string Reason)> _entries;

    public BannedWordList(ModerationConfig config)
    {
        _entries = config.BannedWords
            .Where(e => !string.IsNullOrWhiteSpace(e.Word))
            .Select(e => (e.Word.ToLowerInvariant().Trim(), e.Reason))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>(canonicalWord, rejectionReason) pairs — lowercase, single tokens.</summary>
    public IReadOnlyList<(string Word, string Reason)> All => _entries;
}
