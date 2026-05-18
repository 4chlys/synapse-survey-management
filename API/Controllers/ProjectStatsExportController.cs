using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Dashboard;
using SynapseEnqueteManagement.API.DTOs.Stats;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/stats")]
public class ProjectStatsExportController(
    IProjectManager projectManager,
    IParticipantManager participantManager,
    ICategoryManager categoryManager,
    IExportService exportService,
    IChartRendererService chartRenderer) : ControllerBase
{
    private static readonly HashSet<string> AllSections = ["summary", "timeline", "categories", "ideas"];

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        int projectId,
        [FromQuery] string format = "xlsx",
        [FromQuery] string sections = "",
        [FromQuery] bool includeGraphics = false,
        CancellationToken ct = default)
    {
        var normalized = (format ?? "xlsx").Trim().ToLowerInvariant();
        if (normalized is not ("csv" or "xlsx"))
            return BadRequest(new { error = $"Invalid format '{format}'." });

        var project = await projectManager.GetProjectByIdReadOnlyAsync(projectId);
        if (project is null)
            return NotFound();

        var selectedSections = string.IsNullOrWhiteSpace(sections)
            ? AllSections
            : sections.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .Where(AllSections.Contains)
                .ToHashSet();
        if (selectedSections.Count == 0)
            selectedSections = AllSections;

        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var defaultLang = project.DefaultLanguage?.Code ?? lang;
        var projectName = project.Translations
            .FirstOrDefault(t => t.Language?.Code == defaultLang)?.Name
            ?? project.Translations.FirstOrDefault()?.Name
            ?? string.Empty;

        var completionTask = participantManager.CountCompletionsByProjectAsync(projectId);
        var metricsTask = categoryManager.GetDashboardMetricsAsync(projectId, lang, defaultLang);
        var timelineTask = participantManager.GetTimelineAsync(projectId, 30);
        await Task.WhenAll(completionTask, metricsTask, timelineTask);

        var completionCount = completionTask.Result;
        var metrics = metricsTask.Result;
        var timeline = timelineTask.Result;
        var completionRate = project.ViewCount > 0
            ? Math.Round((double)completionCount / project.ViewCount, 4)
            : 0;

        var exportData = new ProjectExportDataDto
        {
            ProjectId = projectId,
            ProjectName = projectName,
            ViewCount = project.ViewCount,
            CompletionCount = completionCount,
            CompletionRate = completionRate,
            TimelineDates = timeline.Dates,
            TimelineStarts = timeline.Starts,
            TimelineCompletions = timeline.Completions,
            TopCategories = metrics.TopCategories.Select(c => new CategoryMetricDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                IdeaCount = c.IdeaCount,
                TotalReactions = c.TotalReactions,
            }).ToList(),
            TopIdeas = metrics.TopIdeas.Select(i => new IdeaMetricDto
            {
                IdeaId = i.IdeaId,
                Description = i.Description,
                ReactionCount = i.ReactionCount,
                CommentCount = i.CommentCount,
            }).ToList(),
        };

        var sheets = new List<(string Key, string SheetName, string[] Headers, IEnumerable<string[]> Rows)>();

        if (selectedSections.Contains("summary"))
        {
            sheets.Add(("summary", "Summary", ["Metric", "Value"],
            [
                ["Views", exportData.ViewCount.ToString()],
                ["Completions", exportData.CompletionCount.ToString()],
                ["Completion rate", (exportData.CompletionRate * 100).ToString("F1") + "%"],
            ]));
        }

        if (selectedSections.Contains("timeline"))
        {
            sheets.Add(("timeline", "Timeline",
                ["Date", "Started", "Completed"],
                exportData.TimelineDates.Select((d, i) => new[]
                {
                    d,
                    exportData.TimelineStarts.ElementAtOrDefault(i).ToString(),
                    exportData.TimelineCompletions.ElementAtOrDefault(i).ToString(),
                })));
        }

        if (selectedSections.Contains("categories"))
        {
            sheets.Add(("categories", "Categories",
                ["Category", "Ideas", "Reactions"],
                exportData.TopCategories.Select(c => new[]
                {
                    c.Name,
                    c.IdeaCount.ToString(),
                    c.TotalReactions.ToString(),
                })));
        }

        if (selectedSections.Contains("ideas"))
        {
            sheets.Add(("ideas", "Ideas",
                ["Description", "Reactions", "Comments"],
                exportData.TopIdeas.Select(i => new[]
                {
                    i.Description ?? string.Empty,
                    i.ReactionCount.ToString(),
                    i.CommentCount.ToString(),
                })));
        }

        var safeName = SanitizeFileName(projectName.Length > 0 ? projectName : projectId.ToString());

        if (normalized == "xlsx")
        {
            var xlsxSheets = sheets.Select(s => (s.SheetName, s.Headers, s.Rows));
            var bytes = exportService.BuildXlsx(xlsxSheets);

            if (includeGraphics)
                bytes = await chartRenderer.InjectProjectChartsAsync(bytes, exportData, ct);

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{safeName}-dashboard.xlsx");
        }

        if (sheets.Count == 1)
        {
            var s = sheets[0];
            var bytes = exportService.BuildCsv(s.Headers, s.Rows);
            return File(bytes, "text/csv", $"{safeName}-{s.Key}.csv");
        }

        var zipFiles = sheets.Select(s => ($"{safeName}-{s.Key}.csv", s.Headers, s.Rows));
        var zipBytes = exportService.BuildZip(zipFiles);
        return File(zipBytes, "application/zip", $"{safeName}-dashboard.zip");
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.Trim().Replace(' ', '-').ToLowerInvariant();
    }
}
