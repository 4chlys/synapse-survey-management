using SynapseEnqueteManagement.BL.Domain;

namespace SynapseEnqueteManagement.BL.Sorting;

public class OldestFirstSortStrategy<T> : ISortStrategy<T> where T : IHasCreatedAt
{
    public string Key => "oldest";
    public bool Randomize => false;
    public Func<IQueryable<T>, IOrderedQueryable<T>> DbOrderBy => q => q.OrderBy(x => x.CreatedAt);
}
