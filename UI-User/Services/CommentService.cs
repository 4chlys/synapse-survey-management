using System.Text.Json;

namespace SynapseEnqueteManagement.UI.User.Services;

public class CommentService(HttpClient httpClient, ISessionService sessionService)
{
    private const string TokenHeader = "X-Participant-Token";

    public async Task<bool> AddCommentAsync(
        int projectId,
        int categoryId,
        int ideaId,
        string text)
    {
        var moderationRequest = new HttpRequestMessage(HttpMethod.Post, $"api/projects/{projectId}/moderation");
        moderationRequest.Content = JsonContent.Create(new { Text = text });

        var moderationResponse = await httpClient.SendAsync(moderationRequest);
        moderationResponse.EnsureSuccessStatusCode();

        var json = await moderationResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json);

        bool moderationStatus = result.GetProperty("isAcceptable").GetBoolean();

        if (!moderationStatus) return false;
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}/comments");
        request.Content = JsonContent.Create(new { Text = text, ModerationStatus = moderationStatus });

        AddTokenHeader(request, projectId);

        var response = await httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AddIconCommentAsync(int projectId, int categoryId, int ideaId, int emojiId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}/iconcomments");
        request.Content = JsonContent.Create(new { EmojiId = emojiId });
        AddTokenHeader(request, projectId);
        var response = await httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AddCommentEmojiAsync(int projectId, int categoryId, int ideaId, int commentId, int emojiId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}/comments/{commentId}/emoji");
        request.Content = JsonContent.Create(new { EmojiId = emojiId });
        AddTokenHeader(request, projectId);
        var response = await httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    private void AddTokenHeader(HttpRequestMessage request, int projectId)
    {
        var token = sessionService.GetToken(projectId);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Add(TokenHeader, token);
    }

    private class ApiTextCommentDto
    {
        public int CommentId { get; set; }
        public string Text { get; set; }
        public List<ApiIconCommentDto> Emojis { get; set; } = new();
    }

    private class ApiIconCommentDto
    {
        public int EmojiId { get; set; }
        public string Symbol { get; set; }
        public int Count { get; set; }
    }
}