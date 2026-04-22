using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class ParticipantRepository(DbContext context) : Repository<Participant>(context), IParticipantRepository
{
    public Task<Participant> ReadByIdReadOnlyAsync(int participantId)
        => _context.Set<Participant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ParticipantId == participantId);

    public Task<Participant> ReadByIdForUpdateAsync(int participantId)
        => _context.Set<Participant>()
            .FirstOrDefaultAsync(p => p.ParticipantId == participantId);

    public Task<Participant> ReadBySessionTokenWithProjectForUpdateAsync(Guid sessionToken)
        => _context.Set<Participant>()
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.SessionToken == sessionToken);

    public Task<Participant> ReadBySessionTokenReadOnlyAsync(Guid sessionToken)
        => _context.Set<Participant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.SessionToken == sessionToken);

    public Task<Participant> ReadBySessionTokenForUpdateAsync(Guid sessionToken)
        => _context.Set<Participant>()
            .FirstOrDefaultAsync(p => p.SessionToken == sessionToken);
}
