# UI-Admin Redesign

**Goal:** Replace the current raw-Tailwind dark admin UI with a DaisyUI-powered shell that matches UI-User's design language: identical theme tokens, same fonts, light/dark toggle, violet/teal/coral palette, and consistent Dutch copy throughout.

**Language:** Dutch throughout all views. i18n is a future task.

---

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Component library | DaisyUI v5 (same as UI-User) | Already proven in the codebase; shared tokens guarantee visual consistency |
| Theme tokens | Copy `synapse-light` / `synapse-dark` verbatim from UI-User | Identical OKLCH values = pixel-perfect match |
| Fonts | Space Grotesk (headings) + Inter (body) | Same as UI-User |
| Layout | Icon sidebar + top bar + inner project sidebar | Option C chosen in brainstorm; compact and scalable |
| Theme toggle | `localStorage` → `data-theme` on `<html>` | Same mechanism as UI-User; no server round-trip |
| Language | Dutch default, no toggle | i18n deferred to follow-up |
| Scroll fix | `bg-base-200` on `<body>` in `_Layout.cshtml`, remove old `bg-gray-100` | Eliminates grey stripe artefact |
| Inner sidebar | Rendered conditionally via `ViewData["ProjectId"]` | Zero new controllers; layout reads ViewData |

---

## Layout shell — `_Layout.cshtml`

The shell has three horizontal zones:

```
┌──────────────────────────────────────────────────────┐
│  Top bar  (h-14, bg-base-100, border-b)              │
├────┬──────────────┬───────────────────────────────────┤
│Icon│Inner sidebar │  @RenderBody()                    │
│side│(200px, only  │  bg-base-200, overflow-y-auto     │
│bar │ in project   │                                   │
│52px│ context)     │                                   │
└────┴──────────────┴───────────────────────────────────┘
```

### Top bar contents (left → right)
- Synapse logo mark (`bg-primary rounded-lg w-8 h-8`)
- Vertical divider
- Breadcrumb: `Projecten / {ProjectName} / {ActiveSection}` — built from `ViewData["BreadcrumbParts"]` (list of `(label, url?)`)
- Spacer
- Theme toggle (pill switch, sun/moon icon, `localStorage` driven)
- Avatar circle with first letter of `User.Identity.Name`, dropdown with "Wachtwoord wijzigen" + "Uitloggen"

### Icon sidebar (always visible, 52px)
Roles determine which icons appear:

| Icon | Section | Visible to |
|---|---|---|
| Grid 2×2 | Projecten | PPA + SA |
| Building | Platform | SA only |
| Users | Beheerders | SA only |

Active icon: `bg-primary text-primary-content rounded-xl`. Inactive: `bg-base-200 text-base-content/40 hover:bg-base-300`. No labels — tooltip on hover.

### Inner project sidebar (200px, conditional)
Appears when `ViewData["ProjectId"]` is set. Contains:

- Platform name (uppercase, `text-primary text-xs font-bold tracking-widest`)
- Project name (`font-heading font-bold text-base-content`)
- Nav items (in order): Dashboard, Ideeën, Categorieën, Vragen, Instellingen
- Project state badge at bottom (`badge badge-success` / `badge badge-warning` / `badge badge-ghost`)

Active item: `bg-primary/10 text-primary font-semibold rounded-xl`. Inactive: `text-base-content/60 hover:bg-base-200 rounded-xl`.

`ViewData["ActiveSection"]` is set in each controller action to highlight the correct item.

### Login page
Login uses a separate layout (`_LoginLayout.cshtml`) — no sidebars, centered card on `bg-base-200`, identical to UI-User's card pattern.

---

## Theme toggle

`UI-Admin/src/theme.ts` (new file, imported in main entry):

```typescript
const STORAGE_KEY = 'admin-theme';
const html = document.documentElement;

export function initTheme(): void {
    const saved = localStorage.getItem(STORAGE_KEY);
    const preferred = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'synapse-dark' : 'synapse-light';
    html.setAttribute('data-theme', saved ?? preferred);
}

export function toggleTheme(): void {
    const next = html.getAttribute('data-theme') === 'synapse-dark' ? 'synapse-light' : 'synapse-dark';
    html.setAttribute('data-theme', next);
    localStorage.setItem(STORAGE_KEY, next);
}
```

