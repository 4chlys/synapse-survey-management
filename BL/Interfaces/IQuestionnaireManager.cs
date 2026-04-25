using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IQuestionnaireManager
{
    Task<Questionnaire> GetQuestionnaireByProjectIdWithQuestionsReadOnlyAsync(int projectId);
    Task<Questionnaire> GetQuestionnaireByProjectIdReadOnlyAsync(int projectId);
    Task<Questionnaire> GetQuestionnaireByQuestionIdWithQuestionsReadOnlyAsync(int questionId);
    Task<Question> GetQuestionByIdReadOnlyAsync(int questionId);
    Task<Question> AddQuestionAsync(int projectId, string text, QuestionFormat format,
        bool isMandatory, List<string> answerChoices, int? sectionId = null,
        int? parentQuestionId = null, string parentTriggerValue = null,
        List<string> endingAnswers = null);
    Task<IEnumerable<Question>> GetQuestionsForProjectAsync(int projectId);
    Task<bool> UpdateQuestionAsync(int questionId, string text, QuestionFormat format,
        bool isMandatory, List<string> answerChoices, int? sectionId = null,
        List<string> endingAnswers = null);
    Task<bool> DeleteQuestionAsync(int questionId);
    Task<Question> MoveQuestionAsync(int questionId, int? newParentQuestionId,
        string newParentTriggerValue, int newPosition);
    Task<Question> GetFirstQuestionAsync(int projectId);
    Task<int> GetQuestionCountAsync(int projectId);
    void ValidateMandatoryAnswers(Questionnaire questionnaire, IEnumerable<(int QuestionId, string Value)> answers, HashSet<int> visibleQuestionIds = null);

    Task<IEnumerable<Section>> GetSectionsReadOnlyAsync(int projectId);
    Task<Section> CreateSectionAsync(int projectId, string name, string description);
    Task<bool> UpdateSectionAsync(int sectionId, string name, string description);
    Task<bool> DeleteSectionAsync(int sectionId);
}
