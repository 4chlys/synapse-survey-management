namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Two-layer content moderation with optional rewrite suggestion:
///   1. Fast synchronous regex scan (runs on every keystroke via API).
///   2. Async AI scan via Google Vertex AI (runs on submit + debounced while typing).
///   3. Async AI rewrite suggestion (runs when content is flagged on submit).
/// </summary>
public interface IAiModerationService
{
    /// <summary>
    /// Runs the cheap regex layer only.
    /// Safe to call on every keystroke (no network I/O).
    /// </summary>
    ModerationResult CheckWithRegex(string text, string contentType = "idea");

    /// <summary>
    /// Runs the full AI moderation pipeline:
    ///   - Regex first (fast rejection without spending an API call).
    ///   - Then Vertex AI Gemini if regex passes.
    /// </summary>
    Task<ModerationResult> CheckWithAiAsync(string text, CancellationToken ct = default, string contentType = "idea");

    /// <summary>
    /// When content is flagged, asks the AI to rewrite it constructively.
    /// Returns a cleaned-up version the user can accept or decline.
    /// Returns null if no meaningful rewrite could be produced.
    /// </summary>
    Task<string> SuggestRewriteAsync(string text, CancellationToken ct = default);
}