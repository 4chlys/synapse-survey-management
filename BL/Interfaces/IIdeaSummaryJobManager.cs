using SynapseEnqueteManagement.BL.AiIdeaSummary;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IIdeaSummaryJobManager
{
    Task<IdeaSummaryJob> EnqueueAsync(int projectId, int platformId, IdeaSummaryExportFormat format, string filterJson, string requestedBy, TimeSpan dedupWindow, CancellationToken ct = default);
    Task<IdeaSummaryJob> GetForProjectAsync(int projectId, Guid jobId, CancellationToken ct = default);
    Task<IdeaSummaryJob> ClaimNextPendingAsync(CancellationToken ct = default);
    Task<List<Idea>> GetIdeasForSummaryAsync(int projectId, SummaryFilterParams filter, CancellationToken ct = default);
    Task MarkCompletedAsync(IdeaSummaryJob job, CancellationToken ct = default);
    Task MarkFailedAsync(IdeaSummaryJob job, string errorMessage, CancellationToken ct = default);
}