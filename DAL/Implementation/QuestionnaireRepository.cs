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
            .AsNoTracking()
            .Include(q => q.Questions).ThenInclude(qu => qu.EndingAnswers)
            .Include(q => q.Questions).ThenInclude(qu => qu.ParentQuestion)
            .FirstOrDefaultAsync(q => q.Questions.Any(qu => qu.QuestionId == questionId));
}
