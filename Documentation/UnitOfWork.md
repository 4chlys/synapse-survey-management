# Unit of Work — why we use it

This document explains the persistence-coordination pattern behind every database write in this
codebase.

## TL;DR

We use a **Unit of Work** (`IUnitOfWork`) so that the business layer — not the repositories — owns
the transaction boundary. Repositories *stage* changes; they never commit. A manager decides when a
logical operation is complete and calls `SaveChangesAsync` **once**, so every staged insert, update,
and delete across every repository it touched commits atomically or not at all. That is a hard
requirement for an N-layer design where one use case (e.g. "create an idea and flag the category")
spans multiple repositories but must still be all-or-nothing.

## What is a Unit of Work

A Unit of Work is an object that tracks a set of changes you intend to make to the database and
applies them as a single, atomic batch. It answers one question for the rest of the application:
*"when does this set of changes become permanent?"*

In our codebase it is a small DAL-owned interface:

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);

    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct = default);
}
```

It exposes **two modes**, and the distinction is the whole point of the pattern:

| Mode | Method | Use when |
|---|---|---|
| **Implicit transaction** | `SaveChangesAsync` | The work is change-tracked (inserts / updates via `CreateAsync`, `Update`). EF Core wraps a single `SaveChanges` in its own transaction, so one call commits everything staged. |
| **Explicit transaction** | `ExecuteInTransactionAsync` | The work issues SQL that **bypasses** the change tracker (`ExecuteDeleteAsync` / `ExecuteUpdateAsync`). These run immediately and need an explicit `BEGIN … COMMIT` to remain rollback-able. |

## Why not the obvious alternatives

### Calling `SaveChanges()` inside each repository method — uncomposable

The naive repository commits its own work: `CreateAsync` would add *and* save. This looks convenient
until one manager method needs two writes. `AddIdeaAsync` creates an `Idea` and may touch the parent
`Category`; if each repository saved independently you would get two separate transactions, with a
window where the idea exists but the category change was lost to a crash.

A repository that commits cannot be composed. Moving the commit out — into the Unit of Work, called
by the manager — means the manager controls the boundary: stage from any number of repositories,
then one `SaveChangesAsync`, all-or-nothing.

### Injecting `DbContext` straight into managers — leaks EF into the business layer

A manager *could* take `DbContext` and call `_context.SaveChangesAsync()` directly. That re-couples
the business layer to Entity Framework — the exact dependency the repository abstraction exists to
hide. `IUnitOfWork` is the DAL-owned seam that lets a manager say "commit now" without ever naming
EF Core. The business layer depends on `DAL.Interfaces`, never on `Microsoft.EntityFrameworkCore`.

This is the architectural rule made concrete:
> Unit of Work is applied when business logic requires multiple DB operations.

### One transaction per repository call — too many round-trips, no atomicity

Even ignoring composition, per-call commits mean a manager that stages five changes pays for five
transactions. Staging into the shared change tracker and flushing once is both atomic *and* a single
round-trip.

## How it works in this codebase

### Everything in one request shares one `DbContext`

`API/Program.cs` registers the context and the Unit of Work as **scoped**:

```csharp
builder.Services.AddDbContext<SynapseEnqueteContext>(/* … */);
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<SynapseEnqueteContext>());
// …
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

Scoped means **one instance per HTTP request**. Within a single request the Unit of Work, every
repository, and every manager all receive the *same* `DbContext`. That is what makes a single
`SaveChangesAsync` flush work staged by *different* repositories — they were all writing into one
shared change tracker the whole time.

> ⚠️ The shared scoped `DbContext` is **not thread-safe**. Never `Task.WhenAll` across repositories
> or managers inside one request — concurrent access to the same change tracker corrupts it. Await
> sequentially.

### Repositories stage; the Unit of Work commits

The generic `Repository<T>` never saves:

```csharp
public async Task<T> CreateAsync(T entity)
{
    await Context.Set<T>().AddAsync(entity);   // staged, not persisted
    return entity;
}

public void Update(T entity) => Context.Set<T>().Update(entity);   // staged, not persisted
```

So a manager method reads like: resolve the parent, build the entity, validate, **stage**, **commit**.

```csharp
// IdeaManager.AddIdeaAsync — trimmed
var created = await ideaRepository.CreateAsync(idea);   // staged
await uow.SaveChangesAsync(ct);                          // committed — one implicit transaction
```

For a pure update there is not even a `Create` — the entity was fetched tracked (`…ForUpdate`
repository methods return tracked entities), the manager mutates a property, and `SaveChangesAsync`
notices the change:

```csharp
// IdeaManager.RemoveIdeaImageAsync — trimmed
idea.ImageUrl = null;
await uow.SaveChangesAsync(ct);
```

### The delete case — why `ExecuteInTransactionAsync` exists

`Repository<T>.DeleteByIdAsync` does **not** stage a delete. It issues a `DELETE` immediately via
`ExecuteDeleteAsync`:

```csharp
// Issues a single DELETE outside the change tracker;
// callers must wrap in IUnitOfWork.ExecuteInTransactionAsync to preserve rollback.
public async Task<bool> DeleteByIdAsync(int id)
{
    var pkName = typeof(T).Name + "Id";
    var rows = await Context.Set<T>()
        .Where(e => EF.Property<int>(e, pkName) == id)
        .ExecuteDeleteAsync();
    return rows > 0;
}
```

`ExecuteDeleteAsync` runs the SQL the moment it is called — it does not wait for `SaveChanges`. On
its own it is therefore *outside* any unit of work: if a later step in the same manager method
throws, the row is already gone and cannot be rolled back.

