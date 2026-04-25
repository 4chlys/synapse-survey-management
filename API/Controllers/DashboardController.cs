using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Dashboard;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/dashboard")]
public class DashboardController(
    IProjectManager projectManager,
    IParticipantManager participantManager,
    ICategoryManager categoryManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProjectDashboardDto>> GetDashboard(int projectId)
    {
        var project = await projectManager.GetProjectByIdReadOnlyAsync(projectId);
        if (project is null) return NotFound();

        var completionCount = await participantManager.CountCompletionsByProjectAsync(projectId);
        var categories = (await categoryManager
            .GetCategoriesWithIdeasForProjectReadOnlyAsync(projectId)).ToList();

        var topCategories = categories
            .Select(c =>
            {
                var approvedIdeas = c.Ideas
                    .Where(i => i.Status == ModerationStatus.Approved)
                    .ToList();
                var totalReactions = approvedIdeas
                    .Sum(i => i.Comments.OfType<IconComment>().Count());
                return new CategoryMetricDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    IdeaCount = approvedIdeas.Count,
                    TotalReactions = totalReactions
                };
            })
            .OrderByDescending(c => c.IdeaCount + c.TotalReactions)
            .Take(5)
            .ToList();

        var topIdeas = categories
            .SelectMany(c => c.Ideas)
            .Where(i => i.Status == ModerationStatus.Approved)
            .Select(i =>
            {
                var reactionCount = i.Comments.OfType<IconComment>().Count();
                var commentCount = i.Comments.OfType<TextComment>().Count();
                return new IdeaMetricDto
                {
                    IdeaId = i.IdeaId,
                    Description = i.Description.Length > 100
                        ? i.Description[..100] + "…"
                        : i.Description,
                    ReactionCount = reactionCount,
                    CommentCount = commentCount
                };
            })
            .OrderByDescending(i => i.ReactionCount + i.CommentCount)
            .Take(5)
            .ToList();

        return Ok(new ProjectDashboardDto
        {
            ProjectId = projectId,
            ViewCount = project.ViewCount,
            CompletionCount = completionCount,
            CompletionRate = project.ViewCount > 0
                ? Math.Round((double)completionCount / project.ViewCount, 2)
                : 0,
            TopCategories = topCategories,
            TopIdeas = topIdeas
        });
    }
}
