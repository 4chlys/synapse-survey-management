using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IModerationTokenService
{
    string GenerateToken(string text, ModerationStatus status, int projectId);
    ModerationStatus? ValidateToken(string text, string token, int projectId, TimeSpan? maxAge = null);
    void ConsumeToken(string token);

    /// <summary>Issues a token bound to the SHA-256 hash of the image bytes.</summary>
    string GenerateImageToken(byte[] imageBytes, int projectId);

    /// <summary>Returns true if the token is valid, not expired, and scoped to the correct project.</summary>
    bool ValidateImageToken(string token, int projectId);
}