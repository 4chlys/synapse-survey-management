using SynapseEnqueteManagement.BL.Ai;

namespace SynapseEnqueteManagement.API.Ai;

public interface IPlatformAiConfigService
{
    Task<ResolvedPlatformAiConfig> ResolveAsync(int platformId, CancellationToken ct);
    Task<ResolvedPlatformAiConfig> ResolveGlobalAsync(CancellationToken ct);
}
