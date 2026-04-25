using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.User.Models.Ideation;
using SynapseEnqueteManagement.UI.User.Services.Session;

namespace SynapseEnqueteManagement.UI.User.Services.Ideation;

public class IdeaService(HttpClient httpClient, ISessionService sessionService)
    : AuthenticatedApiService(httpClient, sessionService)
{
    public async Task<List<IdeaViewModel>> GetIdeasByCategoryAsync(int projectId, int categoryId, int page = 1, string sort = null, string queryText = null)
    {
        var url = $"api/projects/{projectId}/categories/{categoryId}/ideas?page={page}";
        if (sort is not null) url += $"&sort={Uri.EscapeDataString(sort)}";
        if (queryText is not null) url += $"&query={Uri.EscapeDataString(queryText)}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddTokenHeader(request, projectId);
        var response = await HttpClient.SendAsync(request);
        var paged = await response.Content.ReadFromJsonAsync<ApiPagedResult<IdeaViewModel>>();
        return paged?.Items ?? new List<IdeaViewModel>();
    }

    public async Task<CreateIdeaResultViewModel> CreateIdeaAsync(
        int projectId,
        int categoryId,
        string description,
        string moderationToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"api/projects/{projectId}/categories/{categoryId}/ideas");
        request.Content = JsonContent.Create(new { Description = description, ModerationToken = moderationToken });

        AddTokenHeader(request, projectId);
        var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return new CreateIdeaResultViewModel { Success = false };

        var result = await response.Content.ReadFromJsonAsync<CreateIdeaResultViewModel>();
        if (result is null) return new CreateIdeaResultViewModel { Success = false };

        result.Success = true;
        return result;
    }

    public async Task<List<IdeaViewModel>> GetMyIdeasAsync(int projectId, int categoryId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"api/projects/{projectId}/categories/{categoryId}/ideas/mine");
        AddTokenHeader(request, projectId);
        var response = await HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new List<IdeaViewModel>();
        return await response.Content.ReadFromJsonAsync<List<IdeaViewModel>>() ?? new List<IdeaViewModel>();
    }

    public async Task<IdeaViewModel> GetIdeaByIdAsync(int projectId, int categoryId, int ideaId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}");
        AddTokenHeader(request, projectId);
        var response = await HttpClient.SendAsync(request);
        return await response.Content.ReadFromJsonAsync<IdeaViewModel>();
    }

    public async Task<IdeaIndexResult> GetIdeasForIndexAsync(int projectId, int categoryId, string sort)
    {
        var ownIdeas = await GetMyIdeasAsync(projectId, categoryId);
        var queryText = ownIdeas.FirstOrDefault()?.Description;

        if ((sort == "similar" || sort == "different") && queryText is null)
            sort = "diverse";

        var ideas = await GetIdeasByCategoryAsync(projectId, categoryId, sort: sort, queryText: queryText);

        return new IdeaIndexResult
        {
            Ideas = ideas,
            ResolvedSort = sort,
            HasOwnIdeas = ownIdeas.Count > 0
        };
    }

    public async Task<List<CommentViewModel>> GetCommentsByIdeaAsync(int projectId, int categoryId, int ideaId, int page = 1, string sort = null)
    {
        var url = $"api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}/comments?page={page}";
        if (sort is not null) url += $"&sort={sort}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddTokenHeader(request, projectId);
        var response = await HttpClient.SendAsync(request);
        var paged = await response.Content.ReadFromJsonAsync<ApiPagedResult<CommentViewModel>>();
        return paged?.Items ?? new List<CommentViewModel>();
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
