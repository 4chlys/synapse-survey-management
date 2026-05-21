# Architecture

This document describes the architecture of **SynapseEnqueteManagement**.

## 1. Overview

SynapseEnqueteManagement is a survey and feedback platform that collects input from young
people through questionnaires, free-form idea submissions, and comments. Project owners
configure a project (questionnaire, idea categories, theming, supported languages); youth
participants then answer questions, submit ideas, comment on each other's ideas, and react
with emoji. AI assistance supports the platform throughout — moderating text and images,
helping participants phrase ideas and comments, transcribing speech, and ordering ideas by
semantic similarity. The system is built as a classic **N-layer architecture** because the
project is developed and maintained by a student team: strict layer separation keeps
business rules in one predictable place, lets each layer be tested in isolation, and makes
the boundaries explicit enough that team members can work on different layers without
stepping on each other.

## 2. Layer diagram

The solution `SynapseEnqueteManagement.sln` contains nine projects: six implementation
projects grouped into **four architectural layers**, plus three test projects
(`DAL.Tests`, `BL.Tests`, `API.Tests`). The dependency direction is strictly downward.
The domain model is **part of the Business Logic layer**, not a layer of its own: it lives
in a dedicated `Domain` project — whose root namespace is `SynapseEnqueteManagement.BL.Domain`
— purely so the `DAL` can reference the entity types it persists. It carries no logic.

```
┌──────────────────────────────────────────────────────────────────┐
│ Presentation     UI-User (participants)   UI-Admin (project mgmt) │
│                  ASP.NET MVC + TypeScript / ASP.NET MVC           │
└───────────────────────────────┬──────────────────────────────────┘
                                │  HTTP / REST  (X-Api-Key, JSON)
┌───────────────────────────────▼──────────────────────────────────┐
│ Application      API — ASP.NET Core Web API                       │
│                  Thin controllers, DTOs, middleware               │
└───────────────────────────────┬──────────────────────────────────┘
┌───────────────────────────────▼──────────────────────────────────┐
│ Business Logic   BL      — IManager / Manager, AI services        │
│                  Domain  — Entities, Enums, Interfaces (no logic) │
└───────────────────────────────┬──────────────────────────────────┘
┌───────────────────────────────▼──────────────────────────────────┐
│ Data Access      DAL — Repository<T>, DbContext, UnitOfWork       │
│                  (references the Domain model to persist it)      │
└────────────────────────────────────────────────────────────────────┘
```

| Layer | Responsibility | May NOT do |
|-------|----------------|------------|
| **Presentation** (`UI-User`, `UI-Admin`) | UI-User serves the participant frontend; UI-Admin the project-management panel. They consume the API over HTTP/REST. | Contain business logic; they call the API, not the DAL. |
| **Application** (`API`) | HTTP host: thin controllers, DTOs, middleware, status codes. | Contain business logic; controllers only translate HTTP ⇄ BL. |
| **Business Logic** (`BL` + `Domain`) | All business logic — `IManager`/`Manager` classes, AI services, moderation, validation (`BL`) — and the **domain model**: entities, enums, shared interfaces (`Domain`). | Managers may not touch the `DbContext` or build EF queries; domain entities may not contain logic or relationship-mapping attributes. |
| **Data Access** (`DAL`) | Persistence: `Repository<T>`, per-entity repositories, `SynapseEnqueteContext`, `UnitOfWork`, seeding. All `.Include()` logic. | Contain business rules or decide *when* to save. |

`UI-User` and `UI-Admin` are separate processes. They reach the `API` over HTTP/REST
(authenticated with an `X-Api-Key` header); the `API` process hosts `BL` and `DAL` directly
through dependency injection.

## 3. The domain model

The domain model is **part of the Business Logic layer**, not an architectural layer of its
own. It is kept in a separate `Domain` project (`Domain/Domain.csproj`) for one practical
reason: the `DAL` must reference the entity types in order to persist them, and a separate
project lets it do so without depending on business logic. The project holds only **plain
C# classes** — no logic, no methods, no references to other layers. An entity is a pure
data shape; behaviour lives in the `BL` project alongside it.

