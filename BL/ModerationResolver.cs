using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL;

public class ModerationResolver(
    IModerationTokenService tokenService,
    IAiModerationService moderationService) : IModerationResolver
{
    public async Task<ModerationStatus> ResolveStatusAsync(string text, string token, int projectId)
    {
        if (!string.IsNullOrEmpty(token))
        {
            var cached = tokenService.ValidateToken(text, token, projectId);
            if (cached.HasValue) return cached.Value;
        }

        var result = await moderationService.CheckWithAiAsync(text);
        return result.IsAcceptable ? ModerationStatus.Unflagged : ModerationStatus.Flagged;
    }
}
