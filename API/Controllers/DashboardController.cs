using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Dashboard;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/dashboard")]
public class DashboardController(
    IProjectManager projectManager,
    IParticipantManager participantManager,
    IDashboardManager dashboardManager) : ControllerBase
{
    private static string ActiveCulture() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    [HttpGet]
    public async Task<ActionResult<ProjectDashboardDto>> GetDashboard(int projectId)
    {
        var project = await projectManager.GetProjectByIdReadOnlyAsync(projectId);
        if (project is null) return NotFound();

        var lang = ActiveCulture();
        var defaultLang = project.DefaultLanguage?.Code ?? lang;

        var completionCount = await participantManager.CountCompletionsByProjectAsync(projectId);
        var metrics = await dashboardManager.GetDashboardMetricsAsync(projectId, lang, defaultLang);

        return Ok(new ProjectDashboardDto
        {
            ProjectId = projectId,
            ViewCount = project.ViewCount,
            CompletionCount = completionCount,
            CompletionRate = project.ViewCount > 0
                ? Math.Round((double)completionCount / project.ViewCount, 2)
                : 0,
            TopCategories = metrics.TopCategories.Select(c => new CategoryMetricDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                IdeaCount = c.IdeaCount,
                TotalReactions = c.TotalReactions
            }).ToList(),
            TopIdeas = metrics.TopIdeas.Select(i => new IdeaMetricDto
            {
                IdeaId = i.IdeaId,
                Description = i.Description,
                ReactionCount = i.ReactionCount,
                CommentCount = i.CommentCount
            }).ToList()
        });
    }
}
