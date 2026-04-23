using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IQuestionnaireManager
{
    Task<Questionnaire> GetQuestionnaireByProjectIdAsync(int projectId, bool includeQuestions = true);
    Task<Question> GetQuestionByIdAsync(int questionId);
    Task<Question> AddQuestionAsync(int projectId, string text, QuestionFormat format,
        bool isMandatory, List<string> answerChoices, int? sectionId = null,
        int? parentQuestionId = null, string parentTriggerValue = null,
        List<string> endingAnswers = null);
    Task<List<Question>> GetQuestionsForProjectAsync(int projectId);
    Task<bool> UpdateQuestionAsync(int questionId, string text, QuestionFormat format,
        bool isMandatory, List<string> answerChoices, int? sectionId = null,
        List<string> endingAnswers = null);
    Task<bool> DeleteQuestionAsync(int questionId);
    Task<Question> MoveQuestionAsync(int questionId, int? newParentQuestionId,
        string newParentTriggerValue, int newPosition);
    Task<Question> GetFirstQuestionAsync(int projectId);
    Task<Question> GetNextQuestionAsync(int currentQuestionId, int participantId);
    Task<Question> GetPreviousQuestionAsync(int currentQuestionId, int participantId);
    Task<int> GetQuestionCountAsync(int projectId);
    Task<List<int>> ComputeVisiblePathAsync(int projectId, int participantId);
    Task<Question> GetResumeQuestionAsync(int projectId, int participantId);
    void ValidateMandatoryAnswers(Questionnaire questionnaire, IEnumerable<(int QuestionId, string Value)> answers, HashSet<int> visibleQuestionIds = null);
}
