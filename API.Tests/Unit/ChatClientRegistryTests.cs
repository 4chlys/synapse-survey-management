using Microsoft.Extensions.AI;
using Moq;
using SynapseEnqueteManagement.API.Ai;

namespace SynapseEnqueteManagement.API.Tests.Unit;

public class ChatClientRegistryTests
{
    [Fact]
    public void Get_ReturnsRegisteredClient()
    {
        var client = new Mock<IChatClient>().Object;
        var registry = new ChatClientRegistry(new Dictionary<string, IChatClient>
        {
            ["gemini-2.5-flash"] = client
        });

        var result = registry.Get("gemini-2.5-flash");

        Assert.Same(client, result);
    }

    [Fact]
    public void Get_UnknownModel_Throws()
    {
        var registry = new ChatClientRegistry(new Dictionary<string, IChatClient>());

        Assert.Throws<InvalidOperationException>(() => registry.Get("unknown-model"));
    }

    [Fact]
    public void TryGet_MissingModel_ReturnsFalse()
    {
        var registry = new ChatClientRegistry(new Dictionary<string, IChatClient>());

        var found = registry.TryGet("missing", out var client);

        Assert.False(found);
        Assert.Null(client);
    }

    [Fact]
    public void AvailableModelIds_ReflectsRegisteredModels()
    {
        var clients = new Dictionary<string, IChatClient>
        {
            ["gpt-4o"]      = new Mock<IChatClient>().Object,
            ["grok-3-mini"] = new Mock<IChatClient>().Object,
        };
        var registry = new ChatClientRegistry(clients);

        Assert.Equal(2, registry.AvailableModelIds.Count);
        Assert.Contains("gpt-4o", registry.AvailableModelIds);
    }
}
