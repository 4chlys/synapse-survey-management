using SynapseEnqueteManagement.BL.AiModeration;

namespace SynapseEnqueteManagement.BL.InputValidation;

/// <summary>
/// Thin adapter that exposes <see cref="IRegexModerationService"/> through the
/// <see cref="IInputValidationService"/> contract used by the idea-support pipeline.
/// The result type differs (<see cref="InputValidationResult"/> vs
/// <see cref="ModerationResult"/>) but the underlying check is identical.
/// </summary>
public sealed class InputValidationService(IRegexModerationService regex) : IInputValidationService
{
    public InputValidationResult Validate(string text, string contentType = "idea")
    {
        if (string.IsNullOrWhiteSpace(text)) return InputValidationResult.Valid();

        var result = regex.Check(text, contentType);
        return result.IsAcceptable
            ? InputValidationResult.Valid()
            : InputValidationResult.Invalid(result.Reason);
    }
}
