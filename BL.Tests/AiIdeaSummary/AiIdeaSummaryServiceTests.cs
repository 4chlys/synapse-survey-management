using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SynapseEnqueteManagement.BL.AiIdeaSummary;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Tests.AiIdeaSummary;

public class AiIdeaSummaryServiceTests
{
    private static Mock<IChatClient> MakeChatClient(string reply)
    {
        var mock = new Mock<IChatClient>();
        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, reply)]));
        return mock;
    }

    private static List<Idea> MakeIdeas(int count) =>
        Enumerable.Range(1, count).Select(i => new Idea
        {
            IdeaId = i,
            Description = $"Idea {i}",
            Comments = [],
        }).ToList();

    [Fact]
    public async Task GenerateSummaryAsync_ReturnsSummaryText()
    {
        var client = MakeChatClient("Dit is de samenvatting.");
        var usageManager = new Mock<IAiUsageManager>();
        var svc = new AiIdeaSummaryService(NullLogger<AiIdeaSummaryService>.Instance, usageManager.Object);

        var result = await svc.GenerateSummaryAsync(
            MakeIdeas(3), client.Object, "model-id", "Dit is de prompt.", projectId: 1);

        Assert.Equal("Dit is de samenvatting.", result);
    }

    [Fact]
    public async Task GenerateBatchSummaryAsync_MergesPartialSummaries()
    {
        int callCount = 0;
        var mock = new Mock<IChatClient>();
        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new ChatResponse([new ChatMessage(ChatRole.Assistant, $"Partial {callCount}")]);
            });

        var usageManager = new Mock<IAiUsageManager>();
        var svc = new AiIdeaSummaryService(NullLogger<AiIdeaSummaryService>.Instance, usageManager.Object);

        // 7 ideas, batchSize 3 → 3 batches + 1 merge call = 4 total calls
        var result = await svc.GenerateBatchSummaryAsync(
            MakeIdeas(7), mock.Object, "model-id", "Prompt.", batchSize: 3, projectId: 1);

        Assert.Equal(4, callCount);
        Assert.Equal("Partial 4", result); // merge call returns last partial reply
    }
}