namespace SynapseEnqueteManagement.UI.User.Services;

public interface ISessionService
{
    string GetToken(int projectId);
    void SetToken(int projectId, string token);
    bool HasToken(int projectId);
}