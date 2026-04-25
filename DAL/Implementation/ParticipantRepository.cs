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
}
