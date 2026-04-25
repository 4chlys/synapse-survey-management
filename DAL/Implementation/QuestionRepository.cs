using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class QuestionRepository(DbContext context) : Repository<Question>(context), IQuestionRepository
{
    public Task<Question> ReadByIdReadOnlyAsync(int questionId)
        => Context.Set<Question>()
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.QuestionId == questionId);

    public Task<Question> ReadByIdWithEndingAnswersAndQuestionnaireReadOnlyAsync(int questionId)
        => Context.Set<Question>()
            .AsNoTracking()
            .Include(q => q.EndingAnswers)
            .Include(q => q.Questionnaire)
            .FirstOrDefaultAsync(q => q.QuestionId == questionId);

    public Task<Question> ReadByIdWithFullQuestionnaireReadOnlyAsync(int questionId)
        => Context.Set<Question>()
            .AsNoTrackingWithIdentityResolution()
            .Include(q => q.Questionnaire)
                .ThenInclude(qn => qn.Questions)
                .ThenInclude(qu => qu.EndingAnswers)
            .Include(q => q.Questionnaire)
                .ThenInclude(qn => qn.Questions)
                .ThenInclude(qu => qu.ParentQuestion)
            .FirstOrDefaultAsync(q => q.QuestionId == questionId);

    public Task<Question> ReadByIdWithSectionForUpdateAsync(int questionId)
        => Context.Set<Question>()
            .Include(q => q.Section)
            .Include(q => q.EndingAnswers)
            .FirstOrDefaultAsync(q => q.QuestionId == questionId);

    public async Task<Question> ReadByIdWithParentChainForUpdateAsync(int questionId)
    {
        var question = await Context.Set<Question>()
            .Include(q => q.Questionnaire)
            .Include(q => q.ParentQuestion)
            .FirstOrDefaultAsync(q => q.QuestionId == questionId);
        if (question is null) return null;

        // Eagerly load ancestors one level at a time — depth is tiny in practice
        // and this avoids a fixed ThenInclude chain.
        var cursor = question.ParentQuestion;
        while (cursor is not null)
        {
            await Context.Entry(cursor).Reference(q => q.ParentQuestion).LoadAsync();
            cursor = cursor.ParentQuestion;
        }

        return question;
    }

    public async Task<IEnumerable<Question>> ReadByProjectIdWithSectionAndEndingAnswersReadOnlyAsync(int projectId)
        => await Context.Set<Question>()
            .AsNoTracking()
            .Include(q => q.Section)
            .Include(q => q.EndingAnswers)
            .Include(q => q.ParentQuestion)
            .Where(q => q.Questionnaire.Project.ProjectId == projectId)
            .OrderBy(q => q.Position)
            .ToListAsync();

    public Task<bool> ExistsForProjectAsync(int questionId, int projectId)
        => Context.Set<Question>()
            .AsNoTracking()
            .AnyAsync(q => q.QuestionId == questionId
                           && q.Questionnaire.Project.ProjectId == projectId);

    public async Task<IEnumerable<Question>> ReadSiblingsForUpdateAsync(int? parentQuestionId, string parentTriggerValue, int questionnaireId)
    {
        var query = Context.Set<Question>()
            .Where(q => q.Questionnaire.QuestionnaireId == questionnaireId);

        query = parentQuestionId.HasValue
            ? query.Where(q => q.ParentQuestion.QuestionId == parentQuestionId.Value
                               && q.ParentTriggerValue == parentTriggerValue)
            : query.Where(q => q.ParentQuestion == null);

        return await query.OrderBy(q => q.Position).ToListAsync();
    }
}
