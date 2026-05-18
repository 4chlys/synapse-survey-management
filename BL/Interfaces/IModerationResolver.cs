using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IModerationResolver
{
    Task<ModerationStatus> ResolveStatusAsync(string text, string token, int projectId, ResolvedPlatformAiConfig platformConfig, CancellationToken ct = default, string respondInLanguage = null);
}