The fix is to run the delete inside an **explicit** transaction. `ExecuteInTransactionAsync` opens a
`BEGIN`, runs the work, and `COMMIT`s on success / `ROLLBACK`s on any exception:

```csharp
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
```

Every delete in the business layer goes through it:

```csharp
// CategoryManager
public Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken ct = default)
    => uow.ExecuteInTransactionAsync(_ => categoryRepository.DeleteByIdAsync(categoryId), ct);
```

The same wrapper is used in `ResponseManager`, `QuestionnaireManager` (question delete, section
delete). The rule: **`ExecuteDelete` / `ExecuteUpdate` always go inside `ExecuteInTransactionAsync`.**

## Where the code lives

| File | Purpose |
|---|---|
| `DAL/Interfaces/IUnitOfWork.cs` | The abstraction the business layer depends on. The only persistence-control type BL is allowed to see. |
| `DAL/Implementation/UnitOfWork.cs` | Implementation. Wraps the scoped `DbContext`; owns the optional `IDbContextTransaction`. |
| `DAL/Implementation/Repository.cs` | Generic repository. `CreateAsync` / `Update` stage; `DeleteByIdAsync` issues immediate SQL and must be wrapped. |
| `API/Program.cs` | Scoped DI registration of `DbContext` and `IUnitOfWork` so both are shared per request. |
| `BL/Implementation/*Manager.cs` | Callers. Inject `IUnitOfWork uow`; call `SaveChangesAsync` after staging, `ExecuteInTransactionAsync` for deletes. |
| `DAL.Tests/Integration/UnitOfWorkTests.cs` | Verifies persist / commit / rollback semantics and the guard exceptions. |

Managers that inject `IUnitOfWork`: `IdeaManager`, `ProjectManager`, `QuestionnaireManager`,
`CategoryManager`, `CommentManager`, `ParticipantManager`, `ResponseManager`. Coordinator managers
(`SubmissionManager`, `SurveyFlowManager`) hold no repository and no Unit of Work — they compose
other managers, each of which commits its own work.

## The guard rails

`UnitOfWork` is deliberately strict about misuse, so a bug surfaces as an exception rather than as
silent data loss:

- `BeginTransactionAsync` throws `InvalidOperationException` if a transaction is already active —
  no accidental nesting.
- `CommitAsync` throws if there is no active transaction — you cannot commit nothing.
- `RollbackAsync` is a no-op with no active transaction — safe to call in a `catch` defensively.
- `DisposeAsync` disposes a still-open transaction, so an abandoned scope cannot leak a DB
  transaction.

These are each covered by a test in `UnitOfWorkTests.cs`.

## Should UI-Admin adopt this for the admin database?

**Recommendation: no.** Keep `UI-Admin` services injecting `AdminDbContext` directly. The pattern
would be over-engineering here (it would violate the YAGNI / KISS rules), for three
concrete reasons:

1. **The Unit of Work earns its keep by coordinating *multiple repositories*. UI-Admin has none.**
   The main app has a per-entity repository layer; the Unit of Work is the seam that lets a manager
   compose writes across several of them atomically. `UI-Admin` has no repository layer — services
   such as `PlatformAdminService` and `WidgetPreferenceService` hold `AdminDbContext` directly. There
   is nothing to coordinate.

2. **Every UI-Admin service method already *is* a unit of work.** Each method stages its work and
   ends with exactly one `db.SaveChangesAsync()`. A single `SaveChanges` is itself wrapped in one
   EF Core transaction — `SaveLayoutAsync`, for example, does `RemoveRange(existing)` + several
   `Add`s + one `SaveChangesAsync`, and that whole batch is already atomic. Adding `IUnitOfWork`
   would wrap a transaction in a transaction and change nothing.

3. **The one genuine multi-step risk is an Identity concern a generic UoW does not solve.**
   `PlatformAdminService.AddAdminAsync` does `userManager.CreateAsync` *then*
   `userManager.AddToRoleAsync` — two separate commits, so a failure on the second leaves an admin
   with no role. But `UserManager<T>` owns its own `SaveChanges` internally and does not enlist in
   an external transaction without extra plumbing. If that orphan-user gap matters, fix it with a
   targeted explicit transaction (or a compensating delete) *in that one method* — not by
   introducing a platform-wide abstraction.

There is also a structural reason the two databases differ: the main app DB is wiped and reseeded
on every version change, while the `UI-Admin` Identity database is migration-managed and persistent.
Their write patterns are simple CRUD; the heavy, multi-repository use cases all live behind the API
in the business layer. Put the abstraction where the complexity is.

If `UI-Admin` ever grows a repository layer or a use case that must write across `AdminDbContext`
*and* call back into the API atomically, revisit this — but that is a YAGNI bet for today.

## Briefing summary for non-technical readers

> The Unit of Work is the rule that says "a database change is only real once the *whole operation*
> succeeds." Our data-access objects only *prepare* changes; the business layer decides the moment
> they all commit together. If anything fails halfway, nothing is written. We apply it in the main
> platform, where one action can touch several tables at once. We deliberately do *not* apply it to
> the admin panel, where every action touches one table and is already safe on its own — adding it
> there would be needless machinery.

## References

- [Martin Fowler — Unit of Work](https://martinfowler.com/eaaCatalog/unitOfWork.html)
- [Microsoft — Using transactions in EF Core](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
- [Microsoft — `ExecuteDelete` / `ExecuteUpdate` (bulk operations)](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete)
</content>
</invoke>
