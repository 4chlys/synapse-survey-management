using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

/// <summary>
/// Implementation of IQuestionnaireService that calls the REST API via HttpClient.
/// Replaces StubQuestionnaireService.
/// </summary>
public class QuestionnaireApiService : IQuestionnaireService
{
    private readonly HttpClient _httpClient;

    public QuestionnaireApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SurveyViewModel> GetSurveyAsync(int projectId)
    {
        var response = await _httpClient.GetAsync($"api/projects/{projectId}/questionnaire/questions");

        if (!response.IsSuccessStatusCode)
            return null;

        var questions = await response.Content.ReadFromJsonAsync<List<ApiQuestionDto>>();

        if (questions is null || questions.Count == 0)
            return null;

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

    public async Task<string> SubmitSurveyAsync(int projectId, Dictionary<int, string> answers)
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
        return participant!.ParticipantToken;
    }

    public async Task<bool> HasParticipantRespondedAsync(int projectId, string participantToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"api/projects/{projectId}/participants/me");
        request.Headers.Add("X-Participant-Token", participantToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return false;

        var result = await response.Content.ReadFromJsonAsync<ParticipantStatusDto>();
        return result?.HasResponded ?? false;
    }

    private class ApiQuestionDto
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string QuestionFormat { get; set; } = string.Empty;
        public List<string> AnswerChoices { get; set; } = new();
    }

    private class ParticipantStatusDto
    {
        public bool HasResponded { get; set; }
    }

    private class ApiParticipantDto
    {
        public string ParticipantToken { get; set; } = string.Empty;
    }
}
