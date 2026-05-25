namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IShortFormService
{
    string Normalize(string source);
    Task<string> EnsureUniqueAsync(string candidate, int? excludeProjectId = null);
    bool IsReserved(string candidate);
    bool IsValid(string candidate);
}
