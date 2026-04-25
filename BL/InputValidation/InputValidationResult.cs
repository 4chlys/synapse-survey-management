namespace SynapseEnqueteManagement.BL.InputValidation;

public sealed class InputValidationResult
{
    public bool   IsValid { get; init; }
    public string Reason  { get; init; }

    public static InputValidationResult Valid() => new() { IsValid = true };

    public static InputValidationResult Invalid(string reason) =>
        new() { IsValid = false, Reason = reason };
}
