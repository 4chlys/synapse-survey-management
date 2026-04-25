using SynapseEnqueteManagement.UI.User.Models.Ideation;
using SynapseEnqueteManagement.UI.User.Services.Session;

namespace SynapseEnqueteManagement.UI.User.Services.Ideation;

/// <summary>
/// Thin proxy between UI-User and the backend comment-support endpoint.
/// Follows the same pattern as <see cref="IdeaSupportService"/>.
/// </summary>
public sealed class CommentSupportService(HttpClient httpClient, ISessionService sessionService)
{
    private const string TokenHeader = "X-Participant-Token";

    public async Task<CommentSupportChatResponse> ChatAsync(
        int projectId,
        IEnumerable<CommentSupportMessage> history,
        string userMessage,
        string ideaText,
        string categoryName)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/projects/{projectId}/commentsupport/chat");

        request.Content = JsonContent.Create(new
        {
            History      = history,
            UserMessage  = userMessage,
            IdeaText     = ideaText,
            CategoryName = categoryName,
        });

        AddTokenHeader(request, projectId);

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CommentSupportChatResponse>()
               ?? new CommentSupportChatResponse { AssistantMessage = string.Empty };
    }

    private void AddTokenHeader(HttpRequestMessage request, int projectId)
    {
        var token = sessionService.GetToken(projectId);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Add(TokenHeader, token);
    }
}
