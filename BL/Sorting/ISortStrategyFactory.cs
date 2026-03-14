namespace SynapseEnqueteManagement.BL.Sorting;

public interface ISortStrategyFactory<T>
{
    ISortStrategy<T> Get(string key);
}
