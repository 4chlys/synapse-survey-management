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
            TotalQuestions = total
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
    }

    public void SubmitSurvey(Dictionary<int, string> answers)
    {
        SubmitSurveyAsync(answers).GetAwaiter().GetResult();
    }

    private async Task SubmitSurveyAsync(Dictionary<int, string> answers)
    {
        var projectId = 1;

        // 1. Create anonymous participant session
        var participantResponse = await _httpClient.PostAsync($"api/projects/{projectId}/participants", null);
        participantResponse.EnsureSuccessStatusCode();
        var participant = await participantResponse.Content.ReadFromJsonAsync<ApiParticipantDto>();

        // 2. Submit each answer with the participant token
        foreach (var (questionId, value) in answers)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;

            var request = new HttpRequestMessage(HttpMethod.Post, $"api/projects/{projectId}/responses")
            {
                Content = JsonContent.Create(new { QuestionId = questionId, Value = value })
            };
            request.Headers.Add("X-Participant-Token", participant.ParticipantToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }

    private class ApiParticipantDto
    {
        public string ParticipantToken { get; set; }
    }
}
