using System.Linq.Expressions;

namespace SynapseEnqueteManagement.DAL;

public interface IRepository
{
    Task<T> CreateAsync<T>(T entity, Expression<Func<T, bool>> duplicateCheck = null) where T : class;

    Task<IEnumerable<T>> ReadAsync<T>(
        Expression<Func<T, bool>>[] filters = null,
        Expression<Func<T, object>>[] includes = null,
        string[] includeStrings = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        bool randomize = false,
        int? skip = null,
        int? take = null,
        bool tracked = false
    ) where T : class;

    Task<int> CountAsync<T>(Expression<Func<T, bool>>[] filters = null) where T : class;

    Task<T> ReadByIdAsync<T>(
        object id,
        Expression<Func<T, object>>[] includes = null,
        string[] includeStrings = null,
        bool tracked = false
    ) where T : class;

    Task<T> ReadSingleAsync<T>(
        Expression<Func<T, bool>>[] filters = null,
        Expression<Func<T, object>>[] includes = null,
        string[] includeStrings = null,
        bool tracked = false
    ) where T : class;

    Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> filter) where T : class;

    Task UpdateAsync<T>(T entity) where T : class;

    Task<bool> DeleteAsync<T>(Expression<Func<T, bool>> filter) where T : class;

    Task<bool> DeleteByIdAsync<T>(params object[] id) where T : class;
}