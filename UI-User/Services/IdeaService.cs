using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

public class IdeaService(HttpClient httpClient)
{
    public async Task<List<IdeaViewModel>> GetIdeasByCategoryAsync(int projectId, int categoryId)
    {
        var dtos = await httpClient.GetFromJsonAsync<List<ApiIdeaDto>>($"api/projects/{projectId}/categories/{categoryId}/ideas");
        if (dtos is null) return new List<IdeaViewModel>();
        return dtos.Select(d => new IdeaViewModel
        {
            IdeaId = d.IdeaId,
            Description = d.Description,
            CommentsCount = d.CommentsCount,
            Emojis = d.Emojis.Select(e => new IdeaEmojiViewModel
            {
                EmojiId = e.EmojiId,
                Symbol = e.Symbol,
                Count = e.Count
            }).ToList()
        }).ToList();
    }

    public async Task<bool> CreateIdeaAsync(
        int ProjectId,
        int categoryId,
        string description,
        string moderationStatus = "Unflagged")
    {
        var response = await httpClient.PostAsJsonAsync($"api/projects/{projectId}/categories/{categoryId}/ideas", new
        {
            Description = description,
            ModerationStatus = moderationStatus
        });
        return response.IsSuccessStatusCode;
    }

    public async Task<Idea> GetIdeaByIdAsync(int projectId, int categoryId, int ideaId)
    {
        var dto = await httpClient.GetFromJsonAsync<ApiIdeaDto>(
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}");
        if (dto is null) return null;

        return new IdeaViewModel
        {
            IdeaId = dto.IdeaId,
            Description = dto.Description,
            CommentsCount = dto.CommentsCount,
            Emojis = dto.Emojis.Select(e => new IdeaEmojiViewModel
            {
                EmojiId = e.EmojiId,
                Symbol = e.Symbol,
                Count = e.Count
            }).ToList()
        };
    }

    public async Task<List<CommentViewModel>> GetCommentsByIdeaAsync(int projectId, int categoryId, int ideaId)
    {
        var dtos = await httpClient.GetFromJsonAsync<List<ApiTextCommentDto>>(
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}/comments");
        if (dtos is null) return new List<CommentViewModel>();
        return dtos.Select(d => new CommentViewModel
        {
            CommentId = d.CommentId,
            Text = d.Text,
            Emojis = d.Emojis.Select(e => new CommentEmojiModel
            {
                EmojiId = e.EmojiId,
                Symbol = e.Symbol,
                Count = e.Count
            }).ToList(),
        }).ToList();
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

    private class ApiIdeaDto
    {
        public int IdeaId { get; set; }
        public string Description { get; set; }
        public int CommentsCount { get; set; }
        public List<ApiIconCommentDto> Emojis { get; set; } = new();
    }
}
