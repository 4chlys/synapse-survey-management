namespace SynapseEnqueteManagement.BL.AiSupport;

public interface IAiSupportService<TRequest, TReply>
{
    Task<TReply> ChatAsync(TRequest request, int projectId, CancellationToken ct = default);
}
