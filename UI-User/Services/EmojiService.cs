using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

public class EmojiService
{
    private readonly HttpClient _httpClient;

    public EmojiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public List<Emoji> GetEmojis()
    {
        return GetEmojisAsync().GetAwaiter().GetResult();
    }

    public bool AddIconComment(int ideaId, int emojiId)
    {
        return AddIconCommentAsync(ideaId, emojiId).GetAwaiter().GetResult();
    }
    
    public bool AddCommentEmoji(int ideaId, int commentId, int emojiId)
    {
        return AddCommentEmojiAsync(ideaId, commentId, emojiId).GetAwaiter().GetResult();
    }

    private async Task<List<Emoji>> GetEmojisAsync()
    {
        var emojis = await _httpClient.GetFromJsonAsync<List<Emoji>>("api/emojis");
        return emojis ?? new List<Emoji>();
    }

    private async Task<bool> AddIconCommentAsync(int ideaId, int emojiId)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/categories/0/ideas/{ideaId}/iconcomments",
            new { EmojiId = emojiId }
        );
        return response.IsSuccessStatusCode;
    }
    
    private async Task<bool> AddCommentEmojiAsync(int ideaId, int commentId, int emojiId)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/categories/0/ideas/{ideaId}/comments/{commentId}/emoji",
            new { EmojiId = emojiId }
        );
        return response.IsSuccessStatusCode;
    }
}