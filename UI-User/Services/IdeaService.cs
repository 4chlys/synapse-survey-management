using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

public class IdeaService(HttpClient httpClient, ISessionService sessionService)
{
    private const string TokenHeader = "X-Participant-Token";

    public async Task<List<IdeaViewModel>> GetIdeasByCategoryAsync(int projectId, int categoryId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"api/projects/{projectId}/categories/{categoryId}/ideas");
        AddTokenHeader(request, projectId);
        var response = await httpClient.SendAsync(request);
        var dtos = await response.Content.ReadFromJsonAsync<List<ApiIdeaDto>>();
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
                Count = e.Count,
                PostedByCurrentUser = e.PostedByCurrentUser
            }).ToList()
        }).ToList();
    }

    public async Task<bool> CreateIdeaAsync(
        int projectId,
        int categoryId,
        string description,
        string moderationStatus = "Unflagged")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/projects/{projectId}/categories/{categoryId}/ideas");
        request.Content = JsonContent.Create(new { Description = description, ModerationStatus = moderationStatus });
        AddTokenHeader(request, projectId);
        var response = await httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<IdeaViewModel> GetIdeaByIdAsync(int projectId, int categoryId, int ideaId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}");
        AddTokenHeader(request, projectId);
        var response = await httpClient.SendAsync(request);
        var dto = await response.Content.ReadFromJsonAsync<ApiIdeaDto>();
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
                Count = e.Count,
                PostedByCurrentUser = e.PostedByCurrentUser
            }).ToList()
        };
    }

    public async Task<List<CommentViewModel>> GetCommentsByIdeaAsync(int projectId, int categoryId, int ideaId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}/comments");
        AddTokenHeader(request, projectId);
        var response = await httpClient.SendAsync(request);
        var dtos = await response.Content.ReadFromJsonAsync<List<ApiTextCommentDto>>();
        if (dtos is null) return new List<CommentViewModel>();
        return dtos.Select(d => new CommentViewModel
        {
            CommentId = d.CommentId,
            Text = d.Text,
            Emojis = d.Emojis.Select(e => new CommentEmojiModel
            {
                EmojiId = e.EmojiId,
                Symbol = e.Symbol,
                Count = e.Count,
                PostedByCurrentUser = e.PostedByCurrentUser
            }).ToList(),
        }).ToList();
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
        public bool PostedByCurrentUser { get; set; }
    }

    private class ApiIdeaDto
    {
        public int IdeaId { get; set; }
        public string Description { get; set; }
        public int CommentsCount { get; set; }
        public List<ApiIconCommentDto> Emojis { get; set; } = new();
    }
}
