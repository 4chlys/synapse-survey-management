using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class AiUsageManagerTests
{
    private readonly Mock<IAiUsageRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<AiUsageManager>> _logger = new();
    private readonly AiUsageManager _manager;
    private const string Model = "gemini-1.5-flash";

    public AiUsageManagerTests()
    {
        var config = new AiPricingConfig
        {
            Models = new() { [Model] = new ModelPrice(1m, 0m, 2m) }
        };
        var pricing = new AiPricing(Options.Create(config), NullLogger<AiPricing>.Instance);
        _manager = new AiUsageManager(_repo.Object, pricing, _uow.Object, _logger.Object);
    }

    [Fact]
    public async Task LogChatAsync_WithUsage_PersistsCorrectEntity()
    {
        AiUsageEvent captured = null;
        _repo.Setup(r => r.LogAsync(It.IsAny<AiUsageEvent>(), 42, It.IsAny<CancellationToken>()))
             .Callback<AiUsageEvent, int, CancellationToken>((ev, _, _) => captured = ev)
             .Returns(Task.CompletedTask);

        var usage = new UsageDetails { InputTokenCount = 1_000_000, OutputTokenCount = 500_000 };
        await _manager.LogChatAsync(42, AiServiceKind.IdeaSupport, Model, usage);

        Assert.NotNull(captured);
        Assert.Equal(AiServiceKind.IdeaSupport, captured.Service);
        Assert.Equal(Model, captured.Model);
        Assert.Equal(1_000_000, captured.InputTokens);
        Assert.Equal(500_000, captured.OutputTokens);
        Assert.Equal(1, captured.RequestCount);
        // (1M/1M)*1 + (500K/1M)*2 = 1 + 1 = 2
        Assert.Equal(2m, captured.CostEur);
        // ct + uow plumbing: the manager owns the commit now (repo no longer self-commits)
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogChatAsync_NullUsage_PersistsZeroCostAndLogsWarning()
    {
        AiUsageEvent captured = null;
        _repo.Setup(r => r.LogAsync(It.IsAny<AiUsageEvent>(), 1, It.IsAny<CancellationToken>()))
             .Callback<AiUsageEvent, int, CancellationToken>((ev, _, _) => captured = ev)
             .Returns(Task.CompletedTask);

        await _manager.LogChatAsync(1, AiServiceKind.Moderation, Model, null);

        Assert.NotNull(captured);
        Assert.Equal(0, captured.InputTokens);
        Assert.Equal(0, captured.OutputTokens);
        Assert.Equal(0m, captured.CostEur);
        Assert.Equal(1, captured.RequestCount);

        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogChatAsync_ProjectIdZero_DoesNotPersist()
    {
        await _manager.LogChatAsync(0, AiServiceKind.Moderation, Model, null);

        _repo.Verify(r => r.LogAsync(It.IsAny<AiUsageEvent>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogChatAsync_NegativeProjectId_DoesNotPersist()
    {
        await _manager.LogChatAsync(-5, AiServiceKind.Moderation, Model, null);

        _repo.Verify(r => r.LogAsync(It.IsAny<AiUsageEvent>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogChatAsync_PassesCancellationTokenThroughToRepoAndUnitOfWork()
    {
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        _repo.Setup(r => r.LogAsync(It.IsAny<AiUsageEvent>(), 7, ct))
             .Returns(Task.CompletedTask)
             .Verifiable(Times.Once());
        _uow.Setup(u => u.SaveChangesAsync(ct))
             .ReturnsAsync(1)
             .Verifiable(Times.Once());

        await _manager.LogChatAsync(7, AiServiceKind.Moderation, Model, new UsageDetails(), ct);

        _repo.VerifyAll();
        _uow.VerifyAll();
    }
}