**`{Entity}Id` primary key convention.** Every entity exposes its key as `{TypeName}Id`,
e.g. `Idea.IdeaId`, `Project.ProjectId`. This convention is load-bearing: the generic
`Repository<T>.DeleteByIdAsync` reconstructs the key name as `typeof(T).Name + "Id"`.

**`IHasCreatedAt`.** Entities that carry a creation timestamp implement the marker
interface `Domain/IHasCreatedAt.cs`, which declares a single `DateTime CreatedAt` property.
`Idea` and `Project` both implement it.

**Navigation-only foreign keys.** Entities expose *navigation properties* (object
references), never raw foreign-key integers. `Idea` references `Category Category` and
`Participant Participant`, not a `CategoryId` field:

```csharp
public class Idea : IHasCreatedAt
{
    public int IdeaId { get; set; }
    public ModerationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    [Required] [MaxLength(1000)] public string Description { get; set; }
    public float[] Embedding { get; set; }
    [Required] public Category Category { get; set; }
    [Required] public Participant Participant { get; set; }
    public Language Language { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
```

The integer FK columns (`CategoryId`, `IdeaParticipantId`, …) exist only as **shadow
properties** configured in the `DbContext` (see §4). This keeps the domain model clean: an
entity describes *what it is related to*, not *how the database stores that relation*.

**No lazy loading.** EF Core lazy loading is not enabled. In practice this means a
navigation property is `null` unless it was explicitly requested with `.Include()`. The
benefit is predictability: there are no hidden database round-trips triggered by reading a
property, and every query the application issues is visible in the repository method that
built it. The cost — that callers must say exactly which related data they need — is
deliberately pushed into the repositories, where it is centralised.

## 4. Data Access layer

The `DAL` project owns everything about persistence.

**Generic `Repository<T>` base.** `DAL/Implementation/Repository.cs` provides the small set
of operations that are identical for every entity — create, read-by-id, read-all, update,
delete:

```csharp
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
    // … ReadAllReadOnlyAsync, Update, DeleteByIdAsync
}
```

**Per-entity repositories.** Each aggregate has its own repository — `IIdeaRepository`,
`IProjectRepository`, `ICategoryRepository`, … — that inherits `Repository<T>` and adds
**intent-named query methods**. `IdeaRepository` (`DAL/Implementation/IdeaRepository.cs`) is
the reference implementation. Method names state the use case, not the SQL:

```csharp
Task<Idea> ReadByIdWithProjectForUpdateAsync(int ideaId);
Task<Idea> ReadByIdWithCommentsAndParticipantReadOnlyAsync(int ideaId);
Task<int> CountVisibleByCategoryAsync(int categoryId, string filterLanguageCode = null);
```

**Naming conventions.** Two suffixes communicate change-tracking behaviour:

- `…ReadOnlyAsync` → the query uses `AsNoTracking()`; results are read-only and cheaper.
- `…ForUpdateAsync` → the query returns **tracked** entities, so the caller may mutate them
  and have `SaveChanges` persist the change.
- Count methods carry no suffix — they are inherently read-only.

**All `.Include()` logic lives in the repository.** Managers never write `.Include()`. The
`IdeaRepository` keeps a private `WithComments` helper that composes the nested includes
once and reuses it across query methods. A manager that needs an idea-with-comments simply
calls `ReadByIdWithCommentsAndParticipantReadOnlyAsync` — the eager-loading shape is a
repository decision, not a manager concern.

**`UnitOfWork`.** `DAL/Implementation/UnitOfWork.cs` wraps the `DbContext`'s
`SaveChangesAsync` and transaction lifecycle. It is used whenever a single business
operation requires **multiple database operations** to succeed or fail together.
`ExecuteInTransactionAsync` runs a delegate inside a transaction, committing on success and
rolling back on any exception:

```csharp
public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> work, …)
{
    await BeginTransactionAsync(ct);
    try { var result = await work(ct); await CommitAsync(ct); return result; }
    catch { await RollbackAsync(ct); throw; }
}
```

