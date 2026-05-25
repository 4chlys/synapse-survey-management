using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class IdeaSummaryJobRepository(SynapseEnqueteContext context) : IIdeaSummaryJobRepository
{
    public async Task<IdeaSummaryJob> CreateAsync(IdeaSummaryJob job, CancellationToken ct = default)
    {
        context.IdeaSummaryJobs.Add(job);
        await context.SaveChangesAsync(ct);
        return job;
    }

    public Task<IdeaSummaryJob> GetByIdAsync(Guid jobId, CancellationToken ct = default)
        => context.IdeaSummaryJobs.AsNoTracking().FirstOrDefaultAsync(j => j.IdeaSummaryJobId == jobId, ct);

    public Task<IdeaSummaryJob> FindRecentCompletedAsync(
        int projectId, IdeaSummaryExportFormat format, string filterJson, TimeSpan maxAge, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        return context.IdeaSummaryJobs.AsNoTracking()
            .Where(j => j.ProjectId        == projectId
                     && j.ExportFormat     == format
                     && j.FilterJson       == filterJson
                     && j.Status           == IdeaSummaryJobStatus.Completed
                     && j.CompletedAt      >= cutoff)
            .OrderByDescending(j => j.CompletedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IdeaSummaryJob> ClaimNextPendingAsync(CancellationToken ct = default)
    {
        // Use a tracked query so we can update status atomically
        var job = await context.IdeaSummaryJobs
            .Where(j => j.Status == IdeaSummaryJobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (job is null) return null;

        job.Status = IdeaSummaryJobStatus.Processing;
        job.StartedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return job;
    }

    public async Task UpdateAsync(IdeaSummaryJob job, CancellationToken ct = default)
    {
        context.IdeaSummaryJobs.Update(job);
        await context.SaveChangesAsync(ct);
    }
}