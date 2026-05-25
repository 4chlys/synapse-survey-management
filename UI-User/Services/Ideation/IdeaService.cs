using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.User.Models.Ideation;
using SynapseEnqueteManagement.UI.User.Services.Session;

namespace SynapseEnqueteManagement.UI.User.Services.Ideation;

public class IdeaService(HttpClient httpClient, ISessionService sessionService)
    : AuthenticatedApiService(httpClient, sessionService)
{
    public async Task<IdeaIndexResult> GetIdeasIndexAsync(int projectId, int categoryId, string sort = null, int page = 1, bool showAllLanguages = false)
    {
        var url = $"api/projects/{projectId}/categories/{categoryId}/ideas?page={page}";
        if (sort is not null) url += $"&sort={Uri.EscapeDataString(sort)}";
        if (showAllLanguages) url += "&showAllLanguages=true";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddTokenHeader(request, projectId);
        var response = await HttpClient.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ApiIndexResponse>();
        return new IdeaIndexResult
        {
            Ideas = body?.Items ?? new List<IdeaViewModel>(),
            ResolvedSort = body?.ResolvedSort ?? sort,
            HasOwnIdeas = body?.HasOwnIdeas ?? false
        };
    }

    public async Task<CreateIdeaResultViewModel> CreateIdeaAsync(
        int projectId,
        int categoryId,
        string description,
        string moderationToken = null,
        string imageUrl = null,
        string imageModerationToken = null,
        string languageCode = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"api/projects/{projectId}/categories/{categoryId}/ideas");
        request.Content = JsonContent.Create(new
        {
            Description = description,
            ModerationToken = moderationToken,
            ImageUrl = imageUrl,
            ImageModerationToken = imageModerationToken,
            LanguageCode = languageCode,
        });

        AddTokenHeader(request, projectId);
        var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return new CreateIdeaResultViewModel { Success = false };

        var result = await response.Content.ReadFromJsonAsync<CreateIdeaResultViewModel>();
        if (result is null) return new CreateIdeaResultViewModel { Success = false };

        return result with { Success = true };
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

    private record ApiPagedResult<T>
    {
        public List<T> Items { get; init; } = new();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
    }

    private record ApiIndexResponse : ApiPagedResult<IdeaViewModel>
    {
        public string ResolvedSort { get; init; }
        public bool HasOwnIdeas { get; init; }
    }
}