For a simple single-write operation a manager can just call `uow.SaveChangesAsync` directly
(as `IdeaManager.AddIdeaAsync` does); the explicit transaction is reserved for multi-step
work and for set-based deletes that bypass the change tracker.

**`SynapseEnqueteContext` and shadow FKs.** `DAL/SynapseEnqueteContext.cs` configures every
relationship in `OnModelCreating`. Because the domain entities have no integer FK
properties, the context declares the foreign keys as **shadow properties** via the string
overload of `HasForeignKey`:

```csharp
e.HasMany(c => c.Ideas)
    .WithOne(i => i.Category)
    .HasForeignKey("CategoryId")          // shadow int — not on the Idea class
    .OnDelete(DeleteBehavior.Cascade);
```

The context also adapts to the runtime database: an `Idea.Embedding` (a `float[]`
similarity vector) is mapped to a native `vector(3072)` column on PostgreSQL via `pgvector`,
and to JSON text on SQLite. This is the single place where the dual-database support is
expressed.

## 5. Business Logic layer

`BL` holds **all** business logic. Controllers and the DAL contain none.

**The `IManager` / `Manager` pattern.** Each piece of business logic is exposed through an
interface (`BL/Interfaces/IIdeaManager.cs`) and implemented by a class
(`BL/Implementation/IdeaManager.cs`). Controllers depend on the interface, never the
concrete class, which makes managers trivially mockable in tests.

**Regular managers vs coordinator managers.** There are two kinds of manager, and the
distinction is about ownership of data:

- A **regular manager** owns one aggregate and is injected with **its own repository**.
  `IdeaManager` owns `Idea` and receives `IIdeaRepository`:

  ```csharp
  public class IdeaManager(
      IIdeaRepository ideaRepository,
      ICategoryRepository categoryRepository,   // justified cross-aggregate dependency
      IUnitOfWork uow,
      ILogger<IdeaManager> logger,
      ISortStrategyFactory<Idea> sortStrategyFactory,
      IEmbeddingSortStrategyFactory embeddingSortFactory,
      IEmbeddingService embeddingService) : IIdeaManager
  ```

- A **coordinator manager** owns *no* aggregate and *no* repository. It composes other
  managers to drive a multi-step use case. `SubmissionManager`, `SurveyFlowManager`, and
  `ParticipantLanguageCoordinator` are coordinators — for example:

  ```csharp
  public class SubmissionManager(
      IQuestionnaireManager questionnaireManager,
      ISurveyFlowManager surveyFlowManager,
      IResponseManager responseManager,
      IParticipantManager participantManager) : ISubmissionManager
  ```

  `SubmissionManager.SubmitAsync` orchestrates four managers to validate and commit a
  questionnaire submission.

The distinction exists to keep dependency graphs acyclic and reasoning local: regular
managers stay focused on one aggregate and do not inject other managers; cross-feature
*navigation* is lifted into a thin coordinator that has nothing to persist of its own.

**Justified cross-aggregate repository injections.** A regular manager normally injects
only its own repository. A few managers need a *foreign* repository to enforce an invariant
that spans aggregates. These exceptions are explicitly enumerated and may not be extended
silently:

- `CommentManager` → `IIdeaRepository` — parent-idea invariant.
- `IdeaManager` → `ICategoryRepository` — parent-category invariant and `RequestContactInfo`.
- `ResponseManager` → `IQuestionRepository` — validation needs the `Question`.
- `ProjectManager` → `IEmojiRepository` — `Emoji` has no manager of its own.
- `QuestionnaireManager` → `IQuestionRepository`, `ISectionRepository` — `Question` and
  `Section` are children of the `Questionnaire` aggregate.
- `ParticipantManager` → `ILanguageRepository` — language validation on creation/switch.
- `ProjectManager` → `ILanguageRepository` — translation-language validation.
- `CategoryManager` → `ILanguageRepository` — translation-language validation.
- `QuestionnaireManager` → `ILanguageRepository` — translation-language validation.

**No generic `IRepository<T>` in BL.** Managers never inject the generic `IRepository<T>`.
They depend on the *specific* repository interface (`IIdeaRepository`, …), so the intent-
named query methods are always available and the dependency is explicit.

