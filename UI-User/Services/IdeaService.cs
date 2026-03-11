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
    
      
    public bool AddComment(int ideaId, string text)
    {
        return AddCommentAsync(ideaId, text).GetAwaiter().GetResult();
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

    private async Task<bool> AddCommentAsync(int ideaId, string text)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/categories/0/ideas/{ideaId}/comments", 
            new { Text = text }
        );
        return response.IsSuccessStatusCode;
    }

    private class ApiIdeaDto
    {
        public int IdeaId { get; set; }
        public string Description { get; set; }
        public int EmojiCount { get; set; }
        public int CommentsCount { get; set; }
    }
    
    public Idea GetIdeaById(int categoryId, int ideaId)
    {
        return GetIdeaByIdAsync(categoryId, ideaId).GetAwaiter().GetResult();
    }

    public List<Comment> GetCommentsByIdea(int categoryId, int ideaId)
    {
        return GetCommentsByIdeaAsync(categoryId, ideaId).GetAwaiter().GetResult();
    }

    private async Task<Idea> GetIdeaByIdAsync(int categoryId, int ideaId)
    {
        var dto = await _httpClient.GetFromJsonAsync<ApiIdeaDto>(
            $"api/categories/{categoryId}/ideas/{ideaId}");
        if (dto is null) return null;

        return new Idea
        {
            IdeaId = dto.IdeaId,
            Description = dto.Description,
            EmojiCount = dto.EmojiCount,
            CommentsCount = dto.CommentsCount
        };
    }

    private async Task<List<Comment>> GetCommentsByIdeaAsync(int categoryId, int ideaId)
    {
        var dtos = await _httpClient.GetFromJsonAsync<List<ApiCommentDto>>(
            $"api/categories/{categoryId}/ideas/{ideaId}/comments");
        if (dtos is null) return new List<Comment>();

        return dtos.Select(d => new Comment
        {
            CommentId = d.CommentId,
            Text = d.Text
        }).ToList();
    }

    private class ApiCommentDto
    {
        public int CommentId { get; set; }
        public string Text { get; set; }
    }
}