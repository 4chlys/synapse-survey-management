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
