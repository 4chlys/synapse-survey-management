namespace SynapseEnqueteManagement.BL.Sorting;

public class SortStrategyFactory<T>(IEnumerable<ISortStrategy<T>> strategies, string defaultKey)
    : ISortStrategyFactory<T>
{
    private readonly Dictionary<string, ISortStrategy<T>> _strategies = strategies.ToDictionary(s => s.Key);

    public ISortStrategy<T> Get(string key) =>
        _strategies.TryGetValue(key ?? defaultKey, out var s) ? s : _strategies[defaultKey];
}
