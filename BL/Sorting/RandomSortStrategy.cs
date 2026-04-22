namespace SynapseEnqueteManagement.BL.Sorting;

public class RandomSortStrategy<T> : ISortStrategy<T>
{
    public string Key => "random";
    public bool Randomize => true;
    public Func<IQueryable<T>, IOrderedQueryable<T>> DbOrderBy => q => q.OrderBy(_ => 0);
}
