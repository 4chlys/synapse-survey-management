namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Deterministic, CPU-only content check using compiled regex patterns and
/// fuzzy banned-word matching. No network I/O; safe to call on every keystroke.
/// </summary>
public interface IRegexModerationService
{
    /// <summary>
    /// Runs the full regex + fuzzy-match pipeline across multiple text
    /// normalization variants (raw, normalized, collapsed).
    /// </summary>
    ModerationResult Check(string text, string contentType = "idea");
}
