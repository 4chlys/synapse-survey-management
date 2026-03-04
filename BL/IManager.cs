using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface IManager
{
    Participant AddParticipant(Questionnaire questionnaire);
    IEnumerable<Question> GetQuestions();
    Response AddResponse(string value, Question question, Participant participant);
    Questionnaire GetFirstQuestionnaire();
    IEnumerable<Response> GetResponsesByProjectId(int projectId);
    IEnumerable<Response> GetAllResponses();
    Questionnaire GetQuestionnaireByProjectId(int projectId);
    Question GetQuestionById(int questionId);
    Participant GetParticipantById(int participantId);
    Participant GetParticipantBySessionToken(Guid sessionToken);
    bool HasParticipantResponded(Guid sessionToken);

}