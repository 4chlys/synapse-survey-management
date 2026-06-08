using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using SynapseEnqueteManagement.API.DTOs.AiUsage;
using SynapseEnqueteManagement.API.DTOs.Dashboard;
using SynapseEnqueteManagement.API.DTOs.Stats;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.API.Services.Export;

namespace SynapseEnqueteManagement.API.Tests.Unit;

// Diagnostic: re-opens the chart-injected XLSX with the OpenXML validator so the
// exact invalid element/part is named instead of relying on Excel's vague
// "we found a problem with some content" repair dialog.
public class XlsxChartRendererValidationTests
{
    // 1x1 transparent PNG — lets the EmbedImage path run deterministically
    // without depending on ScottPlot's native rendering libs.
    private static readonly byte[] TinyPng = System.Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    private sealed class StubChartImages : IChartImageService
    {
        public IReadOnlyList<(string Stem, byte[] PngBytes)> RenderAll(SystemStatsDto stats) =>
            new[] { ("chart", TinyPng) };

        public byte[] RenderCompletionBoxPlot(SystemStatsDto stats) => TinyPng;

        public IReadOnlyList<(string Stem, byte[] PngBytes)> RenderProjectCharts(ProjectExportDataDto data) =>
            new[] { ("participation-timeline", TinyPng), ("top-categories", TinyPng) };
    }

    [Fact]
    public async Task SystemChartInjection_ProducesValidXlsx()
    {
        var export = new ExportService();
        var renderer = new XlsxChartRendererService(new StubChartImages());

        var stats = new SystemStatsDto
        {
            PlatformCount = 1,
            ActiveProjectCount = 3,
            DraftProjectCount = 2,
            ArchivedProjectCount = 1,
            TotalParticipants = 50,
            Platforms = new[]
            {
                new PlatformStatsRowDto { PlatformId = 1, Name = "Platform A", ActiveCount = 3, ParticipantCount = 50 },
            },
            Projects = new[]
            {
                new ProjectStatsRowDto { ProjectId = 1, ProjectName = "Project One", ShortForm = "P1", PlatformName = "Platform A", State = "Active", ParticipantCount = 30 },
                new ProjectStatsRowDto { ProjectId = 2, ProjectName = "Project Two", ShortForm = "P2", PlatformName = "Platform A", State = "Active", ParticipantCount = 20 },
            },
            AiUsageBySystem = new[]
            {
                new ServiceCostAggregateDto("OpenAi", "gpt-4o", 1000, 500, 10, 1.25m),
                new ServiceCostAggregateDto("Vertex", "gemini", 2000, 800, 5, 0.75m),
            },
        };

        var sheets = SystemStatsExportSheetBuilder.Build(stats, SystemStatsExportSheetBuilder.AllSections);
        var bytes = export.BuildXlsx(sheets.Select(s => (s.Name, s.Headers, s.Rows)));
        bytes = await renderer.InjectChartsAsync(bytes, stats);

        AssertValid(bytes);
    }

    [Fact]
    public async Task ProjectChartInjection_ProducesValidXlsx()
    {
        var export = new ExportService();
        var renderer = new XlsxChartRendererService(new StubChartImages());

        var data = new ProjectExportDataDto
        {
            ProjectId = 1,
            ProjectName = "Project One",
            ViewCount = 100,
            CompletionCount = 60,
            CompletionRate = 0.6,
            TimelineDates = new[] { "2026-06-01", "2026-06-02" },
            TimelineStarts = new[] { 10, 12 },
            TimelineCompletions = new[] { 6, 8 },
            TopCategories = new[]
            {
                new CategoryMetricDto { CategoryId = 1, Name = "Cat A", IdeaCount = 5, TotalReactions = 20 },
                new CategoryMetricDto { CategoryId = 2, Name = "Cat B", IdeaCount = 3, TotalReactions = 10 },
            },
            TopIdeas = new[]
            {
                new IdeaMetricDto { IdeaId = 1, Description = "Idea A", ReactionCount = 15, CommentCount = 4 },
            },
        };

        var sheets = ProjectStatsExportSheetBuilder.Build(data, ProjectStatsExportSheetBuilder.AllSections);
        var bytes = export.BuildXlsx(sheets.Select(s => (s.Name, s.Headers, s.Rows)));
        bytes = await renderer.InjectProjectChartsAsync(bytes, data);

        AssertValid(bytes);
    }

    private static void AssertValid(byte[] xlsxBytes)
    {
        using var ms = new MemoryStream(xlsxBytes);
        using var doc = SpreadsheetDocument.Open(ms, false);

        var validator = new OpenXmlValidator(DocumentFormat.OpenXml.FileFormatVersions.Office2019);
        var errors = validator.Validate(doc).ToList();

        var report = string.Join("\n", errors.Select(e =>
            $"[{e.ErrorType}] {e.Description}\n    Part: {e.Part?.Uri}\n    Path: {e.Path?.XPath}\n    Node: {e.Node?.LocalName}"));

        Assert.True(errors.Count == 0, $"OpenXML validation produced {errors.Count} error(s):\n{report}");
    }
}
