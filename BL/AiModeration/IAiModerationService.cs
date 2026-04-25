namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// AI-backed content moderation and rewrite suggestions via Google Vertex AI.
/// Regex pre-filtering is handled separately by <see cref="IRegexModerationService"/>.
/// </summary>
public interface IAiModerationService
{
    /// <summary>
    /// Runs the full AI moderation pipeline. Internally applies regex pre-filtering
    /// so an AI token is only spent when the text passes the cheap deterministic check.
    /// </summary>
    Task<ModerationResult> CheckWithAiAsync(string text, CancellationToken ct = default, string contentType = "idea");

    /// <summary>
    /// When content is flagged, asks the AI to rewrite it constructively.
    /// Returns a cleaned-up version the user can accept or decline.
    /// Returns null if no meaningful rewrite could be produced.
    /// </summary>
    Task<string> SuggestRewriteAsync(string text, CancellationToken ct = default);
}