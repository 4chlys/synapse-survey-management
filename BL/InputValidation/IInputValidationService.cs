namespace SynapseEnqueteManagement.BL.InputValidation;

/// <summary>
/// Deterministic (non-AI) input guard: runs compiled regex patterns and
/// fuzzy banned-word matching synchronously, with zero network I/O.
///
/// Call this before any AI service to reject obvious violations cheaply.
/// </summary>
public interface IInputValidationService
{
    /// <summary>
    /// Returns <see cref="InputValidationResult.Valid"/> when the text passes
    /// all regex and fuzzy checks, or <see cref="InputValidationResult.Invalid"/>
    /// with a human-readable reason when it does not.
    ///
    /// <paramref name="contentType"/> is either "idea" (default) or "comment";
    /// idea-only patterns are skipped for comments.
    /// </summary>
    InputValidationResult Validate(string text, string contentType = "idea");
}
