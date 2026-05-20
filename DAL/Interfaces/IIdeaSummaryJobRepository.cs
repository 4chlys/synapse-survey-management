using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IIdeaSummaryJobRepository
{
    Task<IdeaSummaryJob> CreateAsync(IdeaSummaryJob job, CancellationToken ct = default);
    Task<IdeaSummaryJob> GetByIdAsync(Guid jobId, CancellationToken ct = default);
    Task<IdeaSummaryJob> ClaimNextPendingAsync(CancellationToken ct = default);
    Task UpdateAsync(IdeaSummaryJob job, CancellationToken ct = default);
}