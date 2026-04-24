using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IQuestionRepository : IRepository<Question>
{
    Task<Question> ReadByIdReadOnlyAsync(int questionId);

    Task<Question> ReadByIdWithBranchRulesAndQuestionnaireReadOnlyAsync(int questionId);

    Task<Question> ReadByIdWithSectionForUpdateAsync(int questionId);

    Task<IEnumerable<Question>> ReadByProjectIdWithSectionAndBranchRulesReadOnlyAsync(int projectId);
}
