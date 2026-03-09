using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

/// <summary>
/// Implementation of IQuestionnaireService that calls the REST API via HttpClient.
/// Replaces StubQuestionnaireService.
/// </summary>
public class ApiQuestionnaireService : IQuestionnaireService
{
    private readonly HttpClient _httpClient;

    public ApiQuestionnaireService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public SurveyViewModel GetSurvey()
    {
        // Synchronous wrapper - consider making IQuestionnaireService async in the future
        return GetSurveyAsync().GetAwaiter().GetResult();
    }

    private async Task<SurveyViewModel> GetSurveyAsync()
    {
        // projectId will come from route/config — hardcoded 1 for now as example
        var projectId = 1;
        var questions = await _httpClient.GetFromJsonAsync<List<ApiQuestionDto>>($"api/projects/{projectId}/questionnaire/questions");

        if (questions is null || questions.Count == 0)
        {
            return new SurveyViewModel
            {
                Title = "Survey",
                Questions = new List<QuestionViewModel>()
            };
        }

        int total = questions.Count;
        int number = 1;

        var viewModels = questions.Select(q => new QuestionViewModel
        {
            QuestionId = q.QuestionId,
            Text = q.Text,
            Format = Enum.Parse<QuestionFormat>(q.QuestionFormat),
            AnswerChoices = q.AnswerChoices,
            QuestionNumber = number++,
            TotalQuestions = total,
            IsMandatory = q.IsMandatory
        }).ToList();

        return new SurveyViewModel
        {
            Title = "Survey",
            Questions = viewModels
        };
    }

    /// <summary>
    /// Internal DTO matching the API's QuestionDto shape.
    /// </summary>
    private class ApiQuestionDto
    {
        public int QuestionId { get; set; }
        public string Text { get; set; }
        public string QuestionFormat { get; set; }
        public List<string> AnswerChoices { get; set; } = new();
        public bool IsMandatory { get; set; }
    }

    public string SubmitSurvey(Dictionary<int, string> answers)
    {
        return SubmitSurveyAsync(answers).GetAwaiter().GetResult();
    }

    private async Task<string> SubmitSurveyAsync(Dictionary<int, string> answers)
    {
        var projectId = 1;

        var payload = new
        {
            Answers = answers
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .Select(kv => new { QuestionId = kv.Key, Value = kv.Value })
        };

        var response = await _httpClient.PostAsJsonAsync($"api/projects/{projectId}/submit", payload);
        response.EnsureSuccessStatusCode();

        var participant = await response.Content.ReadFromJsonAsync<ApiParticipantDto>();
        return participant.ParticipantToken;
    }

    public bool HasParticipantResponded(string participantToken)
    {
        return HasParticipantRespondedAsync(participantToken).GetAwaiter().GetResult();
    }

    private async Task<bool> HasParticipantRespondedAsync(string participantToken)
    {
        var projectId = 1;
        var request = new HttpRequestMessage(HttpMethod.Get, $"api/projects/{projectId}/participants/me");
        request.Headers.Add("X-Participant-Token", participantToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return false;

        var result = await response.Content.ReadFromJsonAsync<ParticipantStatusDto>();
        return result?.HasResponded ?? false;
    }

    private class ParticipantStatusDto
    {
        public bool HasResponded { get; set; }
    }

    private class ApiParticipantDto
    {
        public string ParticipantToken { get; set; }
    }
}
