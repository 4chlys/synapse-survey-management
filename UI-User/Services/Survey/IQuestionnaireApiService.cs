using SynapseEnqueteManagement.UI.User.Models.Survey;

namespace SynapseEnqueteManagement.UI.User.Services.Survey;

public interface IQuestionnaireApiService
{
    Task<SurveyViewModel> GetSurveyAsync(int projectId);
    Task<bool> HasParticipantRespondedAsync(int projectId, string participantToken);
    Task<string> SaveResponseAsync(int projectId, int questionId, string value, string participantToken);
    Task<int?> GetNextQuestionAsync(int projectId, int currentQuestionId, string participantToken);
    Task<int?> GetPreviousQuestionAsync(int projectId, int currentQuestionId, string participantToken);
    Task<int?> GetResumeQuestionAsync(int projectId, string participantToken);
    Task<Dictionary<int, string>> GetResponsesAsync(int projectId, string participantToken);
    Task<string> StartSessionAsync(int projectId);
    Task<List<int>> GetVisiblePathAsync(int projectId, string participantToken);
    Task SubmitAsync(int projectId, string participantToken);
    Task RemoveAllResponsesAsync(int projectId, string participantToken);
}
