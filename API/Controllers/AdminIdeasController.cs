using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Ideas;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/projects/{projectId:int}/ideas")]
public class AdminIdeasController(
    ICategoryManager categoryManager,
    IProjectManager projectManager) : ControllerBase
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

        var (items, total, approved, pending, flagged) =
            await categoryManager.GetIdeasPagedAsync(projectId, search, categoryId, statusEnum, page, pageSize);

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
                Status        = i.Status.ToString()
            }).ToList()
        });
    }

    [HttpPatch("{ideaId:int}/status")]
    public async Task<IActionResult> SetStatus(int projectId, int ideaId, [FromBody] SetIdeaStatusDto dto)
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        if (!Enum.TryParse<ModerationStatus>(dto.Status, ignoreCase: true, out var status))
            return BadRequest(new { error = $"Ongeldige status '{dto.Status}'." });

        await categoryManager.SetIdeaModerationStatusAsync(ideaId, status, projectId);
        return NoContent();
    }
}
