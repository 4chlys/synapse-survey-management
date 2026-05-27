using SynapseEnqueteManagement.API.DTOs.AiUsage;
using SynapseEnqueteManagement.API.DTOs.Stats;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.BL.Domain.Stats;
using SynapseEnqueteManagement.BL.Stats;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.API.Tests;

public class AdminPlatformStatsControllerTests
{
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IParticipantManager> _participantManager = new();
    private readonly Mock<IAiUsageManager> _usageManager = new();
    private readonly Mock<ISystemStatsAggregator> _statsAggregator = new();
    private readonly Mock<IExportService> _exportService = new();
    private readonly Mock<IChartRendererService> _chartRenderer = new();
    private readonly Mock<IChartImageService> _chartImages = new();

    private readonly AdminPlatformStatsController _statsController;
    private readonly AdminPlatformStatsExportController _exportController;

    public AdminPlatformStatsControllerTests()
    {
        _statsController = new AdminPlatformStatsController(
            _projectManager.Object,
            _participantManager.Object,
            _usageManager.Object,
            _statsAggregator.Object);

        _exportController = new AdminPlatformStatsExportController(
            _projectManager.Object,
            _participantManager.Object,
            _usageManager.Object,
            _statsAggregator.Object,
            _exportService.Object,
            _chartRenderer.Object,
            _chartImages.Object);

        _statsAggregator
            .Setup(a => a.Aggregate(It.IsAny<IReadOnlyList<Project>>(), It.IsAny<IReadOnlyList<ProjectParticipantStats>>()))
            .Returns(new SystemStatsRollup([], [], 0, 0, 0));

        _projectManager
            .Setup(m => m.GetAllProjectsAsync(It.IsAny<int?>()))
            .ReturnsAsync(Array.Empty<Project>());
        _participantManager
            .Setup(m => m.GetStatsByProjectsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(Array.Empty<ProjectParticipantStats>());
        _usageManager
            .Setup(m => m.ReadServiceBreakdownSystemAsync(It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ServiceCostAggregate>());
        _usageManager
            .Setup(m => m.ReadServiceBreakdownByPlatformAsync(It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlatformServiceCostAggregate>());
        _usageManager
            .Setup(m => m.ReadServiceBreakdownByProjectsAsync(It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProjectServiceCostAggregate>());
        _exportService
            .Setup(e => e.BuildXlsx(It.IsAny<IEnumerable<(string, string[], IEnumerable<string[]>)>>()))
            .Returns(new byte[] { 1 });
        _exportService
            .Setup(e => e.BuildCsv(It.IsAny<string[]>(), It.IsAny<IEnumerable<string[]>>()))
            .Returns(new byte[] { 1 });
        _exportService
            .Setup(e => e.BuildZip(It.IsAny<IEnumerable<(string, string[], IEnumerable<string[]>)>>()))
            .Returns(new byte[] { 1 });
        _exportService
            .Setup(e => e.BuildBinaryZip(It.IsAny<IEnumerable<(string, byte[])>>()))
            .Returns(new byte[] { 1 });
        _chartRenderer
            .Setup(r => r.InjectChartsAsync(It.IsAny<byte[]>(), It.IsAny<SystemStatsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[] b, SystemStatsDto _, CancellationToken _) => b);
        _chartImages
            .Setup(c => c.RenderAll(It.IsAny<SystemStatsDto>()))
            .Returns(new List<(string Stem, byte[] PngBytes)>());
        _chartImages
            .Setup(c => c.RenderCompletionBoxPlot(It.IsAny<SystemStatsDto>()))
            .Returns(Array.Empty<byte>());
    }

    private static Project MakeProject(int id, int platformId, ProjectState state = ProjectState.Active) => new()
    {
        ProjectId = id,
        ShortForm = $"p{id}",
        ProjectState = state,
        ProjectPlatformId = platformId,
        ProjectPlatform = new ProjectPlatform { ProjectPlatformId = platformId, Name = $"Platform {platformId}" },
        Translations = [],
    };

    [Fact]
    public async Task GetSystemStats_NoFilter_MapsAggregatorRollupOntoDto()
    {
        var projects = new[] { MakeProject(1, 10), MakeProject(2, 20) };
        _projectManager
            .Setup(m => m.GetAllProjectsAsync(null))
            .ReturnsAsync(projects);

        var rollup = new SystemStatsRollup(
            ProjectRows:
            [
                new ProjectStatsRow(1, "P1", "p1", 10, "Platform 10", "Active", 5, 5, 60, 60, 0, 0, 0, 0),
                new ProjectStatsRow(2, "P2", "p2", 20, "Platform 20", "Active", 3, 3, 90, 90, 0, 0, 0, 0),
            ],
            PlatformRows:
            [
                new PlatformStatsRow(10, "Platform 10", 1, 0, 0, 5, 60, 60),
                new PlatformStatsRow(20, "Platform 20", 1, 0, 0, 3, 90, 90),
            ],
            TotalCompleted: 8, SysAvg: 71.25, SysMedian: 75);
        _statsAggregator
            .Setup(a => a.Aggregate(It.IsAny<IReadOnlyList<Project>>(), It.IsAny<IReadOnlyList<ProjectParticipantStats>>()))
            .Returns(rollup);

        var result = await _statsController.GetSystemStats(null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SystemStatsDto>(ok.Value);
        Assert.Equal(2, dto.PlatformCount);
        Assert.Equal(2, dto.Projects.Count);
        Assert.Equal(8, dto.TotalParticipants);
        Assert.Equal(71.25, dto.AvgCompletionSeconds);
        _statsAggregator.Verify(a => a.Aggregate(
            It.Is<IReadOnlyList<Project>>(p => p.Count == 2),
            It.IsAny<IReadOnlyList<ProjectParticipantStats>>()), Times.Once);
    }

    [Fact]
    public async Task GetSystemStats_WithPlatformId_FiltersToThatPlatform()
    {
        _projectManager
            .Setup(m => m.GetAllProjectsAsync(5))
            .ReturnsAsync(new[] { MakeProject(1, 5) });

        await _statsController.GetSystemStats(5, null);

        _projectManager.Verify(m => m.GetAllProjectsAsync(5), Times.Once);
    }

    [Fact]
    public async Task GetSystemStats_IncludesAiBreakdowns_AtAllThreeGranularities()
    {
        _usageManager
            .Setup(m => m.ReadServiceBreakdownSystemAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new ServiceCostAggregate(AiServiceKind.IdeaSupport, "gemini", 10L, 5L, 1, 0.01m) });
        _usageManager
            .Setup(m => m.ReadServiceBreakdownByPlatformAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new PlatformServiceCostAggregate(10, "KdG", AiServiceKind.IdeaSupport, "gemini", 10L, 5L, 1, 0.01m) });
        _usageManager
            .Setup(m => m.ReadServiceBreakdownByProjectsAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new ProjectServiceCostAggregate(1, "p1", 10, "KdG", AiServiceKind.IdeaSupport, "gemini", 10L, 5L, 1, 0.01m) });

        var result = await _statsController.GetSystemStats(null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SystemStatsDto>(ok.Value);
        Assert.Single(dto.AiUsageBySystem);
        Assert.Single(dto.AiUsageByPlatform);
        Assert.Single(dto.AiUsageByProject);
    }

    [Fact]
    public async Task ExportSystemStats_Xlsx_ReturnsFile()
    {
        var result = await _exportController.ExportSystemStats("xlsx", null, "", false, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
        Assert.Equal("system-stats.xlsx", file.FileDownloadName);
    }

    [Fact]
    public async Task ExportSystemStats_Csv_SingleSection_ReturnsCsv()
    {
        var result = await _exportController.ExportSystemStats("csv", null, "platforms", false, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", file.ContentType);
    }

    [Fact]
    public async Task ExportSystemStats_Csv_MultiSection_ReturnsZip()
    {
        var result = await _exportController.ExportSystemStats("csv", null, "platforms,projects", false, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/zip", file.ContentType);
    }

    [Fact]
    public async Task ExportSystemStats_InvalidFormat_ReturnsBadRequest()
    {
        var result = await _exportController.ExportSystemStats("xml", null, "", false, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExportSystemStats_IncludeGraphics_InjectsChartsExactlyOnce()
    {
        var result = await _exportController.ExportSystemStats("xlsx", null, "", true, CancellationToken.None);

        _chartRenderer.Verify(r => r.InjectChartsAsync(It.IsAny<byte[]>(), It.IsAny<SystemStatsDto>(), It.IsAny<CancellationToken>()), Times.Once);
        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
    }

    [Fact]
    public async Task ExportSystemStats_PngZip_ReturnsZipOfImages()
    {
        var result = await _exportController.ExportSystemStats("png-zip", null, "", false, CancellationToken.None);

        _chartImages.Verify(c => c.RenderAll(It.IsAny<SystemStatsDto>()), Times.Once);
        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/zip", file.ContentType);
        Assert.Equal("system-stats-charts.zip", file.FileDownloadName);
    }

    [Fact]
    public async Task ExportSystemStats_AiSection_OnlyAiSheetEmitted()
    {
        IEnumerable<(string, string[], IEnumerable<string[]>)> capturedSheets = null;
        _exportService
            .Setup(e => e.BuildXlsx(It.IsAny<IEnumerable<(string, string[], IEnumerable<string[]>)>>()))
            .Callback<IEnumerable<(string, string[], IEnumerable<string[]>)>>(s => capturedSheets = s)
            .Returns(new byte[] { 1 });

        await _exportController.ExportSystemStats("xlsx", null, "ai-system", false, CancellationToken.None);

        Assert.NotNull(capturedSheets);
        Assert.Single(capturedSheets);
    }

    [Fact]
    public async Task ExportSystemStats_AllSections_IncludesExpectedSheetCount()
    {
        IEnumerable<(string, string[], IEnumerable<string[]>)> capturedSheets = null;
        _exportService
            .Setup(e => e.BuildXlsx(It.IsAny<IEnumerable<(string, string[], IEnumerable<string[]>)>>()))
            .Callback<IEnumerable<(string, string[], IEnumerable<string[]>)>>(s => capturedSheets = s)
            .Returns(new byte[] { 1 });

        await _exportController.ExportSystemStats("xlsx", null, "", false, CancellationToken.None);

        Assert.NotNull(capturedSheets);
        Assert.Equal(6, capturedSheets.Count());
    }
}
