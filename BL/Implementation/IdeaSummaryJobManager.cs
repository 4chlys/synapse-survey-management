using SynapseEnqueteManagement.BL.AiIdeaSummary;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public sealed class IdeaSummaryJobManager(
    IIdeaSummaryJobRepository jobRepo,
    IIdeaRepository ideaRepo) : IIdeaSummaryJobManager
{
    public async Task<IdeaSummaryJob> EnqueueAsync(
        int projectId, int platformId, IdeaSummaryExportFormat format,
        string filterJson, string requestedBy, TimeSpan dedupWindow, CancellationToken ct = default)
    {
        var recent = await jobRepo.FindRecentCompletedAsync(projectId, format, filterJson, dedupWindow, ct);
        if (recent is not null) return recent;

        return await jobRepo.CreateAsync(new IdeaSummaryJob
        {
            ProjectPlatformId = platformId,
            ProjectId         = projectId,
            FilterJson        = filterJson,
            ExportFormat      = format,
            CreatedAt         = DateTime.UtcNow,
            RequestedBy       = requestedBy,
        }, ct);
    }

    public async Task<IdeaSummaryJob> GetForProjectAsync(int projectId, Guid jobId, CancellationToken ct = default)
    {
        var job = await jobRepo.GetByIdAsync(jobId, ct);
        return job?.ProjectId == projectId ? job : null;
    }

    public Task<IdeaSummaryJob> ClaimNextPendingAsync(CancellationToken ct = default)
        => jobRepo.ClaimNextPendingAsync(ct);

    public Task<List<Idea>> GetIdeasForSummaryAsync(int projectId, SummaryFilterParams filter, CancellationToken ct = default)
    {
        ModerationStatus? status = null;
        if (!string.IsNullOrWhiteSpace(filter.Status) &&
            Enum.TryParse<ModerationStatus>(filter.Status, ignoreCase: true, out var s))
            status = s;

        return ideaRepo.ReadForSummaryAsync(projectId, filter.CategoryId, status, filter.LanguageCode, ct);
    }

    public async Task MarkCompletedAsync(IdeaSummaryJob job, CancellationToken ct = default)
    {
        job.Status      = IdeaSummaryJobStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;
        await jobRepo.UpdateAsync(job, ct);
    }

    public async Task MarkFailedAsync(IdeaSummaryJob job, string errorMessage, CancellationToken ct = default)
    {
        job.Status       = IdeaSummaryJobStatus.Failed;
        job.ErrorMessage = errorMessage;
        job.CompletedAt  = DateTime.UtcNow;
        await jobRepo.UpdateAsync(job, ct);
    }
}