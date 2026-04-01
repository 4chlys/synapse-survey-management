using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IQuestionnaireManager
{
    Task<Questionnaire> GetQuestionnaireByProjectIdAsync(int projectId, bool includeQuestions = true);
    Task<Question> GetQuestionByIdAsync(int questionId);
    void ValidateMandatoryAnswers(Questionnaire questionnaire, IEnumerable<(int QuestionId, string Value)> answers);
    Task<Question> AddQuestionAsync(int projectId, string text, QuestionFormat
        format, bool isMandatory, List<string> answerChoices);
    Task<bool> UpdateQuestionAsync(int questionId, string text, QuestionFormat
        format, bool isMandatory, List<string> answerChoices);
    Task<bool> DeleteQuestionAsync(int questionId);
}