**Managers receive parent entities, not IDs.** When a manager needs a parent entity or a
configuration value, the controller resolves it and passes the **object** (or scalar). For
example `IdeaManager.AddIdeaAsync` takes a `Participant participant` argument, and
`IdeasController` passes a fully-loaded `Participant` rather than a `participantId`. This
keeps the manager from re-fetching data the controller already has and makes the
data-loading boundary explicit.

**`PagedResult<T>` for paged queries.** `BL/PagedResult.cs` is a generic record returned by
every paged query. It carries `Items`, `Page`, `PageSize`, `TotalCount`, and a computed
`TotalPages`. `IdeaManager` uses it internally (`RegularSortAsync`) and the API echoes the
same shape back to clients.

## 6. API layer

The `API` project is an ASP.NET Core Web API that hosts `BL` and `DAL` and exposes them
over HTTP.

**Thin controllers.** A controller's only job is to translate between HTTP and `BL`: read
the request, call a manager, map the result to a DTO, return the right status code. There
is no business logic in a controller. `IdeasController.GetIdeasByCategory` is typical — it
resolves the participant, calls `ideaManager.GetIdeasIndexAsync`, and projects the result
into a DTO:

```csharp
[HttpGet]
public async Task<ActionResult<IdeaIndexResponseDto>> GetIdeasByCategory(
    int projectId, int categoryId, [FromQuery] int page = 1,
    [FromQuery] string sort = null, [FromQuery] bool? showAllLanguages = null,
    CancellationToken ct = default)
{
    var (participantId, participantLanguageCode) = await ResolveParticipantWithLanguageAsync();
    var project = await GetProjectByIdReadOnlyAsync(projectId);
    var pageSize = project?.IdeaPageSize ?? 5;
    var result = await ideaManager.GetIdeasIndexAsync(categoryId, projectId, participantId,
        pageSize, page, sort, participantLanguageCode, showAllLanguages ?? false, ct);
    return Ok(new IdeaIndexResponseDto { /* … map result … */ });
}
```

**DTOs as records.** The API never returns domain entities. Every request and response
body is a dedicated DTO defined under `API/DTOs/` as a C# `record`. Records give value
semantics and concise immutable shapes; collection properties use `ICollection<T>` to match
the domain convention. DTO mapping happens in the controller (e.g. `MapIdeaToDto`).

**HTTP status codes.** Controllers return semantically correct status codes: `200 OK` for a
successful read, `201 Created` for a successful create (`CreateIdea` returns `Created(...)`),
`400 Bad Request` for invalid input, `401 Unauthorized` for a missing participant token,
`403 Forbidden` for a project mismatch, `404 Not Found` for missing resources, and
`429 Too Many Requests` for rate-limited AI endpoints.

**Participant identity.** Participants are not authenticated with ASP.NET Identity. Each
participant is identified by a `SessionToken` (a GUID). The API reads it from the
`X-Participant-Token` header; `ParticipantControllerBase` resolves the `Participant` from
that token and is the base class for participant-facing controllers. The `X-Api-Key` header
(checked by `ApiKeyMiddleware`) authenticates the *caller* — the UI-User/UI-Admin process —
not the participant.

**Moderation token flow.** When a participant submits user-generated text or an image, the
content is first moderated (by AI, regex, or banned-word checks). The moderation result is
returned to the client as an **HMAC-signed token** produced by `ModerationTokenService`
(`BL/Implementation/ModerationTokenService.cs`). The token encodes the SHA-256 hash of the
moderated content, the resulting `ModerationStatus`, the `projectId`, and a UTC timestamp,
all signed with the shared `ApiKey` secret. On the subsequent create request the API
re-validates the token: it checks the HMAC signature, that the project matches, that the
content hash still matches the submitted text, that the token is under ten minutes old, and
that it has not already been consumed. Because the token is self-contained and signed, the
API can trust a moderation decision without shared server-side state — the verification is
stateless across instances, which matters for horizontally-scaled deployment.

## 7. Frontend (UI-User)

`UI-User` is a hybrid: an **ASP.NET MVC** application that renders pages server-side, with a
**Vite-built TypeScript** bundle for client-side interactivity.

