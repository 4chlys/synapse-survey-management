# Dashboard Widget System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the static project dashboard with a 3×3 modular widget grid that polls in real-time, renders charts via Chart.js, and lets each ProjectPlatformAdmin configure their own layout per project.

**Architecture:** A Razor shell page renders 9 `<div data-widget="...">` slots populated from a `UserWidgetPreference` table (in `AdminDbContext`). A TypeScript orchestrator (`dashboard.ts`) discovers these slots, instantiates the matching widget class, and polls each widget's dedicated API endpoint every 30 seconds. Widget preferences are stored per-user per-project in `AdminDbContext`; the default layout is applied when no preference row exists.

**Tech Stack:** .NET 9, EF Core (SQLite dev / Npgsql prod), ASP.NET Core MVC, TypeScript 5, Vite 6, Chart.js 4, Tailwind CSS 4, pnpm.

---

## File Map

**New API files:**
- Create: `API/Controllers/ProjectStatsController.cs` — three stat endpoints (timeline, idea feed, moderation queue)
- Create: `API/DTOs/Stats/TimelineDto.cs`
- Create: `API/DTOs/Stats/IdeaFeedDto.cs`
- Create: `API/DTOs/Stats/ModerationQueueDto.cs`

**New BL files:**
- Modify: `BL/Interfaces/IParticipantManager.cs` — add `GetTimelineAsync`
- Modify: `BL/Interfaces/ICategoryManager.cs` — add `GetIdeaFeedAsync`, `GetModerationQueueAsync`
- Modify: `BL/Implementation/ParticipantManager.cs` — implement `GetTimelineAsync`
- Modify: `BL/Implementation/CategoryManager.cs` — implement `GetIdeaFeedAsync`, `GetModerationQueueAsync`

**New DAL files:**
- Modify: `DAL/Interfaces/IParticipantRepository.cs` — add `ReadTimelineAsync`
- Modify: `DAL/Interfaces/ICategoryRepository.cs` — add `ReadIdeaFeedAsync`, `ReadModerationQueueAsync`
- Modify: `DAL/Implementation/ParticipantRepository.cs` — implement `ReadTimelineAsync`
- Modify: `DAL/Implementation/CategoryRepository.cs` — implement `ReadIdeaFeedAsync`, `ReadModerationQueueAsync`

**New Admin UI C# files:**
- Create: `UI-Admin/Data/UserWidgetPreference.cs` — preference entity
- Modify: `UI-Admin/Data/AdminDbContext.cs` — add `DbSet<UserWidgetPreference>`, configure
- Create: `UI-Admin/Migrations/<timestamp>_AddUserWidgetPreferences.cs` — EF migration
- Create: `UI-Admin/Services/Dashboard/WidgetPreferenceService.cs`
- Create: `UI-Admin/Models/Dashboard/ConfigureDashboardViewModel.cs`
- Modify: `UI-Admin/Models/Dashboard/DashboardViewModel.cs` — add `WidgetLayout` field
- Modify: `UI-Admin/Controllers/ProjectController.cs` — update `Dashboard` action, add `ConfigureDashboard` GET/POST
- Modify: `UI-Admin/Program.cs` — register `WidgetPreferenceService`

**New Admin UI view files:**
- Modify: `UI-Admin/Views/Project/Dashboard.cshtml` — 3×3 widget grid shell
- Create: `UI-Admin/Views/Project/ConfigureDashboard.cshtml`

**New TypeScript files (all in `UI-Admin/src/`):**
- Create: `UI-Admin/src/dashboard.ts` — orchestrator entry point (separate Vite bundle)
- Create: `UI-Admin/src/widget-stat.ts` — ViewCount / CompletionCount / CompletionRate cards
- Create: `UI-Admin/src/widget-timeline.ts` — Chart.js line chart (starts + completions per day)
- Create: `UI-Admin/src/widget-top-categories.ts` — Chart.js bar chart
- Create: `UI-Admin/src/widget-top-ideas.ts` — ranked list
- Create: `UI-Admin/src/widget-survey-distribution.ts` — QR + copy button (static, no polling)
- Create: `UI-Admin/src/widget-idea-feed.ts` — latest ideas list
- Create: `UI-Admin/src/widget-moderation-queue.ts` — status count card

---

## Task 1: Install Chart.js and scaffold TypeScript dashboard orchestrator

**Files:**
- Modify: `UI-Admin/package.json`
- Create: `UI-Admin/src/dashboard.ts`

- [ ] **Step 1: Install Chart.js**

Run from `UI-Admin/`:
```bash
cd UI-Admin && pnpm add chart.js
```
Expected: `chart.js` appears in `package.json` `dependencies`.

- [ ] **Step 2: Verify TypeScript can import Chart.js**

Chart.js 4 ships its own types — no `@types/chart.js` needed. Verify:
```bash
cd UI-Admin && pnpm exec tsc --noEmit
```
Expected: 0 errors (there are currently 0 errors, this should stay that way after adding the package).

- [ ] **Step 3: Create `UI-Admin/src/dashboard.ts` — orchestrator**

```typescript
import { initStatWidget } from './widget-stat';
import { initTimelineWidget } from './widget-timeline';
import { initTopCategoriesWidget } from './widget-top-categories';
import { initTopIdeasWidget } from './widget-top-ideas';
import { initSurveyDistributionWidget } from './widget-survey-distribution';
import { initIdeaFeedWidget } from './widget-idea-feed';
import { initModerationQueueWidget } from './widget-moderation-queue';

const POLL_INTERVAL_MS = 30_000;

type WidgetInit = (el: HTMLElement) => (() => void) | void;

const widgetRegistry: Record<string, WidgetInit> = {
    ViewCount: initStatWidget,
    CompletionCount: initStatWidget,
    CompletionRate: initStatWidget,
    ParticipationTimeline: initTimelineWidget,
    TopCategories: initTopCategoriesWidget,
    TopIdeas: initTopIdeasWidget,
    SurveyDistribution: initSurveyDistributionWidget,
    IdeaFeed: initIdeaFeedWidget,
    ModerationQueue: initModerationQueueWidget,
};

document.addEventListener('DOMContentLoaded', () => {
    const slots = document.querySelectorAll<HTMLElement>('[data-widget]');
    const cleanups: Array<() => void> = [];

    slots.forEach(el => {
        const type = el.dataset.widget ?? '';
        const init = widgetRegistry[type];
        if (!init) return;

        const cleanup = init(el);
        if (cleanup) cleanups.push(cleanup);
    });

    // Teardown on page unload (SPA navigation safety)
    window.addEventListener('beforeunload', () => cleanups.forEach(fn => fn()));
});

export { POLL_INTERVAL_MS };
```

