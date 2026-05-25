namespace SynapseEnqueteManagement.UI.Admin.Services;

/// Thrown by admin service proxies when the API returns 400 with a
/// {"error": "..."} body produced by the BL's LocalizedValidationException.
/// The Message is already localised — controllers should attach it to
/// ModelState / TempData without re-translating.
public sealed class ApiValidationException : Exception
{
    public ApiValidationException(string message) : base(message) { }
}
