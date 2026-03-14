namespace SynapseEnqueteManagement.BL.Sorting;

public interface ISortStrategy<T>
{
    string Key { get; }
    bool Randomize { get; }
    Func<IQueryable<T>, IOrderedQueryable<T>> DbOrderBy { get; }
    IEnumerable<T> Sort(IEnumerable<T> items);
}
