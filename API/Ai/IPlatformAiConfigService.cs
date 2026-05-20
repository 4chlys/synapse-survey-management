using Microsoft.Extensions.AI;
using SynapseEnqueteManagement.BL.Ai;

namespace SynapseEnqueteManagement.API.Ai;

public interface IPlatformAiConfigService
{
    Task<ResolvedPlatformAiConfig> ResolveAsync(int platformId, CancellationToken ct);
    Task<ResolvedPlatformAiConfig> ResolveGlobalAsync(CancellationToken ct);
    Task<IdeaSummaryAiConfig> ResolveIdeaSummaryAsync(int platformId, CancellationToken ct);
}

public sealed record IdeaSummaryAiConfig(
    IChatClient ChatClient,
    string ModelId,
    string Prompt,
    int MaxIdeas);
