using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

public class EmojiService(HttpClient httpClient)
{
    public async Task<List<Emoji>> GetEmojisAsync()
    {
        var emojis = await httpClient.GetFromJsonAsync<List<Emoji>>("api/emojis");
        return emojis ?? new List<Emoji>();
    }
}