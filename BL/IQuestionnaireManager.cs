using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface IQuestionnaireManager
{
    Task<IEnumerable<Question>> GetQuestionsAsync();
    Task<Questionnaire> GetFirstQuestionnaireAsync();
    Task<Questionnaire> GetQuestionnaireByProjectIdAsync(int projectId);
    Task<Question> GetQuestionByIdAsync(int questionId);
    void ValidateMandatoryAnswers(Questionnaire questionnaire, IEnumerable<(int QuestionId, string Value)> answers);
}
