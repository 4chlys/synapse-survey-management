namespace SynapseEnqueteManagement.UI.User.Services;

public class CommentService(HttpClient httpClient)
{
    public async Task<bool> AddCommentAsync(
        int categoryId,
        int ideaId,
        string text,
        string moderationStatus = "Unflagged")
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}/comments",
            new { Text = text, ModerationStatus = moderationStatus }
        );
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AddIconCommentAsync(int projectId, int categoryId, int ideaId, int emojiId)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}/iconcomments",
            new { EmojiId = emojiId }
        );
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AddCommentEmojiAsync(int projectId, int categoryId, int ideaId, int commentId, int emojiId)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}/comments/{commentId}/emoji",
            new { EmojiId = emojiId }
        );
        return response.IsSuccessStatusCode;
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
