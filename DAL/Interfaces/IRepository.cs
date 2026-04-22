using System.Linq.Expressions;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T> CreateAsync(T entity, Expression<Func<T, bool>> duplicateCheck = null);

    Task<IEnumerable<T>> ReadAsync(
        Expression<Func<T, bool>>[] filters = null,
        Expression<Func<T, object>>[] includes = null,
        string[] includeStrings = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        bool randomize = false,
        int? skip = null,
        int? take = null,
        bool tracked = false
    );

    Task<int> CountAsync(Expression<Func<T, bool>>[] filters = null);

    Task<T> ReadByIdAsync(
        object id,
        Expression<Func<T, object>>[] includes = null,
        string[] includeStrings = null,
        bool tracked = false
    );

    Task<T> ReadSingleAsync(
        Expression<Func<T, bool>>[] filters = null,
        Expression<Func<T, object>>[] includes = null,
        string[] includeStrings = null,
        bool tracked = false
    );

    Task<bool> ExistsAsync(Expression<Func<T, bool>> filter);

    void Update(T entity);

    Task<bool> DeleteAsync(Expression<Func<T, bool>> filter);

    Task<bool> DeleteByIdAsync(params object[] id);
}
