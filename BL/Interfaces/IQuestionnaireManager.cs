using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IQuestionnaireManager
{
    Task<Questionnaire> GetQuestionnaireByProjectIdAsync(int projectId, bool includeQuestions = true);
    Task<Question> GetQuestionByIdAsync(int questionId);
    Task<Question> AddQuestionAsync(int projectId, string text, QuestionFormat
        format, bool isMandatory, List<string> answerChoices, int? sectionId = null);
    Task<List<Question>> GetQuestionsForProjectAsync(int projectId);
    Task<bool> UpdateQuestionAsync(int questionId, string text, QuestionFormat
        format, bool isMandatory, List<string> answerChoices);
    Task<bool> DeleteQuestionAsync(int questionId);
    Task<Question> GetFirstQuestionAsync(int projectId);
    Task<Question> GetNextQuestionAsync(int currentQuestionId, int participantId);
    Task<Question> GetPreviousQuestionAsync(int currentQuestionId, int participantId);
    Task<int> GetQuestionCountAsync(int projectId);
    Task<List<int>> ComputeVisiblePathAsync(int projectId, int participantId);
    Task<Question> GetResumeQuestionAsync(int projectId, int participantId);
    void ValidateMandatoryAnswers(Questionnaire questionnaire, IEnumerable<(int QuestionId, string Value)> answers, HashSet<int> visibleQuestionIds = null);
}