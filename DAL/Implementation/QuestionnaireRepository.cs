using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class QuestionnaireRepository(DbContext context) : Repository<Questionnaire>(context), IQuestionnaireRepository
{
    public Task<Questionnaire> ReadByProjectIdReadOnlyAsync(int projectId)
        => Context.Set<Questionnaire>()
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Project.ProjectId == projectId);

    public Task<Questionnaire> ReadByProjectIdWithQuestionsReadOnlyAsync(int projectId)
        => Context.Set<Questionnaire>()
            .AsNoTracking()
            .Include(q => q.Questions).ThenInclude(qu => qu.EndingAnswers)
            .Include(q => q.Questions).ThenInclude(qu => qu.ParentQuestion)
            .FirstOrDefaultAsync(q => q.Project.ProjectId == projectId);

    public Task<Questionnaire> ReadByProjectIdForUpdateAsync(int projectId)
        => Context.Set<Questionnaire>()
            .FirstOrDefaultAsync(q => q.Project.ProjectId == projectId);

    public Task<Questionnaire> ReadByQuestionIdWithQuestionsReadOnlyAsync(int questionId)
        => Context.Set<Questionnaire>()
            .AsNoTrackingWithIdentityResolution()
            .Include(q => q.Questions).ThenInclude(qu => qu.EndingAnswers)
            .Include(q => q.Questions).ThenInclude(qu => qu.ParentQuestion)
            .FirstOrDefaultAsync(q => q.Questions.Any(qu => qu.QuestionId == questionId));

    public Task<Questionnaire> ReadByProjectIdWithQuestionsAndTranslationsReadOnlyAsync(int projectId)
        => Context.Set<Questionnaire>()
            .AsNoTrackingWithIdentityResolution()
            .Include(q => q.Questions).ThenInclude(qu => qu.Translations).ThenInclude(t => t.Language)
            .Include(q => q.Questions).ThenInclude(qu => qu.EndingAnswers).ThenInclude(ea => ea.Translations).ThenInclude(t => t.Language)
            .Include(q => q.Questions).ThenInclude(qu => qu.ParentQuestion)
            .FirstOrDefaultAsync(q => q.Project.ProjectId == projectId);

    public Task<Questionnaire> ReadByQuestionIdWithQuestionsAndTranslationsReadOnlyAsync(int questionId)
        => Context.Set<Questionnaire>()
            .AsNoTrackingWithIdentityResolution()
            .Include(q => q.Questions).ThenInclude(qu => qu.Translations).ThenInclude(t => t.Language)
            .Include(q => q.Questions).ThenInclude(qu => qu.EndingAnswers).ThenInclude(ea => ea.Translations).ThenInclude(t => t.Language)
            .Include(q => q.Questions).ThenInclude(qu => qu.ParentQuestion)
            .FirstOrDefaultAsync(q => q.Questions.Any(qu => qu.QuestionId == questionId));
}
