using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IQuestionRepository : IRepository<Question>
{
    Task<Question> ReadByIdReadOnlyAsync(int questionId);
    Task<Question> ReadByIdWithTranslationsForUpdateAsync(int questionId);
    Task<Question> ReadByIdWithEndingAnswersAndQuestionnaireReadOnlyAsync(int questionId);
    Task<Question> ReadByIdWithSectionForUpdateAsync(int questionId);
    Task<Question> ReadByIdWithParentChainForUpdateAsync(int questionId);
    Task<IEnumerable<Question>> ReadByProjectIdWithSectionAndEndingAnswersReadOnlyAsync(int projectId);
    Task<IEnumerable<Question>> ReadByProjectIdWithTranslationsReadOnlyAsync(int projectId);
    Task<IEnumerable<Question>> ReadSiblingsForUpdateAsync(int? parentQuestionId, int? choiceIndex, int questionnaireId);
    Task<bool> ExistsForProjectAsync(int questionId, int projectId);
}
