using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class QuestionRepository(DbContext context) : Repository<Question>(context), IQuestionRepository
{
    public Task<Question> ReadByIdReadOnlyAsync(int questionId)
        => _context.Set<Question>()
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.QuestionId == questionId);

    public Task<Question> ReadByIdForUpdateAsync(int questionId)
        => _context.Set<Question>()
            .FirstOrDefaultAsync(q => q.QuestionId == questionId);

    public Task<Question> ReadByIdWithBranchRulesAndQuestionnaireReadOnlyAsync(int questionId)
        => _context.Set<Question>()
            .AsNoTracking()
            .Include(q => q.BranchRules).ThenInclude(b => b.TargetQuestion)
            .Include(q => q.Questionnaire)
            .FirstOrDefaultAsync(q => q.QuestionId == questionId);

    public Task<Question> ReadByIdWithFullQuestionnaireReadOnlyAsync(int questionId)
        => _context.Set<Question>()
            .AsNoTracking()
            .Include(q => q.Questionnaire)
                .ThenInclude(qn => qn.Questions)
                .ThenInclude(qu => qu.BranchRules)
                .ThenInclude(b => b.TargetQuestion)
            .FirstOrDefaultAsync(q => q.QuestionId == questionId);

    public async Task<bool> DeleteByIdAndSaveAsync(int questionId)
    {
        var question = await _context.Set<Question>().FindAsync(questionId);
        if (question is null) return false;
        _context.Set<Question>().Remove(question);
        return true;
    }
}
