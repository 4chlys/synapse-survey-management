using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class QuestionnaireRepository(DbContext context) : Repository<Questionnaire>(context), IQuestionnaireRepository
{
    public Task<Questionnaire> ReadByIdForUpdateAsync(int questionnaireId)
        => _context.Set<Questionnaire>()
            .FirstOrDefaultAsync(q => q.QuestionnaireId == questionnaireId);

    public Task<Questionnaire> ReadByProjectIdReadOnlyAsync(int projectId)
        => _context.Set<Questionnaire>()
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Project.ProjectId == projectId);

    public Task<Questionnaire> ReadByProjectIdWithQuestionsReadOnlyAsync(int projectId)
        => _context.Set<Questionnaire>()
            .AsNoTracking()
            .Include(q => q.Questions).ThenInclude(qu => qu.BranchRules)
            .FirstOrDefaultAsync(q => q.Project.ProjectId == projectId);

    public Task<Questionnaire> ReadByProjectIdForUpdateAsync(int projectId)
        => _context.Set<Questionnaire>()
            .FirstOrDefaultAsync(q => q.Project.ProjectId == projectId);

    public Task<Questionnaire> ReadByQuestionIdWithQuestionsReadOnlyAsync(int questionId)
        => _context.Set<Questionnaire>()
            .AsNoTracking()
            .Include(q => q.Questions).ThenInclude(qu => qu.BranchRules).ThenInclude(b => b.TargetQuestion)
            .FirstOrDefaultAsync(q => q.Questions.Any(qu => qu.QuestionId == questionId));
}
