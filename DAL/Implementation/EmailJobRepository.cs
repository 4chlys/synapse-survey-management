using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.BL.Domain.Platform.Enums;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class EmailJobRepository(SynapseEnqueteContext context) : IEmailJobRepository
{
    public async Task<EmailJob> CreateAsync(
        EmailJob job,
        int? projectPlatformId,
        int? projectId,
        int? ideaId,
        CancellationToken ct = default)
    {
        var entry = context.EmailJobs.Add(job);
        // Push FK values into the shadow columns EF created for the nav
        // properties — keeps the entity itself FK-id free.
        if (projectPlatformId.HasValue) entry.Property("ProjectPlatformId").CurrentValue = projectPlatformId.Value;
        if (projectId.HasValue)         entry.Property("ProjectId").CurrentValue         = projectId.Value;
        if (ideaId.HasValue)            entry.Property("IdeaId").CurrentValue            = ideaId.Value;
        await context.SaveChangesAsync(ct);
        return job;
    }

    public Task<EmailJob> GetByIdAsync(int emailJobId, CancellationToken ct = default)
        => context.EmailJobs.FirstOrDefaultAsync(j => j.EmailJobId == emailJobId, ct);

    public async Task<EmailJob> ClaimNextDueAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var job = await context.EmailJobs
            .Where(j => j.Status == EmailJobStatus.Pending && j.ScheduledAt <= now)
            .OrderBy(j => j.ScheduledAt)
            .FirstOrDefaultAsync(ct);

        if (job is null) return null;

        job.Status    = EmailJobStatus.Processing;
        job.StartedAt = now;

        try
        {
            await context.SaveChangesAsync(ct);
            return job;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another worker won the race; let them have it.
            context.Entry(job).State = EntityState.Detached;
            return null;
        }
    }

    public async Task UpdateAsync(EmailJob job, CancellationToken ct = default)
    {
        context.EmailJobs.Update(job);
        await context.SaveChangesAsync(ct);
    }

    public Task<List<EmailJob>> ListByStatusReadOnlyAsync(
        EmailJobStatus status, int? projectPlatformId, int take, CancellationToken ct = default)
        => context.EmailJobs.AsNoTracking()
            .Where(j => j.Status == status
                     && (projectPlatformId == null
                         || EF.Property<int?>(j, "ProjectPlatformId") == projectPlatformId))
            .OrderByDescending(j => j.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public Task<List<EmailJob>> ListRecentSentReadOnlyAsync(
        int? projectPlatformId, int take, CancellationToken ct = default)
        => context.EmailJobs.AsNoTracking()
            .Where(j => j.Status == EmailJobStatus.Sent
                     && (projectPlatformId == null
                         || EF.Property<int?>(j, "ProjectPlatformId") == projectPlatformId))
            .OrderByDescending(j => j.SentAt)
            .Take(take)
            .ToListAsync(ct);
}
