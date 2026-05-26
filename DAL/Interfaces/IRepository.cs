namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T> CreateAsync(T entity);

    Task<T> ReadByIdForUpdateAsync(int id);

    Task<IEnumerable<T>> ReadAllReadOnlyAsync();

    Task<IEnumerable<T>> ReadAllForUpdateAsync();

    void Update(T entity);

    Task<bool> DeleteByIdAsync(int id);
}