- [ ] **Step 4: Create stub files for each widget so TypeScript compiles**

Create `UI-Admin/src/widget-stat.ts`:
```typescript
export function initStatWidget(_el: HTMLElement): void {
    // implemented in Task 2
}
```

Create `UI-Admin/src/widget-timeline.ts`:
```typescript
export function initTimelineWidget(_el: HTMLElement): void {}
```

Create `UI-Admin/src/widget-top-categories.ts`:
```typescript
export function initTopCategoriesWidget(_el: HTMLElement): void {}
```

Create `UI-Admin/src/widget-top-ideas.ts`:
```typescript
export function initTopIdeasWidget(_el: HTMLElement): void {}
```

Create `UI-Admin/src/widget-survey-distribution.ts`:
```typescript
export function initSurveyDistributionWidget(_el: HTMLElement): void {}
```

Create `UI-Admin/src/widget-idea-feed.ts`:
```typescript
export function initIdeaFeedWidget(_el: HTMLElement): void {}
```

Create `UI-Admin/src/widget-moderation-queue.ts`:
```typescript
export function initModerationQueueWidget(_el: HTMLElement): void {}
```

- [ ] **Step 5: Verify TypeScript compiles**

```bash
cd UI-Admin && pnpm exec tsc --noEmit
```
Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add UI-Admin/package.json UI-Admin/pnpm-lock.yaml UI-Admin/src/dashboard.ts UI-Admin/src/widget-*.ts
git commit -m "feat(dashboard): scaffold TypeScript widget orchestrator with Chart.js"
```

---

## Task 2: New API stat endpoints — timeline, idea feed, moderation queue

**Files:**
- Create: `API/DTOs/Stats/TimelineDto.cs`
- Create: `API/DTOs/Stats/IdeaFeedDto.cs`
- Create: `API/DTOs/Stats/ModerationQueueDto.cs`
- Create: `API/Controllers/ProjectStatsController.cs`
- Modify: `BL/Interfaces/IParticipantManager.cs`
- Modify: `BL/Interfaces/ICategoryManager.cs`
- Modify: `BL/Implementation/ParticipantManager.cs`
- Modify: `BL/Implementation/CategoryManager.cs`
- Modify: `DAL/Interfaces/IParticipantRepository.cs`
- Modify: `DAL/Interfaces/ICategoryRepository.cs`
- Modify: `DAL/Implementation/ParticipantRepository.cs`
- Modify: `DAL/Implementation/CategoryRepository.cs`

- [ ] **Step 1: Create DTOs**

Create `API/DTOs/Stats/TimelineDto.cs`:
```csharp
namespace SynapseEnqueteManagement.API.DTOs.Stats;

public class TimelineDto
{
    public List<string> Dates { get; set; } = new();
    public List<int> Starts { get; set; } = new();
    public List<int> Completions { get; set; } = new();
}
```

Create `API/DTOs/Stats/IdeaFeedDto.cs`:
```csharp
namespace SynapseEnqueteManagement.API.DTOs.Stats;

