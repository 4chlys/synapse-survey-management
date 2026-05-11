using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IModerationResolver
{
    Task<ModerationStatus> ResolveStatusAsync(string text, string token, int projectId, CancellationToken ct = default, string respondInLanguage = null);
}
