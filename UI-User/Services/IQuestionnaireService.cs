using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

public interface IQuestionnaireService
{
    SurveyViewModel GetSurvey();
    string SubmitSurvey(Dictionary<int, string> answers);
    bool HasParticipantResponded(string participantToken);
}