public class IdeaFeedItemDto
{
    public int IdeaId { get; set; }
    public string Description { get; set; }
    public string CategoryName { get; set; }
    public int ReactionCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

Create `API/DTOs/Stats/ModerationQueueDto.cs`:
```csharp
namespace SynapseEnqueteManagement.API.DTOs.Stats;

public class ModerationQueueDto
{
    public int Approved { get; set; }
    public int Declined { get; set; }
    public int Flagged { get; set; }
    public int Unflagged { get; set; }
}
```

- [ ] **Step 2: Add DAL interface methods**

In `DAL/Interfaces/IParticipantRepository.cs` add:
```csharp
Task<List<(DateTime Date, bool IsCompletion)>> ReadTimelineAsync(int projectId, int days);
```

In `DAL/Interfaces/ICategoryRepository.cs` add:
```csharp
Task<List<Idea>> ReadLatestIdeasAsync(int projectId, int count);
Task<Dictionary<ModerationStatus, int>> ReadModerationQueueAsync(int projectId);
```
Add `using SynapseEnqueteManagement.BL.Domain.Project.Enums;` to `ICategoryRepository.cs`.

- [ ] **Step 3: Implement DAL methods**

In `DAL/Implementation/ParticipantRepository.cs` add:
```csharp
public async Task<List<(DateTime Date, bool IsCompletion)>> ReadTimelineAsync(int projectId, int days)
{
    var cutoff = DateTime.UtcNow.AddDays(-days);
    var participants = await Context.Set<Participant>()
        .AsNoTracking()
        .Where(p => EF.Property<int>(p, "ProjectId") == projectId
                    && p.CreatedAt >= cutoff)
        .Select(p => new { p.CreatedAt, p.CompletedAt })
        .ToListAsync();

    var result = new List<(DateTime Date, bool IsCompletion)>();
    foreach (var p in participants)
    {
        result.Add((p.CreatedAt.Date, false));
        if (p.CompletedAt.HasValue)
            result.Add((p.CompletedAt.Value.Date, true));
    }
    return result;
}
```

In `DAL/Implementation/CategoryRepository.cs` add:
```csharp
public async Task<List<Idea>> ReadLatestIdeasAsync(int projectId, int count)
    => await Context.Set<Idea>()
        .AsNoTracking()
        .Where(i => i.Status == ModerationStatus.Approved
                    && i.Category.Project.ProjectId == projectId)
        .Include(i => i.Category)
        .Include(i => i.Comments)
        .OrderByDescending(i => i.CreatedAt)
        .Take(count)
        .ToListAsync();

public async Task<Dictionary<ModerationStatus, int>> ReadModerationQueueAsync(int projectId)
    => await Context.Set<Idea>()
        .AsNoTracking()
        .Where(i => i.Category.Project.ProjectId == projectId)
        .GroupBy(i => i.Status)
        .ToDictionaryAsync(g => g.Key, g => g.Count());
```
Add `using SynapseEnqueteManagement.BL.Domain.Project.Enums;` if not present.

- [ ] **Step 4: Add BL interface methods**

In `BL/Interfaces/IParticipantManager.cs` add:
```csharp
Task<TimelineResult> GetTimelineAsync(int projectId, int days = 30);
```

Add record `TimelineResult` to `BL/Interfaces/IParticipantManager.cs`:
```csharp
public record TimelineResult(List<string> Dates, List<int> Starts, List<int> Completions);
```

In `BL/Interfaces/ICategoryManager.cs` add:
```csharp
Task<List<IdeaFeedItem>> GetIdeaFeedAsync(int projectId, int count = 10);
Task<ModerationQueueResult> GetModerationQueueAsync(int projectId);
```

Add records to `BL/Interfaces/ICategoryManager.cs`:
```csharp
public record IdeaFeedItem(int IdeaId, string Description, string CategoryName, int ReactionCount, DateTime CreatedAt);
public record ModerationQueueResult(int Approved, int Declined, int Flagged, int Unflagged);
```

- [ ] **Step 5: Implement BL methods**

In `BL/Implementation/ParticipantManager.cs` add:
```csharp
public async Task<TimelineResult> GetTimelineAsync(int projectId, int days = 30)
{
    var events = await participantRepository.ReadTimelineAsync(projectId, days);
    var cutoff = DateTime.UtcNow.Date.AddDays(-days + 1);
    var allDates = Enumerable.Range(0, days)
        .Select(i => cutoff.AddDays(i))
        .ToList();

    var startsByDate = events.Where(e => !e.IsCompletion)
        .GroupBy(e => e.Date)
        .ToDictionary(g => g.Key, g => g.Count());

    var completionsByDate = events.Where(e => e.IsCompletion)
        .GroupBy(e => e.Date)
        .ToDictionary(g => g.Key, g => g.Count());

    return new TimelineResult(
        Dates: allDates.Select(d => d.ToString("yyyy-MM-dd")).ToList(),
        Starts: allDates.Select(d => startsByDate.GetValueOrDefault(d, 0)).ToList(),
        Completions: allDates.Select(d => completionsByDate.GetValueOrDefault(d, 0)).ToList()
    );
}
```
Add `using SynapseEnqueteManagement.BL.Interfaces;` if the `TimelineResult` record is defined there.

In `BL/Implementation/CategoryManager.cs` add:
```csharp
public async Task<List<IdeaFeedItem>> GetIdeaFeedAsync(int projectId, int count = 10)
{
    var ideas = await categoryRepository.ReadLatestIdeasAsync(projectId, count);
    return ideas.Select(i => new IdeaFeedItem(
        IdeaId: i.IdeaId,
        Description: i.Description.Length > 80 ? i.Description[..80] + "…" : i.Description,
        CategoryName: i.Category.Name,
        ReactionCount: i.Comments.OfType<IconComment>().Count(),
        CreatedAt: i.CreatedAt
    )).ToList();
}

public async Task<ModerationQueueResult> GetModerationQueueAsync(int projectId)
{
    var counts = await categoryRepository.ReadModerationQueueAsync(projectId);
    return new ModerationQueueResult(
        Approved: counts.GetValueOrDefault(ModerationStatus.Approved, 0),
        Declined: counts.GetValueOrDefault(ModerationStatus.Declined, 0),
        Flagged: counts.GetValueOrDefault(ModerationStatus.Flagged, 0),
        Unflagged: counts.GetValueOrDefault(ModerationStatus.Unflagged, 0)
    );
}
```
Add `using SynapseEnqueteManagement.BL.Domain.Project.Entities;` and `using SynapseEnqueteManagement.BL.Domain.Project.Enums;` if not present.

- [ ] **Step 6: Create `API/Controllers/ProjectStatsController.cs`**

```csharp
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Stats;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/stats")]
public class ProjectStatsController(
    IParticipantManager participantManager,
    ICategoryManager categoryManager,
    IProjectManager projectManager) : ControllerBase
{
    [HttpGet("timeline")]
    public async Task<ActionResult<TimelineDto>> GetTimeline(int projectId, [FromQuery] int days = 30)
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        var result = await participantManager.GetTimelineAsync(projectId, Math.Clamp(days, 7, 90));
        return Ok(new TimelineDto
        {
            Dates = result.Dates,
            Starts = result.Starts,
            Completions = result.Completions
        });
    }

    [HttpGet("idea-feed")]
    public async Task<ActionResult<List<IdeaFeedItemDto>>> GetIdeaFeed(int projectId, [FromQuery] int count = 10)
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        var items = await categoryManager.GetIdeaFeedAsync(projectId, Math.Clamp(count, 1, 20));
        return Ok(items.Select(i => new IdeaFeedItemDto
        {
            IdeaId = i.IdeaId,
            Description = i.Description,
            CategoryName = i.CategoryName,
            ReactionCount = i.ReactionCount,
            CreatedAt = i.CreatedAt
        }));
    }

    [HttpGet("moderation-queue")]
    public async Task<ActionResult<ModerationQueueDto>> GetModerationQueue(int projectId)
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        var result = await categoryManager.GetModerationQueueAsync(projectId);
        return Ok(new ModerationQueueDto
        {
            Approved = result.Approved,
            Declined = result.Declined,
            Flagged = result.Flagged,
            Unflagged = result.Unflagged
        });
    }
}
```

- [ ] **Step 7: Build and verify**

```bash
dotnet build --no-incremental 2>&1 | tail -5
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 8: Commit**

```bash
git add API/ BL/ DAL/
git commit -m "feat(stats): add timeline, idea-feed, and moderation-queue API endpoints"
```

---

## Task 3: UserWidgetPreference entity, AdminDbContext migration, WidgetPreferenceService

**Files:**
- Create: `UI-Admin/Data/UserWidgetPreference.cs`
- Modify: `UI-Admin/Data/AdminDbContext.cs`
- Create: `UI-Admin/Migrations/<timestamp>_AddUserWidgetPreferences.cs` (generated)
- Create: `UI-Admin/Services/Dashboard/WidgetPreferenceService.cs`
- Modify: `UI-Admin/Program.cs`

- [ ] **Step 1: Create `UI-Admin/Data/UserWidgetPreference.cs`**

```csharp
namespace SynapseEnqueteManagement.UI.Admin.Data;

public class UserWidgetPreference
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public int ProjectId { get; set; }
    public int Position { get; set; }
    public string WidgetType { get; set; }
}
```

- [ ] **Step 2: Add to `UI-Admin/Data/AdminDbContext.cs`**

In `AdminDbContext`, add DbSet after the existing `ProjectPlatforms` property:
```csharp
public DbSet<UserWidgetPreference> UserWidgetPreferences { get; set; }
```

In `OnModelCreating`, after the existing configuration blocks, add:
```csharp
builder.Entity<UserWidgetPreference>(e =>
{
    e.HasKey(p => p.Id);
    e.HasIndex(p => new { p.UserId, p.ProjectId, p.Position }).IsUnique();
    e.Property(p => p.UserId).IsRequired().HasMaxLength(450);
    e.Property(p => p.WidgetType).IsRequired().HasMaxLength(50);
});
```

- [ ] **Step 3: Generate and run migration**

From the worktree root:
```bash
cd UI-Admin && dotnet ef migrations add AddUserWidgetPreferences --context AdminDbContext
```
Expected: new migration file created in `UI-Admin/Migrations/`.

Verify the generated migration has `CreateTable` for `UserWidgetPreferences` with columns `Id`, `UserId`, `ProjectId`, `Position`, `WidgetType` and a unique index on `(UserId, ProjectId, Position)`.

- [ ] **Step 4: Create `UI-Admin/Services/Dashboard/WidgetPreferenceService.cs`**

The default layout (applied when no preference row exists) is:
position 0 → `ViewCount`, 1 → `CompletionCount`, 2 → `CompletionRate`,
3 → `ParticipationTimeline`, 4 → `TopIdeas`, 5 → `TopCategories`,
6 → `SurveyDistribution`, 7 → `IdeaFeed`, 8 → `ModerationQueue`

```csharp
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.UI.Admin.Data;

