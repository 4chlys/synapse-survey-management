using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.AiUsage;
using SynapseEnqueteManagement.API.DTOs.Stats;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/platform-stats")]
public class AdminPlatformStatsController(
    IProjectManager projectManager,
    IParticipantManager participantManager,
    IAiUsageManager usageManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PlatformStatsDto>> GetStats([FromQuery] int platformId)
    {
        var projects = (await projectManager.GetAllProjectsAsync(platformId)).ToList();
        var counts = await Task.WhenAll(
            projects.Select(p => participantManager.CountCompletionsByProjectAsync(p.ProjectId)));

        return Ok(new PlatformStatsDto
        {
            ActiveCount   = projects.Count(p => p.ProjectState == ProjectState.Active),
            DraftCount    = projects.Count(p => p.ProjectState == ProjectState.Draft),
            ArchivedCount = projects.Count(p => p.ProjectState == ProjectState.Archived),
            ParticipantCount = counts.Sum()
        });
    }

    [HttpGet("system")]
    public async Task<ActionResult<SystemStatsDto>> GetSystemStats(
        [FromQuery] int? platformId,
        [FromQuery] DateTime? aiFrom)
    {
        var projects = (await projectManager.GetAllProjectsAsync(platformId)).ToList();
        var projectIds = projects.Select(p => p.ProjectId).ToList();

        var statsTask      = participantManager.GetStatsByProjectsAsync(projectIds);
        var aiSystemTask   = usageManager.ReadServiceBreakdownSystemAsync(platformId, aiFrom);
        var aiPlatformTask = usageManager.ReadServiceBreakdownByPlatformAsync(platformId, aiFrom);
        var aiProjectTask  = usageManager.ReadServiceBreakdownByProjectsAsync(platformId, aiFrom);

        await Task.WhenAll(statsTask, aiSystemTask, aiPlatformTask, aiProjectTask);

        var participantStats = statsTask.Result;
        var statsById = participantStats.ToDictionary(s => s.ProjectId);

        var projectRows = projects.Select(p =>
        {
            statsById.TryGetValue(p.ProjectId, out var ps);
            var name = p.Translations
                .FirstOrDefault(t => t.Language?.Code == p.DefaultLanguage?.Code)?.Name
                ?? p.Translations.FirstOrDefault()?.Name
                ?? string.Empty;
            return new ProjectStatsRowDto
            {
                ProjectId               = p.ProjectId,
                ProjectName             = name,
                ShortForm               = p.ShortForm,
                PlatformId              = p.ProjectPlatformId,
                PlatformName            = p.ProjectPlatform?.Name ?? string.Empty,
                State                   = p.ProjectState.ToString(),
                ParticipantCount        = ps?.CompletedCount ?? 0,
                StartedCount            = ps?.StartedCount ?? 0,
                AvgCompletionSeconds    = ps?.AvgCompletionSeconds ?? 0,
                MedianCompletionSeconds = ps?.MedianCompletionSeconds ?? 0,
                MinCompletionSeconds    = ps?.MinCompletionSeconds ?? 0,
                Q1CompletionSeconds     = ps?.Q1CompletionSeconds ?? 0,
                Q3CompletionSeconds     = ps?.Q3CompletionSeconds ?? 0,
                MaxCompletionSeconds    = ps?.MaxCompletionSeconds ?? 0,
            };
        }).ToList();

        var platformRows = projects
            .GroupBy(p => p.ProjectPlatformId)
            .Select(g =>
            {
                var gProjects = g.ToList();
                // Look up via statsById (O(1)) instead of scanning participantStats
                var gStats = gProjects
                    .Select(p => statsById.GetValueOrDefault(p.ProjectId))
                    .Where(s => s is not null)
                    .ToList();
                int gCompleted = gStats.Sum(s => s.CompletedCount);
                double gAvg = gCompleted > 0
                    ? gStats.Sum(s => s.AvgCompletionSeconds * s.CompletedCount) / gCompleted
                    : 0;
                // avg-of-project-medians; true platform median needs per-participant durations
                double gMedian = gStats.Count > 0 ? gStats.Average(s => s.MedianCompletionSeconds) : 0;
                return new PlatformStatsRowDto
                {
                    PlatformId              = g.Key,
                    Name                    = gProjects.First().ProjectPlatform?.Name ?? string.Empty,
                    ActiveCount             = gProjects.Count(p => p.ProjectState == ProjectState.Active),
                    DraftCount              = gProjects.Count(p => p.ProjectState == ProjectState.Draft),
                    ArchivedCount           = gProjects.Count(p => p.ProjectState == ProjectState.Archived),
                    ParticipantCount        = gCompleted,
                    AvgCompletionSeconds    = gAvg,
                    MedianCompletionSeconds = gMedian,
                };
            }).ToList();

        int totalCompleted = participantStats.Sum(s => s.CompletedCount);
        double systemAvg = totalCompleted > 0
            ? participantStats.Sum(s => s.AvgCompletionSeconds * s.CompletedCount) / totalCompleted
            : 0;
        double systemMedian = participantStats.Count > 0
            ? participantStats.Average(s => s.MedianCompletionSeconds)
            : 0;

        return Ok(new SystemStatsDto
        {
            PlatformCount           = platformRows.Count,
            ActiveProjectCount      = projects.Count(p => p.ProjectState == ProjectState.Active),
            DraftProjectCount       = projects.Count(p => p.ProjectState == ProjectState.Draft),
            ArchivedProjectCount    = projects.Count(p => p.ProjectState == ProjectState.Archived),
            TotalParticipants       = totalCompleted,
            AvgCompletionSeconds    = systemAvg,
            MedianCompletionSeconds = systemMedian,
            Platforms               = platformRows,
            Projects                = projectRows,
            AiUsageBySystem = aiSystemTask.Result.Select(s => new ServiceCostAggregateDto(
                s.Service.ToString(), s.Model, s.InputTokens, s.OutputTokens, s.RequestCount, s.CostEur)).ToList(),
            AiUsageByPlatform = aiPlatformTask.Result.Select(s => new PlatformServiceCostAggregateDto(
                s.ProjectPlatformId, s.ProjectPlatformName, s.Service.ToString(), s.Model,
                s.InputTokens, s.OutputTokens, s.RequestCount, s.CostEur)).ToList(),
            AiUsageByProject = aiProjectTask.Result.Select(s => new ProjectServiceCostAggregateDto(
                s.ProjectId, s.ShortForm, s.ProjectPlatformId, s.ProjectPlatformName,
                s.Service.ToString(), s.Model, s.InputTokens, s.OutputTokens, s.RequestCount, s.CostEur)).ToList(),
        });
    }
}