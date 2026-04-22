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

    public async Task<bool> DeleteByIdAsync(int id)
    {
        var entity = await Context.Set<T>().FindAsync(id);
        if (entity == null) return false;
        Context.Set<T>().Remove(entity);
        return true;
    }
}
