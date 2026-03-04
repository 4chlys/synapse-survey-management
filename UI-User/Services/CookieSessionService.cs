namespace SynapseEnqueteManagement.UI.User.Services;

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
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(cookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(365)
        });
    }

    public bool HasToken(int projectId)
    {
        return !string.IsNullOrWhiteSpace(GetToken(projectId));
    }
}
