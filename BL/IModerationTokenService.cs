using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL;

public interface IModerationTokenService
{
    string GenerateToken(string text, ModerationStatus status, int projectId);
    ModerationStatus? ValidateToken(string text, string token, int projectId, TimeSpan? maxAge = null);
}