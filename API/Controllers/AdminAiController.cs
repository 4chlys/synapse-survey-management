using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.Ai;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.Domain.Ai;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/ai")]
public class AdminAiController(ChatClientRegistry registry, IPlatformAiConfigService configService, AiConfig aiConfig, ModelAvailabilityService availability) : ControllerBase
{
    [HttpGet("available-models")]
    public async Task<IActionResult> GetAvailableModels(CancellationToken ct)
    {
        var configured   = registry.AvailableModelIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var availability_ = await availability.GetAvailabilityAsync(ct);

        var models = AiModelCatalog.All
            .Where(m => configured.Contains(m.Id))
            .Select(m => new { id = m.Id, isAvailable = availability_.GetValueOrDefault(m.Id, false) })
            .ToList();

        return Ok(models);
    }

    [HttpGet("global-prompts")]
    public async Task<IActionResult> GetGlobalPrompts(CancellationToken ct)
    {
        var config = await configService.ResolveGlobalAsync(ct);
        return Ok(new
        {
            moderation        = config.Moderation.ModerationPrompt,
            commentModeration = config.Moderation.CommentModerationPrompt,
            rewrite           = config.Moderation.RewritePrompt,
            imageModeration   = config.Moderation.ImageModerationPrompt,
            ideaSupport       = config.IdeaSupport.SystemPrompt,
            commentSupport    = config.CommentSupport.SystemPrompt,
            questionSupport   = config.QuestionSupport.SystemPrompt,
            ideaSummary       = aiConfig.IdeaSummary.SystemPrompt,
        });
    }
}