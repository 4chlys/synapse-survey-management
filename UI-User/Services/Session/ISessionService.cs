namespace SynapseEnqueteManagement.UI.User.Services.Session;

public interface ISessionService
{
    string GetToken(int projectId);
    void SetToken(int projectId, string token);
    bool HasToken(int projectId);
}
