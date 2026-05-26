using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IIdeaSummaryJobRepository
{
    Task<IdeaSummaryJob> CreateAsync(IdeaSummaryJob job, CancellationToken ct = default);
    Task<IdeaSummaryJob> GetByIdAsync(Guid jobId, CancellationToken ct = default);
    Task<IdeaSummaryJob> FindRecentCompletedAsync(int projectId, IdeaSummaryExportFormat format, string filterJson, TimeSpan maxAge, CancellationToken ct = default);
    Task<IdeaSummaryJob> ClaimNextPendingAsync(CancellationToken ct = default);
    Task UpdateAsync(IdeaSummaryJob job, CancellationToken ct = default);
}