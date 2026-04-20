using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace SynapseEnqueteManagement.DAL;

/// <summary>
/// A generic repository class that provides standard CRUD (Create, Read, Update, Delete)
/// operations for any entity type using Entity Framework Core.
/// </summary>
public class Repository : IRepository
{
    private readonly DbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository"/> class.
    /// </summary>
    /// <param name="context">The database context to be used for data operations.</param>
    public Repository(DbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Inserts a new entity into the database, with an optional check for duplicates.
    /// </summary>
    /// <typeparam name="T">The type of the entity being created.</typeparam>
    /// <param name="entity">The entity instance to be added.</param>
    /// <param name="duplicateCheck">
    /// An optional expression to define what constitutes a duplicate.
    /// If an entity matching this expression already exists, the creation is skipped.
    /// </param>
    /// <returns>The created entity if successful; <c>null</c> if a duplicate was found.</returns>
    public async Task<T> CreateAsync<T>(T entity, Expression<Func<T, bool>> duplicateCheck = null) where T : class
    {
        // Check for duplicates
        if (duplicateCheck != null && await _context.Set<T>().AnyAsync(duplicateCheck))
        {
            return null;
        }

        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }


    /// <summary>
    /// Retrieves a collection of entities based on provided filters and navigation property includes.
    /// </summary>
    /// <typeparam name="T">The type of entities to retrieve.</typeparam>
    /// <param name="filters">An optional array of filter expressions to apply to the query.</param>
    /// <param name="includes">An optional array of lambda expressions for related entities to include (eager loading).</param>
    /// <param name="includeStrings">An optional array of string paths for nested or complex related entities to include.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> containing the matching entities.</returns>
    public async Task<IEnumerable<T>> ReadAsync<T>(
        Expression<Func<T, bool>>[] filters = null,
        Expression<Func<T, object>>[] includes = null,
        string[] includeStrings = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        bool randomize = false,
        int? skip = null,
        int? take = null,
        bool tracked = true
    ) where T : class
    {
        IQueryable<T> query = tracked ? _context.Set<T>() : _context.Set<T>().AsNoTracking();

        if (includes != null)
            foreach (var include in includes)
                query = query.Include(include);

        if (includeStrings != null)
            foreach (var include in includeStrings)
                query = query.Include(include);

        if (filters != null)
            foreach (var filter in filters)
                query = query.Where(filter);

        IQueryable<T> ordered = randomize
            ? query.OrderBy(_ => EF.Functions.Random())
            : orderBy != null ? orderBy(query) : query;

        if (skip.HasValue)
            ordered = ordered.Skip(skip.Value);

        if (take.HasValue)
            ordered = ordered.Take(take.Value);

        return await ordered.ToListAsync();
    }

    public async Task<int> CountAsync<T>(Expression<Func<T, bool>>[] filters = null) where T : class
    {
        IQueryable<T> query = _context.Set<T>();

        if (filters != null)
            foreach (var filter in filters)
                query = query.Where(filter);

        return await query.CountAsync();
    }

    /// <summary>
    /// Finds an entity by its primary key(s).
    /// </summary>
    /// <typeparam name="T">The type of the entity to find.</typeparam>
    /// <param name="id">The primary key value(s) of the entity.</param>
    /// <returns>The entity if found; <c>null</c> otherwise.</returns>
    public async Task<T> ReadByIdAsync<T>(
        object id,
        Expression<Func<T, object>>[] includes = null,
        string[] includeStrings = null,
        bool tracked = true
    ) where T : class
    {
        if (includes == null && includeStrings == null)
            return await _context.Set<T>().FindAsync(id);

        IQueryable<T> query = tracked ? _context.Set<T>() : _context.Set<T>().AsNoTracking();

        if (includes != null)
            foreach (var include in includes)
                query = query.Include(include);

        if (includeStrings != null)
            foreach (var include in includeStrings)
                query = query.Include(include);

        var keyName = _context.Model.FindEntityType(typeof(T))!
            .FindPrimaryKey()!.Properties[0].Name;

        var parameter = Expression.Parameter(typeof(T));
        var property = Expression.Property(parameter, keyName);
        var constant = Expression.Constant(Convert.ChangeType(id, property.Type));
        var lambda = Expression.Lambda<Func<T, bool>>(Expression.Equal(property, constant), parameter);

        return await query.SingleOrDefaultAsync(lambda);
    }

    /// <summary>
    /// Retrieves a single entity based on provided filters and navigation property includes.
    /// The SingleOrDefaultAsync is executed on the IQueryable, so it translates to a SQL TOP 1 query.
    /// </summary>
    public async Task<T> ReadSingleAsync<T>(
        Expression<Func<T, bool>>[] filters = null,
        Expression<Func<T, object>>[] includes = null,
        string[] includeStrings = null,
        bool tracked = true
    ) where T : class
    {
        IQueryable<T> query = tracked ? _context.Set<T>() : _context.Set<T>().AsNoTracking();

        if (includes != null)
        {
            foreach (var include in includes)
                query = query.Include(include);
        }

        if (includeStrings != null)
        {
            foreach (var include in includeStrings)
                query = query.Include(include);
        }

        if (filters != null)
        {
            foreach (var filter in filters)
                query = query.Where(filter);
        }

        return await query.SingleOrDefaultAsync();
    }

    public async Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> filter) where T : class
    {
        return await _context.Set<T>().AnyAsync(filter);
    }

    /// <summary>
    /// Updates an existing entity in the database.
    /// </summary>
    /// <typeparam name="T">The type of the entity being updated.</typeparam>
    /// <param name="entity">The entity instance with updated values.</param>
    /// <returns><c>true</c> if the update was successful; <c>false</c> if an error occurred (logged).</returns>
    public async Task UpdateAsync<T>(T entity) where T : class
    {
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes the first entity that matches the specified filter.
    /// </summary>
    /// <typeparam name="T">The type of the entity to delete.</typeparam>
    /// <param name="filter">The expression used to locate the entity for deletion.</param>
    /// <returns><c>true</c> if the entity was found and successfully deleted; <c>false</c> if the entity does not exist.</returns>
    public async Task<bool> DeleteAsync<T>(Expression<Func<T, bool>> filter) where T : class
    {
        var entity = await _context.Set<T>().SingleOrDefaultAsync(filter);
        if (entity == null)
        {
            return false;
        }

        _context.Set<T>().Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Deletes an entity by its primary key(s).
    /// </summary>
    /// <typeparam name="T">The type of the entity to delete.</typeparam>
    /// <param name="id">The primary key value(s) of the entity.</param>
    /// <returns><c>true</c> if the entity was found and successfully deleted; <c>false</c> if the entity does not exist.</returns>
    public async Task<bool> DeleteByIdAsync<T>(params object[] id) where T : class
    {
        var entity = await _context.Set<T>().FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        _context.Set<T>().Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
}