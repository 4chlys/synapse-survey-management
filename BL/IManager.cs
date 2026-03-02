using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface IManager
{
    IEnumerable<Question> GetQuestions();
    Response SaveResponse(string value, Question question, Participant participant);
    Participant CreateParticipant(Questionnaire questionnaire);
    Questionnaire GetFirstQuestionnaire();
    IEnumerable<Response> GetAllResponses();
}