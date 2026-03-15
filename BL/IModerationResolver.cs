using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL;

public interface IModerationResolver
{
    Task<ModerationStatus> ResolveStatusAsync(string text, string token, int projectId);
}
