namespace SynapseEnqueteManagement.UI.User.Services.Session;

public class CookieSessionService : ISessionService
{
    private const string CookiePrefix = "ParticipantToken_";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CookieSessionService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetToken(int projectId)
    {
        var cookieName = CookiePrefix + projectId;
        if (_httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue(cookieName, out var token) == true)
            return token;
        return null;
    }

    public void SetToken(int projectId, string token)
    {
        var cookieName = CookiePrefix + projectId;
        var isHttps = _httpContextAccessor.HttpContext?.Request.IsHttps ?? false;
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(cookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(365)
        });

    }

    public bool HasToken(int projectId)
    {
        return !string.IsNullOrWhiteSpace(GetToken(projectId));
    }
    
    public void ClearToken(int projectId)
    {
        var cookieName = CookiePrefix + projectId;
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete(cookieName);
    }

    public int GetIdeaRefreshCount(int projectId, int categoryId)
    {
        var cookieName = $"IdeaRefresh_{projectId}_{categoryId}";
        if (_httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue(cookieName, out var value) == true
            && int.TryParse(value, out var count))
            return count;
        return 0;
    }

    public void IncrementIdeaRefreshCount(int projectId, int categoryId)
    {
        var cookieName = $"IdeaRefresh_{projectId}_{categoryId}";
        var current = GetIdeaRefreshCount(projectId, categoryId);
        var isHttps = _httpContextAccessor.HttpContext?.Request.IsHttps ?? false;
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(cookieName, (current + 1).ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(365)
        });
    }
}
