using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace SynapseEnqueteManagement.DAL.Tests.Integration;

/// <summary>
/// Per-test in-memory SQLite database. A single open connection is kept alive so
/// the schema persists for the lifetime of the test; each call to <see cref="NewContext"/>
/// returns a fresh <see cref="DbContext"/> against that same database — useful to
/// prove that changes made through one context are (or are not) visible to another.
/// </summary>
public sealed class InMemoryDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<SynapseEnqueteContext> _options;

    public InMemoryDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<SynapseEnqueteContext>()
            .UseSqlite(_connection)
            .Options;

        using var seed = new SynapseEnqueteContext(_options);
        seed.Database.EnsureCreated();
    }

    public SynapseEnqueteContext NewContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
