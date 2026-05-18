using Microsoft.Extensions.AI;

namespace SynapseEnqueteManagement.API.Ai;

public sealed class ChatClientRegistry(IDictionary<string, IChatClient> clients, string defaultModelId = null)
{
    private readonly IReadOnlyDictionary<string, IChatClient> _clients = new Dictionary<string, IChatClient>(clients, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> AvailableModelIds => _clients.Keys.ToList();
    public string DefaultModelId { get; } = defaultModelId;

    public IChatClient Get(string modelId)
    {
        if (_clients.TryGetValue(modelId, out var client)) return client;
        throw new InvalidOperationException($"AI model '{modelId}' is not available in this deployment.");
    }

    public bool TryGet(string modelId, out IChatClient client)
        => _clients.TryGetValue(modelId, out client);
}