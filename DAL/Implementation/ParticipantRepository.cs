using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class ParticipantRepository(DbContext context) : Repository<Participant>(context), IParticipantRepository
{
    public Task<Participant> ReadByIdReadOnlyAsync(int participantId)
        => Context.Set<Participant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ParticipantId == participantId);

    public Task<Participant> ReadBySessionTokenWithProjectForUpdateAsync(Guid sessionToken)
        => Context.Set<Participant>()
            .Include(p => p.Project)
                .ThenInclude(p => p.DefaultLanguage)
            .Include(p => p.Language)
            .FirstOrDefaultAsync(p => p.SessionToken == sessionToken);

    public Task<Participant> ReadBySessionTokenReadOnlyAsync(Guid sessionToken)
        => Context.Set<Participant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.SessionToken == sessionToken);

    public Task<Participant> ReadBySessionTokenForUpdateAsync(Guid sessionToken)
        => Context.Set<Participant>()
            .FirstOrDefaultAsync(p => p.SessionToken == sessionToken);

    public Task<int> CountCompletionsByProjectAsync(int projectId)
        => Context.Set<Participant>()
            .AsNoTracking()
            .CountAsync(p => EF.Property<int>(p, "ProjectId") == projectId
                             && p.CompletedAt.HasValue);

    public Task<List<string>> ReadEmailsByProjectIdReadOnlyAsync(int projectId)
        => Context.Set<Participant>()
            .AsNoTracking()
            .Where(p => EF.Property<int>(p, "ProjectId") == projectId
                        && p.Email != null
                        && p.Email != "")
            .Select(p => p.Email)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

    public async Task<IReadOnlyList<ProjectParticipantStats>> ReadStatsByProjectsReadOnlyAsync(IEnumerable<int> projectIds)
    {
        var rows = await Context.Set<Participant>()
            .AsNoTracking()
            .Where(p => projectIds.Contains(EF.Property<int>(p, "ProjectId")))
            .Select(p => new
            {
                ProjectId = EF.Property<int>(p, "ProjectId"),
                p.CreatedAt,
                p.CompletedAt
            })
            .ToListAsync();

        return rows
            .GroupBy(r => r.ProjectId)
            .Select(g =>
            {
                var completed = g
                    .Where(r => r.CompletedAt.HasValue)
                    .Select(r => (r.CompletedAt!.Value - r.CreatedAt).TotalSeconds)
                    .OrderBy(s => s)
                    .ToList();

                return new ProjectParticipantStats(
                    ProjectId: g.Key,
                    StartedCount: g.Count(),
                    CompletedCount: completed.Count,
                    AvgCompletionSeconds: completed.Count > 0 ? completed.Average() : 0,
                    MedianCompletionSeconds: Percentile(completed, 0.5),
                    MinCompletionSeconds: completed.Count > 0 ? completed[0] : 0,
                    Q1CompletionSeconds: Percentile(completed, 0.25),
                    Q3CompletionSeconds: Percentile(completed, 0.75),
                    MaxCompletionSeconds: completed.Count > 0 ? completed[^1] : 0,
                    SortedDurationsSeconds: completed);
            })
            .ToList();
    }

    // Linear interpolation between order statistics (type 7, R's default).
    private static double Percentile(List<double> sorted, double p)
    {
        if (sorted.Count == 0) return 0;
        if (sorted.Count == 1) return sorted[0];
        double pos = p * (sorted.Count - 1);
        int lo = (int)Math.Floor(pos);
        int hi = (int)Math.Ceiling(pos);
        if (lo == hi) return sorted[lo];
        return sorted[lo] + (pos - lo) * (sorted[hi] - sorted[lo]);
    }

    public async Task<List<(DateTime Date, bool IsCompletion)>> ReadTimelineAsync(int projectId, int days)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var participants = await Context.Set<Participant>()
            .AsNoTracking()
            .Where(p => EF.Property<int>(p, "ProjectId") == projectId
                        && p.CreatedAt >= cutoff)
            .Select(p => new { p.CreatedAt, p.CompletedAt })
            .ToListAsync();

        var result = new List<(DateTime Date, bool IsCompletion)>();
        foreach (var p in participants)
        {
            result.Add((p.CreatedAt.Date, false));
            if (p.CompletedAt.HasValue)
                result.Add((p.CompletedAt.Value.Date, true));
        }
        return result;
    }
}
