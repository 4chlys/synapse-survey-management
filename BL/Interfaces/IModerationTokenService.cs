using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IModerationTokenService
{
    string GenerateToken(string text, ModerationStatus status, int projectId);
    ModerationStatus? ValidateToken(string text, string token, int projectId, TimeSpan? maxAge = null);
    void ConsumeToken(string token);

    /// <summary>Issues a token bound to the SHA-256 hash of the image bytes, encoding the AI moderation result.</summary>
    string GenerateImageToken(byte[] imageBytes, int projectId, ModerationStatus status = ModerationStatus.Approved);

    /// <summary>Returns the moderation status embedded in the token, or null if the token is invalid/expired.</summary>
    ModerationStatus? ValidateImageToken(string token, int projectId);
}