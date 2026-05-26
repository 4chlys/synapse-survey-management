using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Ideas;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/projects/{projectId:int}/ideas")]
public class AdminIdeasController(
    IModerationManager moderationManager,
    IIdeaManager ideaManager,
    IProjectManager projectManager,
    IStorageService storageService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IdeaPageDto>> GetIdeas(
        int projectId,
        [FromQuery] string search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] string status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        ModerationStatus? statusEnum = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ModerationStatus>(status, ignoreCase: true, out var parsed))
            statusEnum = parsed;

        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var (items, total, approved, pending, flagged) =
            await moderationManager.GetIdeasPagedAsync(projectId, search, categoryId, statusEnum, page, lang, pageSize);

        return Ok(new IdeaPageDto
        {
            TotalCount    = total,
            Page          = page,
            PageSize      = pageSize,
            ApprovedCount = approved,
            PendingCount  = pending,
            FlaggedCount  = flagged,
            Items = items.Select(i => new AdminIdeaDto
            {
                IdeaId        = i.IdeaId,
                Description   = i.Description,
                CategoryName  = i.CategoryName,
                CategoryId    = i.CategoryId,
                ReactionCount = i.ReactionCount,
                CreatedAt     = i.CreatedAt,
                Status        = i.Status.ToString(),
                ParticipantEmail = i.ParticipantEmail,
                ImageUrl      = i.ImageUrl,
                LanguageCode  = i.LanguageCode
            }).ToList()
        });
    }

    [HttpDelete("{ideaId:int}/image")]
    public async Task<IActionResult> DeleteIdeaImage(int projectId, int ideaId, CancellationToken ct)
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        var oldUrl = await ideaManager.RemoveIdeaImageAsync(ideaId, projectId, ct);
        if (oldUrl is not null)
            await storageService.DeleteAsync(oldUrl);

        return NoContent();
    }

    [HttpPatch("{ideaId:int}/status")]
    public async Task<IActionResult> SetStatus(int projectId, int ideaId, SetIdeaStatusDto dto)
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        if (!Enum.TryParse<ModerationStatus>(dto.Status, ignoreCase: true, out var status))
            return BadRequest(new { error = $"Ongeldige status '{dto.Status}'." });

        await moderationManager.SetIdeaModerationStatusAsync(ideaId, status, projectId);
        return NoContent();
    }
}