The toggle button in the top bar calls `toggleTheme()` on click.

---

## CSS setup — `UI-Admin/src/main.css`

Copy from UI-User verbatim, with two changes:
1. `@source` points to `../Views/**/*.cshtml` (already done in UI-User)
2. Add `@source "../Controllers/**/*.cs"` (for dynamic class detection if needed)

Full token set to copy:
- `synapse-light` theme (all `--color-*`, `--radius-*`, `--border`, `--depth`, `--noise`)
- `synapse-dark` theme (same vars, dark values)
- `@theme` block (`--font-sans`, `--font-heading`)
- `:root` block (`--header-h: 64px`)
- `html { scrollbar-gutter: stable }`
- `body { font-family: var(--font-sans); -webkit-font-smoothing: antialiased; }`

---

## New view: Ideeën (`/Project/Ideas?projectId=&search=&categoryId=&status=`)

### Controller action — `ProjectController.Ideas`

```
GET /Project/Ideas?projectId={id}&search={text}&categoryId={id?}&status={status?}&page={n}
```

- Requires `[Authorize(Roles = "ProjectPlatformAdmin")]`
- Ownership check: same as existing actions (user's `ProjectPlatformId` must match project)
- Returns `IdeasViewModel`

### `IdeasViewModel`

```csharp
public class IdeasViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; }
    public string Search { get; set; }
    public int? CategoryId { get; set; }
    public string Status { get; set; }
    public int Page { get; set; } = 1;
    public int TotalCount { get; set; }
    public int PageSize { get; set; } = 20;
    public List<IdeaRowViewModel> Ideas { get; set; } = new();
    public List<CategoryOptionViewModel> Categories { get; set; } = new();
    // Summary counts
    public int ApprovedCount { get; set; }
    public int PendingCount { get; set; }
    public int FlaggedCount { get; set; }
}

public class IdeaRowViewModel
{
    public int IdeaId { get; set; }
    public string Description { get; set; }
    public string CategoryName { get; set; }
    public int CategoryId { get; set; }
    public string Status { get; set; }   // "Approved" | "Unflagged" | "Flagged" | "Declined"
    public int ReactionCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CategoryOptionViewModel
{
    public int CategoryId { get; set; }
    public string Name { get; set; }
}
```

### Filtering logic (EF Core)
```
query = ideas
  .Where(i => i.Category.ProjectId == projectId)
  .If(search != null, i => i.Description.Contains(search))
  .If(categoryId != null, i => i.CategoryId == categoryId)
  .If(status != null, i => i.Status == Enum.Parse<ModerationStatus>(status))
  .OrderByDescending(i => i.CreatedAt)
  .Skip((page - 1) * pageSize)
  .Take(pageSize)
```

### Moderation actions — `ProjectController`

Three POST actions, all redirect back with current query string preserved:

```
POST /Project/ApproveIdea   { ideaId, returnUrl }
POST /Project/DeclineIdea   { ideaId, returnUrl }
POST /Project/FlagIdea      { ideaId, returnUrl }
```

Each action looks up the idea, verifies project ownership, sets `idea.Status`, saves, and redirects to `returnUrl`.

### View layout
- Filter bar: search `<input type="search">` + category `<select>` + status `<select>` — wrapped in a `<form method="get">` so pressing Enter or changing a select submits
- Summary badges: approved (success), nieuw/pending (warning), gemarkeerd (error)
- Table: description + date, category badge, status badge, reaction count, action buttons
- Row background tinted by status: pending = `bg-warning/5`, flagged = `bg-error/5`
- Action buttons are small `<form method="post">` elements inside the row (one form per action)
- Pagination: previous / page numbers / next

### BL + DAL additions needed

**`ICategoryManager` / `CategoryManager`**: Add `GetIdeasPagedAsync(int projectId, string search, int? categoryId, ModerationStatus? status, int page, int pageSize)` returning `(List<Idea> items, int total)`.

**`ICategoryRepository` / `CategoryRepository`**: Add `ReadIdeasPagedAsync(...)` with the EF query above.

**`ICategoryManager` / `CategoryManager`**: Add `SetIdeaModerationStatusAsync(int ideaId, ModerationStatus status)`.

**`ICategoryRepository` / `CategoryRepository`**: Add `ReadIdeaByIdForUpdateAsync(int ideaId)`.

---

## Pages to update (all views)

Every page below gets rebuilt using DaisyUI components inside the new shell. No logic changes — only markup.

| View file | Key DaisyUI components used |
|---|---|
| `_Layout.cshtml` | Layout shell (icon sidebar, top bar, inner sidebar) |
| `_LoginLayout.cshtml` (new) | No sidebar, centered `card` |
| `Auth/Login.cshtml` | `card`, `input`, `btn btn-primary`, `form-control`, `label` |
| `Auth/ChangePassword.cshtml` | Same pattern as Login |
| `Project/Index.cshtml` | `card` grid, `badge`, `btn`, `dropdown` for actions |
| `Project/Create.cshtml` | `form-control`, `input`, `select`, `btn`, `steps` for multi-step |
| `Project/Dashboard.cshtml` | Widget grid (existing logic), DaisyUI card shells |
| `Project/ConfigureDashboard.cshtml` | `select`, `btn`, drag handles |
| `Project/Ideas.cshtml` (new) | `table`, `badge`, `input`, `select`, `btn`, `pagination` |
| `Project/Ideation.cshtml` | `table`, `badge`, `btn`, `modal` for add category |
| `Project/Questions.cshtml` | `table`, `btn`, `modal` for add/edit question |
| `Project/Activate.cshtml` | `card`, `btn btn-primary` |
| `Platform/Index.cshtml` | `table`, `btn`, `badge` |
| `Admin/Add.cshtml` | `form-control`, `input`, `select`, `btn` |

---

## File map

**Modify:**
- `UI-Admin/src/main.css` — replace with DaisyUI + synapse themes
- `UI-Admin/package.json` — add `daisyui`
- `UI-Admin/Views/Shared/_Layout.cshtml` — full rewrite as shell
- All view files listed above

**Create:**
- `UI-Admin/src/theme.ts` — theme toggle logic
- `UI-Admin/Views/Shared/_LoginLayout.cshtml` — login-only layout
- `UI-Admin/Views/Project/Ideas.cshtml` — new ideas view
- `UI-Admin/Models/Project/IdeasViewModel.cs` — view model
- `UI-Admin/Services/Project/IdeaAdminService.cs` — HTTP calls to API

**Modify (BL/DAL/API):**
- `BL/Interfaces/ICategoryManager.cs` — add `GetIdeasPagedAsync`, `SetIdeaModerationStatusAsync`
- `BL/Implementation/CategoryManager.cs` — implement above
- `DAL/Interfaces/ICategoryRepository.cs` — add `ReadIdeasPagedAsync`, `ReadIdeaByIdForUpdateAsync`
- `DAL/Implementation/CategoryRepository.cs` — implement above
- Create: `API/Controllers/IdeasController.cs` — two endpoints: `GET /api/projects/{id}/ideas` (paged, filtered) and `PATCH /api/projects/{id}/ideas/{ideaId}/status`
- `UI-Admin/Controllers/ProjectController.cs` — add `Ideas`, `ApproveIdea`, `DeclineIdea`, `FlagIdea` actions

**`IdeaAdminService`** (new, pattern identical to `ProjectAdminService`):
- `GetIdeasAsync(int projectId, string search, int? categoryId, string status, int page)` — `GET /api/projects/{id}/ideas?...`
- `SetStatusAsync(int projectId, int ideaId, string status)` — `PATCH /api/projects/{id}/ideas/{ideaId}/status`

---

## Out of scope

- i18n / language toggle (follow-up)
- Segment filters on Ideeën view (follow-up)
- Bulk moderation actions (follow-up)
- Mobile responsive sidebar (follow-up — admin is desktop-first)
