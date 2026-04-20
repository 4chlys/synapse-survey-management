using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.User.Models.Ideation;

namespace SynapseEnqueteManagement.UI.User.Services.Ideation;

public class EmojiService(HttpClient httpClient)
{
    public async Task<List<EmojiViewModel>> GetEmojisAsync(int projectId)
    {
        var emojis = await httpClient.GetFromJsonAsync<List<EmojiViewModel>>($"api/projects/{projectId}/emojis");
        return emojis ?? new List<EmojiViewModel>();
    }
}