namespace SynapseEnqueteManagement.UI.Admin.Services.Dashboard;

public class WidgetPreferenceService(AdminDbContext db)
{
    public static readonly string[] DefaultLayout =
    [
        "ViewCount", "CompletionCount", "CompletionRate",
        "ParticipationTimeline", "TopIdeas", "TopCategories",
        "SurveyDistribution", "IdeaFeed", "ModerationQueue"
    ];

    public static readonly string[] AllWidgetTypes =
    [
        "ViewCount", "CompletionCount", "CompletionRate",
        "ParticipationTimeline", "TopCategories", "TopIdeas",
        "SurveyDistribution", "IdeaFeed", "ModerationQueue"
    ];

    public async Task<string[]> GetLayoutAsync(string userId, int projectId)
    {
        var prefs = await db.UserWidgetPreferences
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.ProjectId == projectId)
            .OrderBy(p => p.Position)
            .ToListAsync();

        if (prefs.Count == 0)
            return DefaultLayout;

        var layout = new string[9];
        foreach (var p in prefs)
            if (p.Position >= 0 && p.Position < 9)
                layout[p.Position] = p.WidgetType;

        // fill any gap with the default
        for (var i = 0; i < 9; i++)
            if (string.IsNullOrEmpty(layout[i]))
                layout[i] = DefaultLayout[i];

        return layout;
    }

    public async Task SaveLayoutAsync(string userId, int projectId, string[] widgetTypes)
    {
        var existing = await db.UserWidgetPreferences
            .Where(p => p.UserId == userId && p.ProjectId == projectId)
            .ToListAsync();

        db.UserWidgetPreferences.RemoveRange(existing);

        for (var i = 0; i < Math.Min(widgetTypes.Length, 9); i++)
        {
            db.UserWidgetPreferences.Add(new UserWidgetPreference
            {
                UserId = userId,
                ProjectId = projectId,
                Position = i,
                WidgetType = widgetTypes[i]
            });
        }

        await db.SaveChangesAsync();
    }
}
```

- [ ] **Step 5: Register in `UI-Admin/Program.cs`**

Find the block where other services are registered with `AddScoped` (around line 88). Add:
```csharp
builder.Services.AddScoped<WidgetPreferenceService>();
```
Add `using SynapseEnqueteManagement.UI.Admin.Services.Dashboard;` at the top if not already present via global usings.

- [ ] **Step 6: Build**

```bash
dotnet build --no-incremental 2>&1 | tail -5
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Commit**

```bash
git add UI-Admin/Data/UserWidgetPreference.cs UI-Admin/Data/AdminDbContext.cs UI-Admin/Migrations/ UI-Admin/Services/Dashboard/WidgetPreferenceService.cs UI-Admin/Program.cs
git commit -m "feat(dashboard): add UserWidgetPreference entity, migration, and WidgetPreferenceService"
```

---

## Task 4: Update DashboardViewModel and ProjectController.Dashboard action

**Files:**
- Modify: `UI-Admin/Models/Dashboard/DashboardViewModel.cs`
- Modify: `UI-Admin/Controllers/ProjectController.cs`

- [ ] **Step 1: Update `UI-Admin/Models/Dashboard/DashboardViewModel.cs`**

Add two new fields to the existing `DashboardViewModel` class:
```csharp
// Existing fields remain unchanged. Add:
public string[] WidgetLayout { get; set; } = WidgetPreferenceService.DefaultLayout;
public string ApiBaseUrl { get; set; }
```
Add `using SynapseEnqueteManagement.UI.Admin.Services.Dashboard;` at the top.

- [ ] **Step 2: Update `ProjectController` constructor**

The constructor already has `ProjectAdminService`, `QuestionAdminService`, `CategoryAdminService`, `SectionAdminService`, `IWebHostEnvironment`, `UserManager<ApplicationUser>`, `DashboardAdminService`. Add `WidgetPreferenceService widgetPreferenceService`:

