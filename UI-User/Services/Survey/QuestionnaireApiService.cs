using SynapseEnqueteManagement.UI.User.Models.Survey;

namespace SynapseEnqueteManagement.UI.User.Services.Survey;

public class QuestionnaireApiService : IQuestionnaireApiService
{
    private readonly HttpClient _httpClient;
    private const string TokenHeader = "X-Participant-Token";

    public QuestionnaireApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SurveyViewModel> GetSurveyAsync(int projectId, string languageCode = null)
    {
        var url = languageCode is not null
            ? $"api/projects/{projectId}/questionnaire/questions?languageCode={Uri.EscapeDataString(languageCode)}"
            : $"api/projects/{projectId}/questionnaire/questions";
        var response = await _httpClient.GetAsync(url);

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
            Format = Enum.TryParse<QuestionFormat>(q.QuestionFormat, out var fmt) ? fmt : QuestionFormat.Open,
            AnswerChoices = q.AnswerChoices,
            IsMandatory = q.IsMandatory,
            QuestionNumber = number++,
            TotalQuestions = total,
            Position = q.Position,
            MediaUrl = q.MediaUrl,
            MediaType = q.MediaType.HasValue ? (QuestionMediaType)q.MediaType.Value : null,
            MediaIsEmbed = q.MediaIsEmbed,
            MediaText = q.MediaText
        }).ToList();

        return new SurveyViewModel
        {
            Title = "Survey",
            Questions = viewModels
        };
    }

    public async Task<bool> HasParticipantRespondedAsync(int projectId, string participantToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/projects/{projectId}/participants/me");
        request.Headers.Add(TokenHeader, participantToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return false;

        var result = await response.Content.ReadFromJsonAsync<ParticipantStatusDto>();
        return result?.HasResponded ?? false;
    }

    public async Task<string> SaveResponseAsync(int projectId, int questionId, string value, string participantToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/projects/{projectId}/responses");
        request.Headers.Add(TokenHeader, participantToken);
        request.Content = JsonContent.Create(new { QuestionId = questionId, Value = value });

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"SaveResponse failed: {(int)response.StatusCode} {response.ReasonPhrase} — {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponseDto>();
        return result?.Value;
    }

    public async Task<int?> GetNextQuestionAsync(int projectId, int currentQuestionId, string participantToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"api/projects/{projectId}/questionnaire/next-question?currentQuestionId={currentQuestionId}");
        request.Headers.Add(TokenHeader, participantToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<NavigationResult>();
        return result?.QuestionId;
    }

    public async Task<int?> GetPreviousQuestionAsync(int projectId, int currentQuestionId, string participantToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"api/projects/{projectId}/questionnaire/previous-question?currentQuestionId={currentQuestionId}");
        request.Headers.Add(TokenHeader, participantToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<NavigationResult>();
        return result?.QuestionId;
    }

    public async Task<int?> GetResumeQuestionAsync(int projectId, string participantToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"api/projects/{projectId}/questionnaire/resume-question");
        request.Headers.Add(TokenHeader, participantToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<NavigationResult>();
        return result?.QuestionId;
    }

    public async Task<Dictionary<int, string>> GetResponsesAsync(int projectId, string participantToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/projects/{projectId}/responses");
        request.Headers.Add(TokenHeader, participantToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var results = await response.Content.ReadFromJsonAsync<List<ApiResponseDto>>();
        return results?.ToDictionary(r => r.QuestionId, r => r.Value) ?? new();
    }

    public async Task<List<int>> GetVisiblePathAsync(int projectId, string participantToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"api/projects/{projectId}/questionnaire/visible-path");
        request.Headers.Add(TokenHeader, participantToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<VisiblePathResult>();
        return result?.QuestionIds ?? [];
    }

    public async Task<string> StartSessionAsync(int projectId, string languageCode)
    {
        var response = await _httpClient.PostAsync(
            $"api/projects/{projectId}/participants?languageCode={Uri.EscapeDataString(languageCode)}", null);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiParticipantDto>();
        return result!.ParticipantToken;
    }

    public async Task SubmitAsync(int projectId, string participantToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/projects/{projectId}/submit");
        request.Headers.Add(TokenHeader, participantToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveAllResponsesAsync(int projectId, string participantToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/projects/{projectId}/responses");
        request.Headers.Add(TokenHeader, participantToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private class ApiQuestionDto
    {
        public int QuestionId { get; set; }
        public string Text { get; set; }
        public string QuestionFormat { get; set; }
        public List<string> AnswerChoices { get; set; } = new();
        public int AnswerChoiceCount { get; set; }
        public int? ChoiceIndex { get; set; }
        public bool IsMandatory { get; set; }
        public int Position { get; set; }
        public string MediaUrl { get; set; }
        public byte? MediaType { get; set; }
        public bool MediaIsEmbed { get; set; }
        public string MediaText { get; set; }
    }

    private class ParticipantStatusDto
    {
        public bool HasResponded { get; set; }
    }

    private class ApiParticipantDto
    {
        public string ParticipantToken { get; set; } = string.Empty;
    }

    private class ApiResponseDto
    {
        public int ResponseId { get; set; }
        public string Value { get; set; }
        public int QuestionId { get; set; }
    }

    private class NavigationResult
    {
        public int? QuestionId { get; set; }
    }

    private class VisiblePathResult
    {
        public List<int> QuestionIds { get; set; } = [];
    }
}
