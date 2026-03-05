using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

public interface IQuestionnaireApiService
{
    Task<SurveyViewModel> GetSurveyAsync();
    Task<string> SubmitSurveyAsync(Dictionary<int, string> answers);
    Task<bool> HasParticipantRespondedAsync(string participantToken);
}
