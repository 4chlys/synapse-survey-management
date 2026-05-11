using SynapseEnqueteManagement.BL.Ai;

namespace SynapseEnqueteManagement.BL.AiSupport;

public interface IAiSupportService<TRequest, TReply>
{
    Task<TReply> ChatAsync(TRequest request, ResolvedPlatformAiConfig platformConfig, CancellationToken ct = default);
}
