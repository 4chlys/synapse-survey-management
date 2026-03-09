using System.Linq.Expressions;

namespace SynapseEnqueteManagement.DAL;


public interface IRepository
{
    T Create<T>(T entity, Expression<Func<T, bool>> duplicateCheck = null) where T : class;

    IEnumerable<T> Read<T>(
        Expression<Func<T, bool>>[] filters = null,
        Expression<Func<T, object>>[] includes = null,
        string[] includeStrings = null
    ) where T : class;

    public T ReadById<T>(params object[] id) where T : class;

    T ReadSingle<T>(
        Expression<Func<T, bool>>[] filters = null,
        Expression<Func<T, object>>[] includes = null,
        string[] includeStrings = null
    ) where T : class;

    bool Update<T>(T entity) where T : class;

    bool Delete<T>(Expression<Func<T, bool>> filter) where T : class;

    bool DeleteById<T>(params object[] id) where T : class;
}