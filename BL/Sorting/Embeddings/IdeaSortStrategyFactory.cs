namespace SynapseEnqueteManagement.BL.Sorting.Embeddings;

public class IdeaSortStrategyFactory(IEnumerable<IIdeaSortStrategy> strategies) : IIdeaSortStrategyFactory
{
    private readonly Dictionary<string, IIdeaSortStrategy> _strategies =
        strategies.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);

    public bool IsEmbeddingKey(string key) => key != null && _strategies.ContainsKey(key);

    public IIdeaSortStrategy Get(string key) =>
        _strategies.TryGetValue(key ?? string.Empty, out var strategy)
            ? strategy
            : throw new KeyNotFoundException($"No embedding sort strategy registered for key '{key}'");
}
