using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.Admin.Models.Question;

namespace SynapseEnqueteManagement.UI.Admin.Services.Question;

public class QuestionAdminService(HttpClient httpClient)
{
    public async Task<List<QuestionViewModel>> GetQuestionsAsync(int projectId)
    {
        var response = await
            httpClient.GetAsync($"api/projects/{projectId}/questionnaire/questions");
        if (!response.IsSuccessStatusCode)
            return new List<QuestionViewModel>();

        return await
                   response.Content.ReadFromJsonAsync<List<QuestionViewModel>>()
               ?? new List<QuestionViewModel>();
    }

    public async Task CreateQuestionAsync(int projectId, string text, string
        questionFormat, bool isMandatory, List<string> answerChoices)
    {
        var payload = new { Text = text, QuestionFormat = questionFormat,
            IsMandatory = isMandatory, AnswerChoices = answerChoices };
        var response = await
            httpClient.PostAsJsonAsync($"api/projects/{projectId}/questionnaire/questions",
                payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateQuestionAsync(int projectId, int questionId, string
        text, string questionFormat, bool isMandatory, List<string> answerChoices)
    {
        var payload = new { Text = text, QuestionFormat = questionFormat,
            IsMandatory = isMandatory, AnswerChoices = answerChoices };
        var response = await httpClient.PutAsJsonAsync($"api/projects/{projectId}/questionnaire/questions/{questionId}", payload);
response.EnsureSuccessStatusCode();
}

public async Task DeleteQuestionAsync(int projectId, int questionId)
{
    var response = await httpClient.DeleteAsync($"api/projects/{projectId}/questionnaire/questions/{questionId}");
    response.EnsureSuccessStatusCode();
}
}