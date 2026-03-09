using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface IQuestionnaireManager
{
    IEnumerable<Question> GetQuestions();
    Questionnaire GetFirstQuestionnaire();
    Questionnaire GetQuestionnaireByProjectId(int projectId);
    Question GetQuestionById(int questionId);
    void ValidateMandatoryAnswers(Questionnaire questionnaire, IEnumerable<(int QuestionId, string Value)> answers);
}
