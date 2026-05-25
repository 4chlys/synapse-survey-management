using System.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using SynapseEnqueteManagement.API.Ai;
using SynapseEnqueteManagement.API.Controllers;

namespace SynapseEnqueteManagement.API.Tests.Unit;

public class AdminAiControllerTests
{
    [Fact]
    public async Task GetAvailableModels_ReturnsConfiguredModelsWithAvailabilityFlag()
    {
        // gemini (Vertex) is probed and fails -> unavailable; gpt-4o (OpenAI) needs no probe -> available.
        var failingVertex = new Mock<IChatClient>();
        failingVertex
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("region"));

        var registry = new ChatClientRegistry(new Dictionary<string, IChatClient>
        {
            ["gemini-2.5-flash-lite"] = failingVertex.Object,
            ["gpt-4o"]                = new Mock<IChatClient>().Object,
        });
        var availability = new ModelAvailabilityService(registry, new Mock<ILogger<ModelAvailabilityService>>().Object);
        var controller   = new AdminAiController(registry, new Mock<IPlatformAiConfigService>().Object, availability);

        var result = await controller.GetAvailableModels(CancellationToken.None);

        var ok    = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable>(ok.Value).Cast<object>().ToList();

        Assert.Equal(2, items.Count);
        Assert.True(AvailabilityOf(items, "gpt-4o"));
        Assert.False(AvailabilityOf(items, "gemini-2.5-flash-lite"));
    }

    [Fact]
    public async Task GetAvailableModels_ExcludesUnregisteredModels()
    {
        var registry     = new ChatClientRegistry(new Dictionary<string, IChatClient> { ["gpt-4o"] = new Mock<IChatClient>().Object });
        var availability  = new ModelAvailabilityService(registry, new Mock<ILogger<ModelAvailabilityService>>().Object);
        var controller    = new AdminAiController(registry, new Mock<IPlatformAiConfigService>().Object, availability);

        var result = await controller.GetAvailableModels(CancellationToken.None);

        var ok    = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable>(ok.Value).Cast<object>().ToList();

        Assert.Single(items);
        Assert.Equal("gpt-4o", IdOf(items[0]));
    }

    private static bool AvailabilityOf(IEnumerable<object> items, string id) =>
        (bool)Prop(items.Single(i => IdOf(i) == id), "isAvailable");

    private static string IdOf(object item) => (string)Prop(item, "id");

    private static object Prop(object item, string name) =>
        item.GetType().GetProperty(name)!.GetValue(item)!;
}
