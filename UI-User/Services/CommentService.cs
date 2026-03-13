namespace SynapseEnqueteManagement.UI.User.Services;

public class CommentService(HttpClient httpClient)
{
    public async Task<bool> AddCommentAsync(int categoryId, int ideaId, string text)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/categories/{categoryId}/ideas/{ideaId}/comments",
            new { Text = text }
        );
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> AddIconCommentAsync(int categoryId,int ideaId, int emojiId)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/categories/{categoryId}/ideas/{ideaId}/iconcomments",
            new { EmojiId = emojiId }
        );
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AddCommentEmojiAsync(int categoryId,int ideaId, int commentId, int emojiId)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/categories/{categoryId}/ideas/{ideaId}/comments/{commentId}/emoji",
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