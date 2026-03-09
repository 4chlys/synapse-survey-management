using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

public class IdeaService
{
    private readonly HttpClient _httpClient;

    public IdeaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public List<Idea> GetIdeasByCategory(int categoryId)
    {
        return GetIdeasByCategoryAsync(categoryId).GetAwaiter().GetResult();
    }

    public bool CreateIdea(int categoryId, string description)
    {
        return CreateIdeaAsync(categoryId, description).GetAwaiter().GetResult();
    }

    private async Task<List<Idea>> GetIdeasByCategoryAsync(int categoryId)
    {
        var dtos = await _httpClient.GetFromJsonAsync<List<ApiIdeaDto>>($"api/categories/{categoryId}/ideas");
        if (dtos is null) return new List<Idea>();

        return dtos.Select(d => new Idea
        {
            IdeaId = d.IdeaId,
            Description = d.Description,
            EmojiCount = d.EmojiCount,
            CommentsCount = d.CommentsCount
        }).ToList();
    }

    private async Task<bool> CreateIdeaAsync(int categoryId, string description)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/categories/{categoryId}/ideas", new
        {
            Description = description
        });
        return response.IsSuccessStatusCode;
    }

    private class ApiIdeaDto
    {
        public int IdeaId { get; set; }
        public string Description { get; set; }
        public int EmojiCount { get; set; }
        public int CommentsCount { get; set; }
    }
}