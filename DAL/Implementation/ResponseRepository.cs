using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class ResponseRepository(DbContext context) : Repository<Response>(context), IResponseRepository
{
    public Task<Response> ReadByQuestionAndParticipantReadOnlyAsync(int questionId, int participantId)
        => Context.Set<Response>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Question.QuestionId == questionId
                                       && r.Participant.ParticipantId == participantId);

    public Task<Response> ReadByQuestionAndParticipantForUpdateAsync(int questionId, int participantId)
        => Context.Set<Response>()
            .SingleOrDefaultAsync(r => r.Question.QuestionId == questionId
                                       && r.Participant.ParticipantId == participantId);

    public async Task<IEnumerable<Response>> ReadByParticipantWithQuestionForUpdateAsync(int participantId)
        => await Context.Set<Response>()
            .Include(r => r.Question)
            .Where(r => r.Participant.ParticipantId == participantId)
            .ToListAsync();

    public async Task<IEnumerable<Response>> ReadByParticipantWithQuestionReadOnlyAsync(int participantId)
        => await Context.Set<Response>()
            .AsNoTracking()
            .Include(r => r.Question)
            .Where(r => r.Participant.ParticipantId == participantId)
            .ToListAsync();
}
