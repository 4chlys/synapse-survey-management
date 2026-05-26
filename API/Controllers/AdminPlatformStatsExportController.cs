using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.AiUsage;
using SynapseEnqueteManagement.API.DTOs.Stats;
using SynapseEnqueteManagement.API.Services;
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
    private static readonly HashSet<string> AllSections =
        ["platforms", "projects", "summary", "ai-system", "ai-platforms", "ai-projects"];

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
            ? AllSections
            : sections.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .Where(AllSections.Contains)
                .ToHashSet();
        if (selectedSections.Count == 0)
            selectedSections = AllSections;

        var projects = (await projectManager.GetAllProjectsAsync(platformId)).ToList();
        var projectIds = projects.Select(p => p.ProjectId).ToList();

        var participantStats = await participantManager.GetStatsByProjectsAsync(projectIds);
        var aiSystem   = (await usageManager.ReadServiceBreakdownSystemAsync(platformId, null)).ToList();
        var aiPlatform = (await usageManager.ReadServiceBreakdownByPlatformAsync(platformId, null)).ToList();
        var aiProject  = (await usageManager.ReadServiceBreakdownByProjectsAsync(platformId, null)).ToList();

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

        int totalCompleted = rollup.TotalCompleted;
        double sysAvg = rollup.SysAvg;
        double sysMedian = rollup.SysMedian;

        // Assemble export sheets for each requested section
        var sheets = new List<(string Key, string SheetName, string[] Headers, IEnumerable<string[]> Rows)>();

        if (selectedSections.Contains("platforms"))
        {
            sheets.Add(("platforms", "Platforms",
                ["Platform", "Active", "Draft", "Archived", "Participants", "AvgTime", "MedianTime"],
                platformRows.Select(r => new[]
                {
                    r.Name,
                    r.ActiveCount.ToString(), r.DraftCount.ToString(), r.ArchivedCount.ToString(),
                    r.ParticipantCount.ToString(),
                    r.AvgCompletionSeconds.ToString("F1"), r.MedianCompletionSeconds.ToString("F1"),
                })));
        }

        if (selectedSections.Contains("projects"))
        {
            sheets.Add(("projects", "Projects",
                ["Platform", "Project", "Short", "State", "Participants", "Started", "AvgTime", "MedianTime"],
                projectRows.Select(r => new[]
                {
                    r.PlatformName, r.ProjectName, r.ShortForm, r.State,
                    r.ParticipantCount.ToString(), r.StartedCount.ToString(),
                    r.AvgCompletionSeconds.ToString("F1"), r.MedianCompletionSeconds.ToString("F1"),
                })));
        }

        if (selectedSections.Contains("summary"))
        {
            List<string[]> summaryRows =
            [
                ["Platforms", platformRows.Count.ToString()],
                ["Active projects", projects.Count(p => p.ProjectState == ProjectState.Active).ToString()],
                ["Draft projects", projects.Count(p => p.ProjectState == ProjectState.Draft).ToString()],
                ["Archived projects", projects.Count(p => p.ProjectState == ProjectState.Archived).ToString()],
                ["Total participants", totalCompleted.ToString()],
                ["Avg completion (s)", sysAvg.ToString("F1")],
                ["Median completion (s)", sysMedian.ToString("F1")],
            ];
            sheets.Add(("summary", "Summary", ["Metric", "Value"], summaryRows));
        }

        if (selectedSections.Contains("ai-system"))
        {
            sheets.Add(("ai-system", "AI by service",
                ["Service", "Model", "InputTokens", "OutputTokens", "Requests", "Cost (EUR)"],
                aiSystem.Select(s => new[]
                {
                    s.Service.ToString(), s.Model,
                    s.InputTokens.ToString(), s.OutputTokens.ToString(),
                    s.RequestCount.ToString(), s.CostEur.ToString("F4"),
                })));
        }

        if (selectedSections.Contains("ai-platforms"))
        {
            sheets.Add(("ai-platforms", "AI by platform",
                ["Platform", "Service", "Model", "InputTokens", "OutputTokens", "Requests", "Cost (EUR)"],
                aiPlatform.Select(s => new[]
                {
                    s.ProjectPlatformName, s.Service.ToString(), s.Model,
                    s.InputTokens.ToString(), s.OutputTokens.ToString(),
                    s.RequestCount.ToString(), s.CostEur.ToString("F4"),
                })));
        }

        if (selectedSections.Contains("ai-projects"))
        {
            sheets.Add(("ai-projects", "AI by project",
                ["Platform", "Project", "Service", "Model", "InputTokens", "OutputTokens", "Requests", "Cost (EUR)"],
                aiProject.Select(s => new[]
                {
                    s.ProjectPlatformName, s.ShortForm, s.Service.ToString(), s.Model,
                    s.InputTokens.ToString(), s.OutputTokens.ToString(),
                    s.RequestCount.ToString(), s.CostEur.ToString("F4"),
                })));
        }

        // statsDto is shared by the chart renderer (xlsx) and the PNG zip path.
        SystemStatsDto BuildStatsDto() => new()
        {
            PlatformCount           = platformRows.Count,
            ActiveProjectCount      = projects.Count(p => p.ProjectState == ProjectState.Active),
            DraftProjectCount       = projects.Count(p => p.ProjectState == ProjectState.Draft),
            ArchivedProjectCount    = projects.Count(p => p.ProjectState == ProjectState.Archived),
            TotalParticipants       = totalCompleted,
            AvgCompletionSeconds    = sysAvg,
            MedianCompletionSeconds = sysMedian,
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
        };

        if (normalized == "xlsx")
        {
            var xlsxSheets = sheets.Select(s => (s.SheetName, s.Headers, s.Rows));
            var bytes = exportService.BuildXlsx(xlsxSheets);

            if (includeGraphics)
                bytes = await chartRenderer.InjectChartsAsync(bytes, BuildStatsDto(), ct);

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "system-stats.xlsx");
        }

        if (normalized == "png-zip")
        {
            var imageFiles = chartImages.RenderAll(BuildStatsDto())
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

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.Trim().Replace(' ', '-').ToLowerInvariant();
    }
}
