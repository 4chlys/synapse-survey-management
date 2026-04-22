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

    public Task<Question> ReadByIdWithBranchRulesAndQuestionnaireReadOnlyAsync(int questionId)
        => Context.Set<Question>()
            .AsNoTracking()
            .Include(q => q.BranchRules).ThenInclude(b => b.TargetQuestion)
            .Include(q => q.Questionnaire)
            .FirstOrDefaultAsync(q => q.QuestionId == questionId);

    public Task<Question> ReadByIdWithFullQuestionnaireReadOnlyAsync(int questionId)
        => Context.Set<Question>()
            .AsNoTracking()
            .Include(q => q.Questionnaire)
                .ThenInclude(qn => qn.Questions)
                .ThenInclude(qu => qu.BranchRules)
                .ThenInclude(b => b.TargetQuestion)
            .FirstOrDefaultAsync(q => q.QuestionId == questionId);
}
