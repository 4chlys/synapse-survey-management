using System.Text.Json;
using SynapseEnqueteManagement.UI.User.Services.Session;

namespace SynapseEnqueteManagement.UI.User.Services.Ideation;

public class CommentService(HttpClient httpClient, ISessionService sessionService)
    : AuthenticatedApiService(httpClient, sessionService)
{
    public async Task<bool> AddCommentAsync(
        int projectId,
        int categoryId,
        int ideaId,
        string text,
        string moderationToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}/comments");
        request.Content = JsonContent.Create(new { Text = text, ModerationToken = moderationToken });

        AddTokenHeader(request, projectId);

        var response = await HttpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AddIconCommentAsync(int projectId, int categoryId, int ideaId, int emojiId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}/iconcomments");
        request.Content = JsonContent.Create(new { EmojiId = emojiId });
        AddTokenHeader(request, projectId);
        var response = await HttpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AddCommentEmojiAsync(int projectId, int categoryId, int ideaId, int commentId, int emojiId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}/comments/{commentId}/emoji");
        request.Content = JsonContent.Create(new { EmojiId = emojiId });
        AddTokenHeader(request, projectId);
        var response = await HttpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveIconCommentAsync(int projectId, int categoryId, int ideaId, int emojiId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}/iconcomments");
        request.Content = JsonContent.Create(new { EmojiId = emojiId });
        AddTokenHeader(request, projectId);
        var response = await HttpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveCommentEmojiAsync(int projectId, int categoryId, int ideaId, int commentId,
        int emojiId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}/comments/{commentId}/emoji");
        request.Content = JsonContent.Create(new { EmojiId = emojiId });
        AddTokenHeader(request, projectId);
        var response = await HttpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }
}
