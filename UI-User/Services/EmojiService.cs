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

    public async Task<List<Emoji>> GetEmojisAsync()
    {
        var emojis = await _httpClient.GetFromJsonAsync<List<Emoji>>("api/emojis");
        return emojis ?? new List<Emoji>();
    }

    public async Task<bool> AddIconCommentAsync(int categoryId,int ideaId, int emojiId)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/categories/{categoryId}/ideas/{ideaId}/iconcomments",
            new { EmojiId = emojiId }
        );
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AddCommentEmojiAsync(int categoryId,int ideaId, int commentId, int emojiId)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/categories/{categoryId}/ideas/{ideaId}/comments/{commentId}/emoji",
            new { EmojiId = emojiId }
        );
        return response.IsSuccessStatusCode;
    }
}