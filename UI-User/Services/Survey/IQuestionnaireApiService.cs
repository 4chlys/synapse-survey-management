using SynapseEnqueteManagement.UI.User.Models.Survey;

namespace SynapseEnqueteManagement.UI.User.Services.Survey;

public interface IQuestionnaireApiService
{
    Task<SurveyViewModel> GetSurveyAsync(int projectId);
    Task<string> SubmitSurveyAsync(int projectId, Dictionary<int, string> answers);
    Task<bool> HasParticipantRespondedAsync(int projectId, string participantToken);
}
