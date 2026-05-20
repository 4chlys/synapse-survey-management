using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.BL.Exceptions;

// Carries a stable error code (e.g. "Validation.MinAge.GreaterThanMaxAge") plus formatting args.
// The API ExceptionHandlerMiddleware looks the code up against BL/Resources/SharedResource.*.resx
// using the current request culture. Falls back to the code itself when no resource is found.
// Inherits ValidationException so the existing middleware → 400 mapping still applies.
public class LocalizedValidationException : ValidationException
{
    public string Code { get; }
    public object[] Args { get; }

    public LocalizedValidationException(string code, params object[] args) : base(code)
    {
        Code = code;
        Args = args ?? Array.Empty<object>();
    }
}
