using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

public interface IQuestionnaireApiService
{
    Task<SurveyViewModel> GetSurveyAsync(int projectId);
    Task<string> SubmitSurveyAsync(int projectId, Dictionary<int, string> answers);
    Task<bool> HasParticipantRespondedAsync(int projectId, string participantToken);
}