```csharp
public class ProjectController(
    ProjectAdminService projectService,
    QuestionAdminService questionService,
    CategoryAdminService categoryService,
    SectionAdminService sectionService,
    IWebHostEnvironment env,
    UserManager<ApplicationUser> userManager,
    DashboardAdminService dashboardService,
    WidgetPreferenceService widgetPreferenceService) : Controller
```

- [ ] **Step 3: Update `ProjectController.Dashboard` action**

Replace the existing `Dashboard` action with:
```csharp
public async Task<IActionResult> Dashboard(int projectId)
{
    var user = await userManager.GetUserAsync(User);
    if (user is null) return RedirectToAction("Login", "Auth");

    var project = await projectService.GetProjectByIdAsync(projectId);
    if (project is null) return NotFound();

    var dashboard = await dashboardService.GetDashboardAsync(projectId, project.Name);
    var layout = await widgetPreferenceService.GetLayoutAsync(user.Id, projectId);

    dashboard.WidgetLayout = layout;
    dashboard.ApiBaseUrl = $"{Request.Scheme}://{Request.Host}";

    return View(dashboard);
}
```

- [ ] **Step 4: Add ConfigureDashboard actions to ProjectController**

Add after the `Dashboard` action:
```csharp
public async Task<IActionResult> ConfigureDashboard(int projectId)
{
    var user = await userManager.GetUserAsync(User);
    if (user is null) return RedirectToAction("Login", "Auth");

    var project = await projectService.GetProjectByIdAsync(projectId);
    if (project is null) return NotFound();

    var layout = await widgetPreferenceService.GetLayoutAsync(user.Id, projectId);
    return View(new ConfigureDashboardViewModel
    {
        ProjectId = projectId,
        ProjectName = project.Name,
        Layout = layout,
        AllWidgetTypes = WidgetPreferenceService.AllWidgetTypes
    });
}

[HttpPost]
public async Task<IActionResult> ConfigureDashboard(int projectId, ConfigureDashboardViewModel model)
{
    var user = await userManager.GetUserAsync(User);
    if (user is null) return RedirectToAction("Login", "Auth");

    var sanitized = (model.Layout ?? WidgetPreferenceService.DefaultLayout)
        .Take(9)
        .Select(w => WidgetPreferenceService.AllWidgetTypes.Contains(w) ? w : WidgetPreferenceService.DefaultLayout[0])
        .ToArray();

    await widgetPreferenceService.SaveLayoutAsync(user.Id, projectId, sanitized);
    return RedirectToAction(nameof(Dashboard), new { projectId });
}
```

- [ ] **Step 5: Create `UI-Admin/Models/Dashboard/ConfigureDashboardViewModel.cs`**

```csharp
namespace SynapseEnqueteManagement.UI.Admin.Models.Dashboard;

public class ConfigureDashboardViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; }
    public string[] Layout { get; set; } = [];
    public string[] AllWidgetTypes { get; set; } = [];
}
```

- [ ] **Step 6: Build**

```bash
dotnet build --no-incremental 2>&1 | tail -5
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Commit**

```bash
git add UI-Admin/Models/Dashboard/ UI-Admin/Controllers/ProjectController.cs
git commit -m "feat(dashboard): update DashboardViewModel and Dashboard action with widget layout"
```

---

## Task 5: Implement TypeScript stat, survey-distribution, and moderation-queue widgets

**Files:**
- Modify: `UI-Admin/src/widget-stat.ts`
- Modify: `UI-Admin/src/widget-survey-distribution.ts`
- Modify: `UI-Admin/src/widget-moderation-queue.ts`

Each widget receives an `HTMLElement` with:
- `data-widget` — widget type string
- `data-project-id` — project numeric ID
- `data-api-base` — API base URL (e.g. `https://localhost:5001`)
- `data-survey-url` — survey URL (only for SurveyDistribution widget)

The existing API endpoint `GET {apiBase}/api/projects/{id}/dashboard` returns `{ viewCount, completionCount, completionRate, topCategories, topIdeas }`.

- [ ] **Step 1: Implement `UI-Admin/src/widget-stat.ts`**

```typescript
import { POLL_INTERVAL_MS } from './dashboard';

interface DashboardStats {
    viewCount: number;
    completionCount: number;
    completionRate: number;
}

function renderStat(el: HTMLElement, value: string): void {
    const valueEl = el.querySelector<HTMLElement>('.widget-value');
    if (valueEl) valueEl.textContent = value;
}

function formatValue(type: string, stats: DashboardStats): string {
    if (type === 'ViewCount') return String(stats.viewCount);
    if (type === 'CompletionCount') return String(stats.completionCount);
    if (type === 'CompletionRate') return `${Math.round(stats.completionRate * 100)}%`;
    return '–';
}

export function initStatWidget(el: HTMLElement): () => void {
    const type = el.dataset.widget ?? '';
    const projectId = el.dataset.projectId ?? '';
    const apiBase = el.dataset.apiBase ?? '';
    const url = `${apiBase}/api/projects/${projectId}/dashboard`;

    async function fetch_and_render(): Promise<void> {
        try {
            const res = await fetch(url);
            if (!res.ok) return;
            const stats: DashboardStats = await res.json();
            renderStat(el, formatValue(type, stats));
        } catch {
            // ignore network errors during polling
        }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
}
```

- [ ] **Step 2: Implement `UI-Admin/src/widget-survey-distribution.ts`**

```typescript
export function initSurveyDistributionWidget(el: HTMLElement): void {
    const surveyUrl = el.dataset.surveyUrl ?? '';
    const qrSrc = `https://chart.googleapis.com/chart?cht=qr&chs=200x200&chl=${encodeURIComponent(surveyUrl)}`;

    el.innerHTML = `
        <p class="text-xs text-gray-400 mb-2">Survey-link</p>
        <div class="flex items-center gap-2 mb-3">
            <span class="text-gray-200 text-xs break-all flex-1">${surveyUrl}</span>
            <button id="copy-btn-${el.dataset.projectId}"
                class="bg-gray-700 hover:bg-gray-600 text-white text-xs px-2 py-1 rounded whitespace-nowrap transition">
                Kopieer
            </button>
        </div>
        <img src="${qrSrc}" alt="QR code" class="rounded mb-2" width="160" height="160" />
        <a href="${qrSrc}" download="qr-code.png" class="text-xs text-gray-400 hover:text-white underline">
            QR downloaden
        </a>
    `;

    const btn = el.querySelector<HTMLButtonElement>(`#copy-btn-${el.dataset.projectId}`);
    btn?.addEventListener('click', () => {
        navigator.clipboard.writeText(surveyUrl).then(() => {
            if (btn) btn.textContent = 'Gekopieerd!';
            setTimeout(() => { if (btn) btn.textContent = 'Kopieer'; }, 2000);
        });
    });
}
```

- [ ] **Step 3: Implement `UI-Admin/src/widget-moderation-queue.ts`**

```typescript
import { POLL_INTERVAL_MS } from './dashboard';

