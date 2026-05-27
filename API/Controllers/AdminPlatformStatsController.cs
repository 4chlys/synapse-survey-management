using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.AiUsage;
using SynapseEnqueteManagement.API.DTOs.Stats;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Stats;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/platform-stats")]
public class AdminPlatformStatsController(
    IProjectManager projectManager,
    IParticipantManager participantManager,
    IAiUsageManager usageManager,
    ISystemStatsAggregator statsAggregator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PlatformStatsDto>> GetStats([FromQuery] int platformId)
    {
        var projects = (await projectManager.GetAllProjectsAsync(platformId)).ToList();
        var counts = new List<int>(projects.Count);
        foreach (var p in projects)
            counts.Add(await participantManager.CountCompletionsByProjectAsync(p.ProjectId));

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

        var participantStats = await participantManager.GetStatsByProjectsAsync(projectIds);
        var aiSystem   = await usageManager.ReadServiceBreakdownSystemAsync(platformId, aiFrom);
        var aiPlatform = await usageManager.ReadServiceBreakdownByPlatformAsync(platformId, aiFrom);
        var aiProject  = await usageManager.ReadServiceBreakdownByProjectsAsync(platformId, aiFrom);


        // Roll-up math lives in the BL aggregator; map its result rows to the API DTOs here.
        var rollup = statsAggregator.Aggregate(projects, participantStats);

        var projectRows = rollup.ProjectRows.Select(r => new ProjectStatsRowDto
        {
            ProjectId               = r.ProjectId,
            ProjectName             = r.ProjectName,
            ShortForm               = r.ShortForm,
            PlatformId              = r.PlatformId,
            PlatformName            = r.PlatformName,
            State                   = r.State,
            ParticipantCount        = r.ParticipantCount,
            StartedCount            = r.StartedCount,
            AvgCompletionSeconds    = r.AvgCompletionSeconds,
            MedianCompletionSeconds = r.MedianCompletionSeconds,
            MinCompletionSeconds    = r.MinCompletionSeconds,
            Q1CompletionSeconds     = r.Q1CompletionSeconds,
            Q3CompletionSeconds     = r.Q3CompletionSeconds,
            MaxCompletionSeconds    = r.MaxCompletionSeconds,
        }).ToList();

        var platformRows = rollup.PlatformRows.Select(r => new PlatformStatsRowDto
        {
            PlatformId              = r.PlatformId,
            Name                    = r.Name,
            ActiveCount             = r.ActiveCount,
            DraftCount              = r.DraftCount,
            ArchivedCount           = r.ArchivedCount,
            ParticipantCount        = r.ParticipantCount,
            AvgCompletionSeconds    = r.AvgCompletionSeconds,
            MedianCompletionSeconds = r.MedianCompletionSeconds,
        }).ToList();

        return Ok(new SystemStatsDto
        {
            PlatformCount           = platformRows.Count,
            ActiveProjectCount      = projects.Count(p => p.ProjectState == ProjectState.Active),
            DraftProjectCount       = projects.Count(p => p.ProjectState == ProjectState.Draft),
            ArchivedProjectCount    = projects.Count(p => p.ProjectState == ProjectState.Archived),
            TotalParticipants       = rollup.TotalCompleted,
            AvgCompletionSeconds    = rollup.SysAvg,
            MedianCompletionSeconds = rollup.SysMedian,
            Platforms               = platformRows,
            Projects                = projectRows,
            AiUsageBySystem = aiSystem.Select(s => new ServiceCostAggregateDto(
                s.Service.ToString(), s.Model, s.InputTokens, s.OutputTokens, s.RequestCount, s.CostEur)).ToList(),
            AiUsageByPlatform = aiPlatform.Select(s => new PlatformServiceCostAggregateDto(
                s.ProjectPlatformId, s.ProjectPlatformName, s.Service.ToString(), s.Model,
                s.InputTokens, s.OutputTokens, s.RequestCount, s.CostEur)).ToList(),
            AiUsageByProject = aiProject.Select(s => new ProjectServiceCostAggregateDto(
                s.ProjectId, s.ShortForm, s.ProjectPlatformId, s.ProjectPlatformName,
                s.Service.ToString(), s.Model, s.InputTokens, s.OutputTokens, s.RequestCount, s.CostEur)).ToList(),
        });
    }
}