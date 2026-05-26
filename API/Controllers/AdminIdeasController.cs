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
    ICategoryManager categoryManager,
    IIdeaManager ideaManager,
    IProjectManager projectManager,
    ICommentManager commentManager,
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
            await categoryManager.GetIdeasPagedAsync(projectId, search, categoryId, statusEnum, page, lang, pageSize);

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

    [HttpGet("{ideaId:int}")]
    public async Task<ActionResult<AdminIdeaDetailDto>> GetIdeaDetail(int projectId, int ideaId)
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        var idea = await ideaManager.GetIdeaByIdWithCommentsReadOnlyAsync(ideaId);
        if (idea is null)
            return NotFound();

        var directReactions = idea.Comments
            .OfType<SynapseEnqueteManagement.BL.Domain.Project.Entities.IconComment>()
            .Where(ic => ic.TextComment == null)
            .GroupBy(ic => ic.Emoji.EmojiId)
            .Select(g => new AdminIdeaEmojiReactionDto
            {
                Symbol = g.First().Emoji.Symbol,
                Name   = g.First().Emoji.Name,
                Count  = g.Count()
            })
            .ToList();

        var comments = idea.Comments
            .OfType<SynapseEnqueteManagement.BL.Domain.Project.Entities.TextComment>()
            .OrderBy(c => c.CreatedAt)
            .Select(c => new AdminCommentDto
            {
                CommentId        = c.CommentId,
                Text             = c.Text,
                ModerationStatus = c.ModerationStatus.ToString(),
                CreatedAt        = c.CreatedAt,
                ParticipantEmail = c.Participant?.Email,
                LanguageCode     = c.Language?.Code,
                EmojiReactions   = c.EmojiReactions
                    .GroupBy(r => r.Emoji.EmojiId)
                    .Select(g => new AdminIdeaEmojiReactionDto
                    {
                        Symbol = g.First().Emoji.Symbol,
                        Name   = g.First().Emoji.Name,
                        Count  = g.Count()
                    })
                    .ToList()
            })
            .ToList();

        return Ok(new AdminIdeaDetailDto
        {
            IdeaId      = idea.IdeaId,
            Description = idea.Description,
            Status      = idea.Status.ToString(),
            CreatedAt        = idea.CreatedAt,
            ParticipantEmail = idea.Participant?.Email,
            ImageUrl         = idea.ImageUrl,
            LanguageCode     = idea.Language?.Code,
            EmojiReactions   = directReactions,
            Comments         = comments
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

        await categoryManager.SetIdeaModerationStatusAsync(ideaId, status, projectId);
        return NoContent();
    }

    [HttpPatch("{ideaId:int}/comments/{commentId:int}/status")]
    public async Task<IActionResult> SetCommentStatus(int projectId, int ideaId, int commentId, SetIdeaStatusDto dto, CancellationToken ct)
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        if (!Enum.TryParse<ModerationStatus>(dto.Status, ignoreCase: true, out var status))
            return BadRequest(new { error = $"Ongeldige status '{dto.Status}'." });

        try
        {
            await commentManager.SetCommentModerationStatusAsync(commentId, status, projectId, ct);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        return NoContent();
    }
}