**Single entry point.** All client-side TypeScript flows through one entry file,
`Assets/main.ts`, whose only responsibility is to import and initialise the other modules.

**No JavaScript framework.** There is no React, Vue, or jQuery. The frontend is **vanilla
TypeScript**, organised into modules, compiled in strict mode. There is no inline
JavaScript or inline CSS.

**Styling.** Styling uses **Tailwind CSS v4** utility classes. Site-wide theme values
(colours, etc.) are expressed as CSS custom properties so that per-project theming — a
project's `PrimaryColor`, `Theme`, and `CustomCss` — can override them.

**Vite ⇄ .NET integration.** The MVC project integrates the Vite build via the
`Vite.AspNetCore` package. In `UI-User/Program.cs` the Vite services are registered with
`builder.Services.AddViteServices()`, and in development `app.UseViteDevelopmentServer()`
proxies asset requests to the Vite dev server. The recommended development workflow is to
run `dotnet run` and `pnpm dev` concurrently inside `UI-User/`. A production build
(`pnpm build`) compiles the bundle into `wwwroot/dist/`, which is then served as static
files; files there are generated and must not be edited directly.

**Communication with the API.** `UI-User` does not access the `DAL` or `BL` directly. It
calls the `API` over HTTP through typed `HttpClient` service classes (`IdeaService`,
`CategoryService`, `QuestionnaireApiService`, …) registered in `Program.cs`, each
configured with the API base URL and the `X-Api-Key` header.

## 8. Key architectural rules

A reviewer should know these hard rules; they are enforced consistently across the codebase:

- **N-layer dependency direction is strictly downward** — every layer may reference only
  the layer(s) below it; nothing references the presentation layer.
- **Domain entities contain no logic** — plain classes with `{Entity}Id` primary keys and
  navigation properties only.
- **Foreign keys are shadow properties** — declared in the `DbContext`, never on entities.
- **No lazy loading** — related data is loaded only via explicit `.Include()`.
- **All `.Include()` logic lives in repositories**, never in managers.
- **Repository method naming is meaningful** — `ReadOnly` ⇒ `AsNoTracking`,
  `ForUpdate` ⇒ tracked.
- **Controllers hold no business logic** — logic lives in `BL` managers.
- **BL never touches the `DbContext`** and never injects the generic `IRepository<T>`.
- **Regular managers inject only their own repository**, except for the explicitly
  enumerated cross-aggregate exceptions.
- **Coordinator managers own no repository** and compose other managers.
- **Controllers pass entities or scalars to managers, not IDs**, when a manager needs a
  parent entity or a config value.
- **The Unit of Work is used when a business operation spans multiple DB operations.**
- **DTOs (records) are the only thing crossing the API boundary** — domain entities are
  never serialised to clients.
- **The frontend is vanilla TypeScript** — no JS framework, no inline JS/CSS, strict mode.

## 9. Why this architecture?

N-layer architecture solves three concrete problems for this project.

**Maintainability across a student team.** The project is built by a rotating student team.
Strict layers mean every contributor knows where a given concern belongs: a query goes in a
repository, a rule goes in a manager, an HTTP concern goes in a controller. The explicitly
enumerated exceptions (cross-aggregate injections) and naming conventions remove the need
to re-decide these things per feature, so the codebase stays consistent even as authors
change.

**Testability.** Because every manager is reached through an interface (`IIdeaManager`, …)
and never instantiates its own dependencies, each layer can be tested in isolation with
mocked collaborators. The solution ships dedicated test projects for exactly this —
`DAL.Tests`, `BL.Tests`, `API.Tests` — one per layer that carries logic.

**A clean, swappable database boundary.** All persistence detail is confined to the `DAL`.
The application runs on **SQLite in development** and **PostgreSQL in production**, and the
only place that knows the difference is `SynapseEnqueteContext` (for example, the dual
mapping of the `Idea.Embedding` vector). Because `BL` depends on repository *interfaces* and
never on EF Core, and controllers depend on manager interfaces, the database engine can be
changed without touching a single line of business logic or any controller.
