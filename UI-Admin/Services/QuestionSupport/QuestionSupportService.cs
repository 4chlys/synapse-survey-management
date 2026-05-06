using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.Admin.Models.QuestionSupport;

namespace SynapseEnqueteManagement.UI.Admin.Services.QuestionSupport;

public sealed class QuestionSupportService(HttpClient httpClient)
{
    public async Task<QuestionSupportChatResponse> ChatAsync(
        int projectId,
        IEnumerable<QuestionSupportMessage> history,
        string userMessage,
        bool isFirstTurn)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/projects/{projectId}/questionsupport/chat");

        request.Content = JsonContent.Create(new
        {
            History     = history,
            UserMessage = userMessage,
            IsFirstTurn = isFirstTurn,
        });

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<QuestionSupportChatResponse>()
               ?? new QuestionSupportChatResponse { AssistantMessage = string.Empty };
    }

    public async Task UpdateModeAsync(int projectId, string mode)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"api/projects/{projectId}/questionsupport/mode");

        request.Content = JsonContent.Create($"\"{mode}\"");
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
