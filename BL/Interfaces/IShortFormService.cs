namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IShortFormService
{
    string Normalize(string source);
    Task<string> EnsureUniqueAsync(string candidate, int? excludeProjectId = null);
    bool IsReserved(string candidate);
    bool IsValid(string candidate);

    // End-to-end resolution of the shortForm a caller wants: pick `desired` if given else
    // normalise from `defaultName`, then validate + reservedness-check, then ensure unique.
    // Centralises the policy Create/Update/Duplicate previously each implemented.
    Task<string> ResolveAsync(string desired, string defaultName, int? excludeProjectId = null);
}