interface ModerationQueue {
    approved: number;
    declined: number;
    flagged: number;
    unflagged: number;
}

export function initModerationQueueWidget(el: HTMLElement): () => void {
    const projectId = el.dataset.projectId ?? '';
    const apiBase = el.dataset.apiBase ?? '';
    const url = `${apiBase}/api/projects/${projectId}/stats/moderation-queue`;

    async function fetch_and_render(): Promise<void> {
        try {
            const res = await fetch(url);
            if (!res.ok) return;
            const q: ModerationQueue = await res.json();
            el.innerHTML = `
                <p class="text-xs text-gray-400 mb-3">Moderatiestatus</p>
                <div class="grid grid-cols-2 gap-2 text-center">
                    <div class="bg-gray-700 rounded p-2">
                        <p class="text-lg font-bold text-green-400">${q.approved}</p>
                        <p class="text-xs text-gray-400">Goedgekeurd</p>
                    </div>
                    <div class="bg-gray-700 rounded p-2">
                        <p class="text-lg font-bold text-yellow-400">${q.unflagged}</p>
                        <p class="text-xs text-gray-400">Nieuw</p>
                    </div>
                    <div class="bg-gray-700 rounded p-2">
                        <p class="text-lg font-bold text-red-400">${q.flagged}</p>
                        <p class="text-xs text-gray-400">Gemarkeerd</p>
                    </div>
                    <div class="bg-gray-700 rounded p-2">
                        <p class="text-lg font-bold text-gray-400">${q.declined}</p>
                        <p class="text-xs text-gray-400">Afgewezen</p>
                    </div>
                </div>
            `;
        } catch {
            // ignore
        }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
}
```

- [ ] **Step 4: Verify TypeScript compiles**

```bash
cd UI-Admin && pnpm exec tsc --noEmit
```
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add UI-Admin/src/widget-stat.ts UI-Admin/src/widget-survey-distribution.ts UI-Admin/src/widget-moderation-queue.ts
git commit -m "feat(dashboard): implement stat, survey-distribution, and moderation-queue widgets"
```

---

## Task 6: Implement TypeScript chart widgets (timeline, top-categories) and list widgets (top-ideas, idea-feed)

**Files:**
- Modify: `UI-Admin/src/widget-timeline.ts`
- Modify: `UI-Admin/src/widget-top-categories.ts`
- Modify: `UI-Admin/src/widget-top-ideas.ts`
- Modify: `UI-Admin/src/widget-idea-feed.ts`

- [ ] **Step 1: Implement `UI-Admin/src/widget-timeline.ts`**

```typescript
import { Chart, LineController, LineElement, PointElement, LinearScale, CategoryScale, Tooltip, Legend } from 'chart.js';
import { POLL_INTERVAL_MS } from './dashboard';

Chart.register(LineController, LineElement, PointElement, LinearScale, CategoryScale, Tooltip, Legend);

interface TimelineData {
    dates: string[];
    starts: number[];
    completions: number[];
}

export function initTimelineWidget(el: HTMLElement): () => void {
    const projectId = el.dataset.projectId ?? '';
    const apiBase = el.dataset.apiBase ?? '';
    const url = `${apiBase}/api/projects/${projectId}/stats/timeline?days=30`;

    const canvas = document.createElement('canvas');
    canvas.style.width = '100%';
    el.innerHTML = '<p class="text-xs text-gray-400 mb-2">Deelname (30 dagen)</p>';
    el.appendChild(canvas);

    const chart = new Chart(canvas, {
        type: 'line',
        data: { labels: [], datasets: [
            { label: 'Gestart', data: [], borderColor: '#60a5fa', backgroundColor: 'transparent', tension: 0.3, pointRadius: 2 },
            { label: 'Voltooid', data: [], borderColor: '#34d399', backgroundColor: 'transparent', tension: 0.3, pointRadius: 2 },
        ]},
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { labels: { color: '#d1d5db', font: { size: 10 } } } },
            scales: {
                x: { ticks: { color: '#9ca3af', font: { size: 9 }, maxTicksLimit: 7 }, grid: { color: '#374151' } },
                y: { ticks: { color: '#9ca3af', font: { size: 9 }, precision: 0 }, grid: { color: '#374151' } },
            },
        },
    });

    async function fetch_and_render(): Promise<void> {
        try {
            const res = await fetch(url);
            if (!res.ok) return;
            const data: TimelineData = await res.json();
            // show last 14 date labels to keep x-axis readable
            chart.data.labels = data.dates.map(d => d.slice(5));
            chart.data.datasets[0].data = data.starts;
            chart.data.datasets[1].data = data.completions;
            chart.update('none');
        } catch {
            // ignore
        }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => { clearInterval(interval); chart.destroy(); };
}
```

- [ ] **Step 2: Implement `UI-Admin/src/widget-top-categories.ts`**

```typescript
import { Chart, BarController, BarElement, LinearScale, CategoryScale, Tooltip } from 'chart.js';
import { POLL_INTERVAL_MS } from './dashboard';

Chart.register(BarController, BarElement, LinearScale, CategoryScale, Tooltip);

interface CategoryMetric {
    name: string;
    ideaCount: number;
    totalReactions: number;
}

interface DashboardData {
    topCategories: CategoryMetric[];
}

export function initTopCategoriesWidget(el: HTMLElement): () => void {
    const projectId = el.dataset.projectId ?? '';
    const apiBase = el.dataset.apiBase ?? '';
    const url = `${apiBase}/api/projects/${projectId}/dashboard`;

    const canvas = document.createElement('canvas');
    el.innerHTML = '<p class="text-xs text-gray-400 mb-2">Top categorieën</p>';
    el.appendChild(canvas);

    const chart = new Chart(canvas, {
        type: 'bar',
        data: { labels: [], datasets: [
            { label: 'Ideeën', data: [], backgroundColor: '#60a5fa' },
            { label: 'Reacties', data: [], backgroundColor: '#a78bfa' },
        ]},
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { labels: { color: '#d1d5db', font: { size: 10 } } } },
            scales: {
                x: { ticks: { color: '#9ca3af', font: { size: 9 } }, grid: { color: '#374151' } },
                y: { ticks: { color: '#9ca3af', font: { size: 9 }, precision: 0 }, grid: { color: '#374151' } },
            },
        },
    });

    async function fetch_and_render(): Promise<void> {
        try {
            const res = await fetch(url);
            if (!res.ok) return;
            const data: DashboardData = await res.json();
            chart.data.labels = data.topCategories.map(c => c.name);
            chart.data.datasets[0].data = data.topCategories.map(c => c.ideaCount);
            chart.data.datasets[1].data = data.topCategories.map(c => c.totalReactions);
            chart.update('none');
        } catch { /* ignore */ }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => { clearInterval(interval); chart.destroy(); };
}
```

- [ ] **Step 3: Implement `UI-Admin/src/widget-top-ideas.ts`**

```typescript
import { POLL_INTERVAL_MS } from './dashboard';

interface IdeaMetric {
    description: string;
    reactionCount: number;
    commentCount: number;
}

interface DashboardData {
    topIdeas: IdeaMetric[];
}

export function initTopIdeasWidget(el: HTMLElement): () => void {
    const projectId = el.dataset.projectId ?? '';
    const apiBase = el.dataset.apiBase ?? '';
    const url = `${apiBase}/api/projects/${projectId}/dashboard`;

    async function fetch_and_render(): Promise<void> {
        try {
            const res = await fetch(url);
            if (!res.ok) return;
            const data: DashboardData = await res.json();
            const rows = data.topIdeas.map((idea, i) => `
                <div class="flex gap-2 py-1 border-b border-gray-700">
                    <span class="text-gray-500 text-xs w-4">${i + 1}</span>
                    <span class="text-gray-200 text-xs flex-1">${idea.description}</span>
                    <span class="text-blue-400 text-xs whitespace-nowrap">${idea.reactionCount} 👍</span>
                </div>
            `).join('');
            el.innerHTML = `<p class="text-xs text-gray-400 mb-2">Top ideeën</p>${rows || '<p class="text-gray-500 text-xs">Nog geen ideeën</p>'}`;
        } catch { /* ignore */ }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
}
```

- [ ] **Step 4: Implement `UI-Admin/src/widget-idea-feed.ts`**

```typescript
import { POLL_INTERVAL_MS } from './dashboard';

interface IdeaFeedItem {
    ideaId: number;
    description: string;
    categoryName: string;
    reactionCount: number;
    createdAt: string;
}

export function initIdeaFeedWidget(el: HTMLElement): () => void {
    const projectId = el.dataset.projectId ?? '';
    const apiBase = el.dataset.apiBase ?? '';
    const url = `${apiBase}/api/projects/${projectId}/stats/idea-feed?count=10`;

    async function fetch_and_render(): Promise<void> {
        try {
            const res = await fetch(url);
            if (!res.ok) return;
            const items: IdeaFeedItem[] = await res.json();
            const rows = items.map(item => {
                const time = new Date(item.createdAt).toLocaleDateString('nl-NL', { day: 'numeric', month: 'short' });
                return `
                    <div class="py-1 border-b border-gray-700">
                        <div class="flex justify-between items-start gap-1">
                            <span class="text-gray-200 text-xs flex-1">${item.description}</span>
                            <span class="text-gray-500 text-xs whitespace-nowrap">${time}</span>
                        </div>
                        <span class="text-xs text-blue-400">${item.categoryName}</span>
                    </div>
                `;
            }).join('');
            el.innerHTML = `<p class="text-xs text-gray-400 mb-2">Nieuwste ideeën</p>${rows || '<p class="text-gray-500 text-xs">Nog geen ideeën</p>'}`;
        } catch { /* ignore */ }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
}
```

- [ ] **Step 5: Verify TypeScript compiles**

```bash
cd UI-Admin && pnpm exec tsc --noEmit
```
Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add UI-Admin/src/widget-timeline.ts UI-Admin/src/widget-top-categories.ts UI-Admin/src/widget-top-ideas.ts UI-Admin/src/widget-idea-feed.ts
git commit -m "feat(dashboard): implement chart and list widget TypeScript modules"
```

---

## Task 7: Rebuild Dashboard.cshtml as widget grid shell and create ConfigureDashboard view

**Files:**
- Modify: `UI-Admin/Views/Project/Dashboard.cshtml`
- Create: `UI-Admin/Views/Project/ConfigureDashboard.cshtml`

- [ ] **Step 1: Rewrite `UI-Admin/Views/Project/Dashboard.cshtml`**

The widget slot HTML must match what the TypeScript orchestrator expects:
- `data-widget="{WidgetType}"` — matches registry key
- `data-project-id="{projectId}"` — numeric ID
- `data-api-base="{apiBaseUrl}"` — full base URL for fetch calls
- `data-survey-url="{surveyUrl}"` — only needed by SurveyDistribution widget

Widget slots that render charts need a fixed height or the Chart.js canvas will collapse. Use `h-56` (14rem) on those slots.

```cshtml
@using SynapseEnqueteManagement.UI.Admin.Models.Dashboard
@model DashboardViewModel
@{
    ViewData["Title"] = Model.ProjectName + " — Dashboard";
    Layout = "_Layout";
}

<div class="min-h-screen bg-gray-900 text-white p-6">

    <div class="flex items-center justify-between mb-6">
        <div>
            <a asp-controller="Project" asp-action="Index" class="text-gray-400 hover:text-white text-xs">← Projecten</a>
            <h1 class="text-xl font-bold mt-1">@Model.ProjectName</h1>
        </div>
        <a asp-action="ConfigureDashboard" asp-route-projectId="@Model.ProjectId"
           class="text-xs border border-gray-600 px-3 py-1.5 text-gray-300 hover:text-white hover:border-gray-400 transition">
            Widgets instellen
        </a>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        @for (var i = 0; i < Model.WidgetLayout.Length; i++)
        {
            var widgetType = Model.WidgetLayout[i];
            var isChart = widgetType is "ParticipationTimeline" or "TopCategories";
            <div data-widget="@widgetType"
                 data-project-id="@Model.ProjectId"
                 data-api-base="@Model.ApiBaseUrl"
                 data-survey-url="@Model.SurveyUrl"
                 class="bg-gray-800 rounded p-4 @(isChart ? "h-56" : "min-h-32")">
                <p class="text-gray-500 text-xs">Laden…</p>
            </div>
        }
    </div>

</div>

@section Scripts {
    <script type="module" vite-src="~/dashboard.ts"></script>
}
```

- [ ] **Step 2: Create `UI-Admin/Views/Project/ConfigureDashboard.cshtml`**

```cshtml
@using SynapseEnqueteManagement.UI.Admin.Models.Dashboard
@model ConfigureDashboardViewModel
@{
    ViewData["Title"] = "Widgets instellen";
    Layout = "_Layout";
}

<div class="min-h-screen bg-gray-900 text-white p-8 max-w-2xl mx-auto">
    <a asp-action="Dashboard" asp-route-projectId="@Model.ProjectId"
       class="text-gray-400 hover:text-white text-xs">← Terug naar dashboard</a>
    <h1 class="text-xl font-bold mt-2 mb-6">Widgets instellen — @Model.ProjectName</h1>

    <form asp-action="ConfigureDashboard" asp-route-projectId="@Model.ProjectId" method="post" class="space-y-4">
        @for (var i = 0; i < 9; i++)
        {
            <div class="flex items-center gap-4">
                <span class="text-gray-400 text-xs w-16">Positie @(i + 1)</span>
                <select name="Layout[@i]"
                        class="flex-1 bg-gray-800 border border-gray-600 text-white text-sm px-3 py-2 focus:outline-none focus:border-gray-400">
                    @foreach (var type in Model.AllWidgetTypes)
                    {
                        var selected = i < Model.Layout.Length && Model.Layout[i] == type ? "selected" : "";
                        var label = type switch
                        {
                            "ViewCount" => "Bekeken (aantal)",
                            "CompletionCount" => "Ingevuld (aantal)",
                            "CompletionRate" => "Voltooiingsgraad (%)",
                            "ParticipationTimeline" => "Deelname over tijd (grafiek)",
                            "TopCategories" => "Top categorieën (grafiek)",
                            "TopIdeas" => "Top ideeën (lijst)",
                            "SurveyDistribution" => "Survey-link & QR-code",
                            "IdeaFeed" => "Nieuwste ideeën (feed)",
                            "ModerationQueue" => "Moderatiestatus",
                            _ => type
                        };
                        <option value="@type" @selected>@label</option>
                    }
                </select>
            </div>
        }

        <div class="flex gap-3 pt-4">
            <button type="submit"
                    class="px-4 py-2 text-xs bg-white text-gray-900 hover:bg-gray-200 transition">
                Opslaan
            </button>
            <a asp-action="Dashboard" asp-route-projectId="@Model.ProjectId"
               class="px-4 py-2 text-xs border border-gray-600 text-gray-300 hover:text-white hover:border-gray-400 transition">
                Annuleer
            </a>
        </div>
    </form>
</div>
```

- [ ] **Step 3: Build**

```bash
dotnet build --no-incremental 2>&1 | tail -5
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add UI-Admin/Views/Project/Dashboard.cshtml UI-Admin/Views/Project/ConfigureDashboard.cshtml
git commit -m "feat(dashboard): rebuild Dashboard.cshtml as widget grid shell; add ConfigureDashboard view"
```

---

## Task 8: Add widget loader to Dashboard view — link widgetstat rendering placeholders

This task validates that the widget `data-widget` slots render initial `"Laden…"` text server-side, and that the TypeScript build includes `dashboard.ts` as a separate entry. No code changes — verification only.

- [ ] **Step 1: Verify Vite picks up dashboard.ts as an entry point**

The `vite.config.ts` auto-discovers all `.ts` files directly in `src/`. `dashboard.ts` is now in `src/`, so it will be a separate bundle.

```bash
cd UI-Admin && pnpm build 2>&1 | grep -E "dashboard|error|warn" | head -20
```
Expected: Lines mentioning `dashboard.js` or `dashboard-<hash>.js` in the output. 0 errors.

- [ ] **Step 2: Confirm stat widget HTML structure matches TS selector**

`initStatWidget` does `el.querySelector<HTMLElement>('.widget-value')`. The Dashboard.cshtml slot renders `<p class="text-gray-500 text-xs">Laden…</p>` which does NOT have `.widget-value`. The TypeScript stat widget uses `innerHTML` indirectly — re-read `widget-stat.ts`.

Actually `renderStat` in `widget-stat.ts` calls `el.querySelector('.widget-value')` — but the slot div has no `.widget-value` child on initial render. The `renderStat` function will silently do nothing because `valueEl` is null.

Fix `widget-stat.ts` to replace the entire slot content on first render:

Replace the `renderStat` function in `UI-Admin/src/widget-stat.ts`:
```typescript
const LABELS: Record<string, string> = {
    ViewCount: 'Bekeken',
    CompletionCount: 'Ingevuld',
    CompletionRate: 'Voltooiingsgraad',
};

function renderStat(el: HTMLElement, type: string, value: string): void {
    el.innerHTML = `
        <p class="text-3xl font-bold text-white">${value}</p>
        <p class="text-xs text-gray-400 mt-1">${LABELS[type] ?? type}</p>
    `;
}
```

Update the call site in `initStatWidget`:
```typescript
renderStat(el, type, formatValue(type, stats));
```

- [ ] **Step 3: Verify TypeScript compiles**

```bash
cd UI-Admin && pnpm exec tsc --noEmit
```
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add UI-Admin/src/widget-stat.ts
git commit -m "fix(dashboard): stat widget replaces full slot content on render"
```
