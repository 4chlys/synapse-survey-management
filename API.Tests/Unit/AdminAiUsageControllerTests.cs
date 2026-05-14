using SynapseEnqueteManagement.API.DTOs.AiUsage;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.API.Tests;

public class AdminAiUsageControllerTests
{
    private readonly Mock<IAiUsageManager> _usageManager = new();
    private readonly AdminAiUsageController _controller;

    public AdminAiUsageControllerTests()
    {
        _controller = new AdminAiUsageController(_usageManager.Object);
    }

    [Fact]
    public async Task GetProjects_ReturnsOkWithMappedDtos()
    {
        var aggregate = new ProjectCostAggregate(1, "demo", 10, "KdG", 100L, 50L, 2, 0.01m);
        _usageManager.Setup(m => m.ReadProjectAggregatesAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { aggregate });

        var result = await _controller.GetProjects(null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<ICollection<ProjectCostAggregateDto>>(ok.Value);
        Assert.Single(dtos);
        var dto = dtos.First();
        Assert.Equal(1, dto.ProjectId);
        Assert.Equal("demo", dto.ProjectName);
        Assert.Equal(10, dto.PlatformId);
        Assert.Equal("KdG", dto.PlatformName);
        Assert.Equal(100L, dto.InputTokens);
        Assert.Equal(50L, dto.OutputTokens);
        Assert.Equal(0.01m, dto.CostEur);
    }

    [Fact]
    public async Task GetProjects_NoFromParam_DefaultsToApproximatelyLast24Hours()
    {
        DateTime? capturedFrom = null;
        _usageManager.Setup(m => m.ReadProjectAggregatesAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime?, CancellationToken>((f, _) => capturedFrom = f)
            .ReturnsAsync(Array.Empty<ProjectCostAggregate>());

        await _controller.GetProjects(null);

        Assert.NotNull(capturedFrom);
        Assert.True(capturedFrom > DateTime.UtcNow.AddHours(-25));
        Assert.True(capturedFrom < DateTime.UtcNow.AddHours(-23));
    }

    [Fact]
    public async Task GetPlatforms_ReturnsOkWithMappedDtos()
    {
        var aggregate = new PlatformCostAggregate(5, "KdG", 200L, 100L, 3, 0.05m);
        _usageManager.Setup(m => m.ReadPlatformAggregatesAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { aggregate });

        var result = await _controller.GetPlatforms(null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<ICollection<PlatformCostAggregateDto>>(ok.Value);
        Assert.Single(dtos);
        var dto = dtos.First();
        Assert.Equal(5, dto.PlatformId);
        Assert.Equal("KdG", dto.PlatformName);
        Assert.Equal(0.05m, dto.CostEur);
    }

    [Fact]
    public async Task GetByService_ReturnsOkWithMappedDtos()
    {
        var aggregate = new ServiceCostAggregate(AiServiceKind.IdeaSupport, "gemini-1.5-flash", 300L, 150L, 1, 0.02m);
        _usageManager.Setup(m => m.ReadServiceBreakdownByProjectAsync(7, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { aggregate });

        var result = await _controller.GetByService(7, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<ICollection<ServiceCostAggregateDto>>(ok.Value);
        Assert.Single(dtos);
        var dto = dtos.First();
        Assert.Equal("IdeaSupport", dto.Service);
        Assert.Equal("gemini-1.5-flash", dto.Model);
        Assert.Equal(300L, dto.InputTokens);
        Assert.Equal(150L, dto.OutputTokens);
        Assert.Equal(0.02m, dto.CostEur);
    }

    [Fact]
    public async Task GetProjects_EmptyResult_ReturnsOkWithEmptyCollection()
    {
        _usageManager.Setup(m => m.ReadProjectAggregatesAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProjectCostAggregate>());

        var result = await _controller.GetProjects(null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<ICollection<ProjectCostAggregateDto>>(ok.Value);
        Assert.Empty(dtos);
    }
}
