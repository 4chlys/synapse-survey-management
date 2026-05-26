using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using SynapseEnqueteManagement.API.Ai;

namespace SynapseEnqueteManagement.API.Tests.Unit;

public class ModelAvailabilityServiceTests
{
    private static ModelAvailabilityService BuildService(IDictionary<string, IChatClient> clients) =>
        new(new ChatClientRegistry(clients), new Mock<ILogger<ModelAvailabilityService>>().Object);

    private static Mock<IChatClient> ChatClientReturning() =>
        SetupClient(m => m
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, "ok")])));

    private static Mock<IChatClient> ChatClientThrowing() =>
        SetupClient(m => m
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("model not available in region")));

    private static Mock<IChatClient> SetupClient(Action<Mock<IChatClient>> setup)
    {
        var mock = new Mock<IChatClient>();
        setup(mock);
        return mock;
    }

    [Fact]
    public async Task NonVertexModel_IsMarkedAvailableWithoutProbing()
    {
        // gpt-4o is an OpenAI model — a registered client (API key present) is enough; no live probe.
        var client = new Mock<IChatClient>(MockBehavior.Strict);
        var service = BuildService(new Dictionary<string, IChatClient> { ["gpt-4o"] = client.Object });

        var result = await service.GetAvailabilityAsync();

        Assert.True(result["gpt-4o"]);
        client.Verify(c => c.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VertexModel_IsProbed_AvailableWhenProbeSucceeds()
    {
        var client = ChatClientReturning();
        var service = BuildService(new Dictionary<string, IChatClient> { ["gemini-2.5-flash-lite"] = client.Object });

        var result = await service.GetAvailabilityAsync();

        Assert.True(result["gemini-2.5-flash-lite"]);
        client.Verify(c => c.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VertexModel_IsMarkedUnavailableWhenProbeThrows()
    {
        var client = ChatClientThrowing();
        var service = BuildService(new Dictionary<string, IChatClient> { ["gemini-2.5-flash-lite"] = client.Object });

        var result = await service.GetAvailabilityAsync();

        Assert.False(result["gemini-2.5-flash-lite"]);
    }

    [Fact]
    public async Task Availability_IsCached_AcrossCalls()
    {
        var client = ChatClientReturning();
        var service = BuildService(new Dictionary<string, IChatClient> { ["gemini-2.5-flash-lite"] = client.Object });

        await service.GetAvailabilityAsync();
        await service.GetAvailabilityAsync();

        // The Vertex probe runs once; the second call is served from the 5-minute cache.
        client.Verify(c => c.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
