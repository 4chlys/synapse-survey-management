using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IQuestionnaireRepository : IRepository<Questionnaire>
{
    Task<Questionnaire> ReadByProjectIdReadOnlyAsync(int projectId);

    Task<Questionnaire> ReadByProjectIdWithQuestionsReadOnlyAsync(int projectId);

    Task<Questionnaire> ReadByProjectIdForUpdateAsync(int projectId);

    Task<Questionnaire> ReadByQuestionIdWithQuestionsReadOnlyAsync(int questionId);
}
