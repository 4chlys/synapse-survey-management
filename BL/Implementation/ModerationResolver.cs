using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ModerationResolver(
    IModerationTokenService tokenService,
    IAiModerationService moderationService) : IModerationResolver
{
    public async Task<ModerationStatus> ResolveStatusAsync(string text, string token, int projectId, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(token))
        {
            var cached = tokenService.ValidateToken(text, token, projectId);
            if (cached.HasValue)
            {
                tokenService.ConsumeToken(token);
                return cached.Value;
            }
        }

        var result = await moderationService.CheckWithAiAsync(text, ct);
        return result.IsAcceptable ? ModerationStatus.Unflagged : ModerationStatus.Flagged;
    }
}
