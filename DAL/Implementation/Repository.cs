using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class Repository<T>(DbContext context) : IRepository<T> where T : class
{
    protected DbContext Context { get; } = context;

    public async Task<T> CreateAsync(T entity)
    {
        await Context.Set<T>().AddAsync(entity);
        return entity;
    }

    public Task<T> ReadByIdForUpdateAsync(int id)
        => Context.Set<T>().FindAsync(id).AsTask();

    public async Task<IEnumerable<T>> ReadAllReadOnlyAsync()
        => await Context.Set<T>().AsNoTracking().ToListAsync();

    public void Update(T entity) => Context.Set<T>().Update(entity);

    // Issues a single DELETE outside the change tracker; callers must wrap in IUnitOfWork.ExecuteInTransactionAsync to preserve rollback.
    public async Task<bool> DeleteByIdAsync(int id)
    {
        var pkName = typeof(T).Name + "Id";
        var rows = await Context.Set<T>()
            .Where(e => EF.Property<int>(e, pkName) == id)
            .ExecuteDeleteAsync();
        return rows > 0;
    }
}
