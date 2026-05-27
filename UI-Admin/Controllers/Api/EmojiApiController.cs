using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.UI.Admin.Filters;
using SynapseEnqueteManagement.UI.Admin.Models.Project;
using SynapseEnqueteManagement.UI.Admin.Services.Project;

namespace SynapseEnqueteManagement.UI.Admin.Controllers.Api;

[ApiController]
[IgnoreAntiforgeryToken]
[Authorize(Roles = "ProjectPlatformAdmin")]
[RequireProjectPlatformOwnership]
[Route("api/admin/projects/{projectId:int}/emojis")]
public class EmojiApiController(
    EmojiAdminService emojiService,
    ProjectAdminService projectService,
    IStringLocalizer<SharedResource> localizer) : ControllerBase
{
    [HttpPost("{emojiId:int}/enable")]
    public async Task<IActionResult> EnableEmoji(int projectId, int emojiId)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null) return NotFound();
        if (IsProjectLocked(project))
            return StatusCode(403, localizer["Project_LockedNotEditable"].Value);
        await emojiService.EnableEmojiAsync(projectId, emojiId);
        return NoContent();
    }

    [HttpPost("{emojiId:int}/disable")]
    public async Task<IActionResult> DisableEmoji(int projectId, int emojiId)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null) return NotFound();
        if (IsProjectLocked(project))
            return StatusCode(403, localizer["Project_LockedNotEditable"].Value);
        await emojiService.DisableEmojiAsync(projectId, emojiId);
        return NoContent();
    }

    [HttpPost("enable-preset")]
    public async Task<IActionResult> EnableEmojiPreset(int projectId, [FromForm] List<int> emojiIds)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null) return NotFound();
        if (IsProjectLocked(project))
            return StatusCode(403, localizer["Project_LockedNotEditable"].Value);
        await emojiService.EnableEmojiPresetAsync(projectId, emojiIds ?? new());
        return NoContent();
    }

    [HttpPost("disable-preset")]
    public async Task<IActionResult> DisableEmojiPreset(int projectId, [FromForm] List<int> emojiIds)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null) return NotFound();
        if (IsProjectLocked(project))
            return StatusCode(403, localizer["Project_LockedNotEditable"].Value);
        await emojiService.DisableEmojiPresetAsync(projectId, emojiIds ?? new());
        return NoContent();
    }

    private static bool IsProjectLocked(ProjectAdminViewModel project) =>
        project.ProjectState is "Active" or "Archived";
}
