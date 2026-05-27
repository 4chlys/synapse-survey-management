using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.AiUsage;
using SynapseEnqueteManagement.API.DTOs.Stats;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.API.Services.Export;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Stats;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/platform-stats")]
public class AdminPlatformStatsExportController(
    IProjectManager projectManager,
    IParticipantManager participantManager,
    IAiUsageManager usageManager,
    ISystemStatsAggregator statsAggregator,
    IExportService exportService,
    IChartRendererService chartRenderer,
    IChartImageService chartImages) : ControllerBase
{
    [HttpGet("system/export")]
    public async Task<IActionResult> ExportSystemStats(
        [FromQuery] string format = "xlsx",
        [FromQuery] int? platformId = null,
        [FromQuery] string sections = "",
        [FromQuery] bool includeGraphics = false,
        CancellationToken ct = default)
    {
        var normalized = (format ?? "xlsx").Trim().ToLowerInvariant();
        if (normalized is not ("csv" or "xlsx" or "png-zip"))
            return BadRequest(new { error = $"Invalid format '{format}'." });

        var selectedSections = string.IsNullOrWhiteSpace(sections)
            ? SystemStatsExportSheetBuilder.AllSections
            : sections.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .Where(SystemStatsExportSheetBuilder.AllSections.Contains)
                .ToHashSet();
        if (selectedSections.Count == 0)
            selectedSections = SystemStatsExportSheetBuilder.AllSections;

        var projects = (await projectManager.GetAllProjectsAsync(platformId)).ToList();
        var projectIds = projects.Select(p => p.ProjectId).ToList();

        var participantStats = await participantManager.GetStatsByProjectsAsync(projectIds);
        var aiSystem   = (await usageManager.ReadServiceBreakdownSystemAsync(platformId, null)).ToList();
        var aiPlatform = (await usageManager.ReadServiceBreakdownByPlatformAsync(platformId, null)).ToList();
        var aiProject  = (await usageManager.ReadServiceBreakdownByProjectsAsync(platformId, null)).ToList();

        var rollup = statsAggregator.Aggregate(projects, participantStats);

        var statsDto = new SystemStatsDto
        {
            PlatformCount           = rollup.PlatformRows.Count,
            ActiveProjectCount      = projects.Count(p => p.ProjectState == ProjectState.Active),
            DraftProjectCount       = projects.Count(p => p.ProjectState == ProjectState.Draft),
            ArchivedProjectCount    = projects.Count(p => p.ProjectState == ProjectState.Archived),
            TotalParticipants       = rollup.TotalCompleted,
            AvgCompletionSeconds    = rollup.SysAvg,
            MedianCompletionSeconds = rollup.SysMedian,
            Platforms = rollup.PlatformRows.Select(r => new PlatformStatsRowDto
            {
                PlatformId              = r.PlatformId,
                Name                    = r.Name,
                ActiveCount             = r.ActiveCount,
                DraftCount              = r.DraftCount,
                ArchivedCount           = r.ArchivedCount,
                ParticipantCount        = r.ParticipantCount,
                AvgCompletionSeconds    = r.AvgCompletionSeconds,
                MedianCompletionSeconds = r.MedianCompletionSeconds,
            }).ToList(),
            Projects = rollup.ProjectRows.Select(r => new ProjectStatsRowDto
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
            }).ToList(),
            AiUsageBySystem = aiSystem.Select(s => new ServiceCostAggregateDto(
                s.Service.ToString(), s.Model, s.InputTokens, s.OutputTokens, s.RequestCount, s.CostEur)).ToList(),
            AiUsageByPlatform = aiPlatform.Select(s => new PlatformServiceCostAggregateDto(
                s.ProjectPlatformId, s.ProjectPlatformName, s.Service.ToString(), s.Model,
                s.InputTokens, s.OutputTokens, s.RequestCount, s.CostEur)).ToList(),
            AiUsageByProject = aiProject.Select(s => new ProjectServiceCostAggregateDto(
                s.ProjectId, s.ShortForm, s.ProjectPlatformId, s.ProjectPlatformName,
                s.Service.ToString(), s.Model, s.InputTokens, s.OutputTokens, s.RequestCount, s.CostEur)).ToList(),
        };

        var sheets = SystemStatsExportSheetBuilder.Build(statsDto, selectedSections);

        if (normalized == "xlsx")
        {
            var xlsxSheets = sheets.Select(s => (s.Name, s.Headers, s.Rows));
            var bytes = exportService.BuildXlsx(xlsxSheets);

            if (includeGraphics)
                bytes = await chartRenderer.InjectChartsAsync(bytes, statsDto, ct);

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "system-stats.xlsx");
        }

        if (normalized == "png-zip")
        {
            var imageFiles = chartImages.RenderAll(statsDto)
                .Select(img => ($"{img.Stem}.png", img.PngBytes))
                .ToList();
            var zipBytes = exportService.BuildBinaryZip(imageFiles);
            return File(zipBytes, "application/zip", "system-stats-charts.zip");
        }

        // csv
        if (sheets.Count == 1)
        {
            var s = sheets[0];
            var bytes = exportService.BuildCsv(s.Headers, s.Rows);
            return File(bytes, "text/csv", $"system-stats-{s.Key}.csv");
        }

        var csvZipFiles = sheets.Select(s => ($"system-stats-{s.Key}.csv", s.Headers, s.Rows));
        var csvZipBytes = exportService.BuildZip(csvZipFiles);
        return File(csvZipBytes, "application/zip", "system-stats.zip");
    }
}
