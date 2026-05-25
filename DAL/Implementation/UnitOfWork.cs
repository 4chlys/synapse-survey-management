using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly DbContext _context;
    private IDbContextTransaction _tx;

    public UnitOfWork(DbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_tx != null) throw new InvalidOperationException("Transaction already active");
        _tx = await _context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_tx == null) throw new InvalidOperationException("No active transaction");
        await _context.SaveChangesAsync(ct);
        await _tx.CommitAsync(ct);
        await _tx.DisposeAsync();
        _tx = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_tx == null) return;
        await _tx.RollbackAsync(ct);
        await _tx.DisposeAsync();
        _tx = null;
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct = default)
    {
        await ExecuteInTransactionAsync<object>(async innerCt =>
        {
            await work(innerCt);
            return null;
        }, ct);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct = default)
    {
        await BeginTransactionAsync(ct);
        try
        {
            var result = await work(ct);
            await CommitAsync(ct);
            return result;
        }
        catch
        {
            await RollbackAsync(ct);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_tx != null) await _tx.DisposeAsync();
    }
}
