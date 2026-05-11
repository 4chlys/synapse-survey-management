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

    public async Task<IEnumerable<Response>> ReadByParticipantWithQuestionTranslationsForUpdateAsync(int participantId)
        => await Context.Set<Response>()
            .Include(r => r.Language)
            .Include(r => r.Question).ThenInclude(q => q.Translations).ThenInclude(t => t.Language)
            .Where(r => r.Participant.ParticipantId == participantId)
            .ToListAsync();

    public async Task<IEnumerable<Response>> ReadByParticipantWithQuestionReadOnlyAsync(int participantId)
        => await Context.Set<Response>()
            .AsNoTracking()
            .Include(r => r.Question)
            .Include(r => r.Language)
            .Where(r => r.Participant.ParticipantId == participantId)
            .ToListAsync();

    public async Task DeleteAllByParticipantIdAsync(int participantId)
    {
        var rows = await Context.Set<Response>()
            .Where(r => r.Participant.ParticipantId == participantId)
            .ToListAsync();
        if (rows.Count == 0) return;
        Context.Set<Response>().RemoveRange(rows);
    }

    public async Task<IEnumerable<Response>> ReadCommittedByProjectWithQuestionReadOnlyAsync(int projectId)
        => await Context.Set<Response>()
            .AsNoTracking()
            .Include(r => r.Question)
            .Include(r => r.Participant)
            .Where(r => r.IsCommitted && r.Question.Questionnaire.Project.ProjectId == projectId)
            .OrderBy(r => r.Question.Position)
            .ThenBy(r => r.ResponseId)
            .ToListAsync();
}
