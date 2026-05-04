namespace SynapseEnqueteManagement.UI.User.Services.Session;

public interface ISessionService
{
    string GetToken(int projectId);
    void SetToken(int projectId, string token);
    bool HasToken(int projectId);
    void ClearToken(int projectId);
    int GetIdeaRefreshCount(int projectId, int categoryId);
    void IncrementIdeaRefreshCount(int projectId, int categoryId);
}
