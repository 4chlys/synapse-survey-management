namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Two-layer content moderation:
///   1. Fast synchronous regex scan (runs on every keystroke via API).
///   2. Async AI scan via Google Vertex AI (runs on submit + debounced while typing).
/// </summary>
public interface IAiModerationService
{
    /// <summary>
    /// Runs the cheap regex layer only.
    /// Safe to call on every keystroke (no network I/O).
    /// </summary>
    ModerationResult CheckWithRegex(string text);

    /// <summary>
    /// Runs the full AI moderation pipeline:
    ///   - Regex first (fast rejection without spending an API call).
    ///   - Then Vertex AI Gemini if regex passes.
    /// Use for final submit validation or the debounced live-check.
    /// </summary>
    Task<ModerationResult> CheckWithAiAsync(string text, CancellationToken ct = default);
}