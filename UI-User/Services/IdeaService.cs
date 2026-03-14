using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

public class IdeaService(HttpClient httpClient, ISessionService sessionService)
{
    private const string TokenHeader = "X-Participant-Token";

    public async Task<List<IdeaViewModel>> GetIdeasByCategoryAsync(int projectId, int categoryId, int page = 1, string sort = null)
    {
        var url = $"api/projects/{projectId}/categories/{categoryId}/ideas?page={page}";
        if (sort is not null) url += $"&sort={sort}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddTokenHeader(request, projectId);
        var response = await httpClient.SendAsync(request);
        var paged = await response.Content.ReadFromJsonAsync<ApiPagedResult<IdeaViewModel>>();
        return paged?.Items ?? new List<IdeaViewModel>();
    }

    public async Task<bool> CreateIdeaAsync(
        int projectId,
        int categoryId,
        string description,
        string moderationToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"api/projects/{projectId}/categories/{categoryId}/ideas");
        request.Content = JsonContent.Create(new { Description = description, ModerationToken = moderationToken });

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
        return await response.Content.ReadFromJsonAsync<IdeaViewModel>();
    }

    public async Task<List<CommentViewModel>> GetCommentsByIdeaAsync(int projectId, int categoryId, int ideaId, int page = 1, string sort = null)
    {
        var url = $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}/comments?page={page}";
        if (sort is not null) url += $"&sort={sort}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddTokenHeader(request, projectId);
        var response = await httpClient.SendAsync(request);
        var paged = await response.Content.ReadFromJsonAsync<ApiPagedResult<CommentViewModel>>();
        return paged?.Items ?? new List<CommentViewModel>();
    }

    private void AddTokenHeader(HttpRequestMessage request, int projectId)
    {
        var token = sessionService.GetToken(projectId);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Add(TokenHeader, token);
    }

    private class ApiPagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

}
