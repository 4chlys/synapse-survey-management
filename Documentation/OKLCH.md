# OKLCH — why we use it

This document explains the color model behind every visual decision in this codebase. It is written so a team member or a client can read it once and understand both *what* OKLCH is and *why* we chose it for a multi-tenant youth platform.

## TL;DR

We use **OKLCH** instead of `hex` / `rgb` / `hsl` because OKLCH lets us derive a full, readable, brand-consistent palette from a single color the admin picks — automatically, predictably, in both light and dark mode, without a designer in the loop. That is a hard requirement for a platform where every project (every school, city, youth organisation) has its own brand color and where non-technical admins configure it themselves.

## What is OKLCH

OKLCH is a CSS color function. It expresses a color as three numbers:

| Component | Meaning | Range |
|---|---|---|
| **L** | Perceived **L**ightness — how bright the color looks to the human eye | `0%` (black) to `100%` (white) |
| **C** | **C**hroma — how saturated the color is (gray → vivid) | `0` (gray) to ~`0.4` (most saturated) |
| **H** | **H**ue angle on the color wheel | `0` to `360` degrees |

Optional fourth component: opacity (`/ a`).

```css
oklch(52% 0.22 277)        /* Synapse violet — our default primary */
oklch(61% 0.16 162)        /* Clarity teal — secondary / success */
oklch(62% 0.22 37)         /* Spark coral — accent / error */
oklch(100% 0 0)            /* white */
oklch(0% 0 0)              /* pure black */
oklch(52% 0.22 277 / 50%)  /* violet at 50% opacity */
```

The hue wheel: 0° red, 30° orange, 80° yellow, 140° green, 200° cyan, 270° blue, 330° magenta. Rotating H by 180° gives the complementary color — useful when we want a contrasting accent.

## Why not hex or HSL

### Hex / RGB — opaque numbers

`#4F46E5` is six hex digits. A non-technical reader cannot tell what color that is, cannot tell which two hex codes are "similar", and cannot reason about "darker" or "more saturated". Every modification (hover state, focus state, dimmed disabled state) requires a designer to hand-pick the next color.

For a single static brand this is fine. For a multi-tenant platform with hundreds of tenant colors we cannot afford a designer for each one.

### HSL — looks right, behaves wrong

HSL solves readability (`hsl(245, 75%, 59%)` is "blue, saturated, mid-bright") but its **L is mathematical, not perceptual**. The eye does not see lightness uniformly across hues. `hsl(60, 100%, 50%)` (yellow) looks much brighter than `hsl(240, 100%, 50%)` (blue) — same L, very different perceived brightness.

This breaks derived palettes. If we wrote "hover state = primary minus 10% lightness", the result would look correct in blue and wrong in yellow. We saw exactly this in the audit: the original Playful theme at `hsl`-equivalent L=78% rendered as nearly-invisible pale yellow on white because HSL's L is not what the eye reads.

### OKLCH — perceptual, predictable

OKLCH's L is **perceptual lightness**. Two colors with the same L look equally bright to the eye, regardless of hue. That means a derivation like "hover = primary with L − 0.06" produces a visually consistent darkening for any brand color.

This is the property that makes one-color-picker theming work.

## Where we use OKLCH in this codebase

### Every theme token

`UI-User/src/main.css` defines daisyUI's two base themes (`synapse-light`, `synapse-dark`) and four project presets (`default`, `playful`, `professional`, `highcontrast`). Every color token is OKLCH.

```css
@plugin "daisyui/theme" {
  name: "synapse-light";
  --color-primary: oklch(52% 0.22 277);       /* Idea Violet */
  --color-primary-content: oklch(100% 0 0);   /* readable white text */
  --color-secondary: oklch(61% 0.16 162);     /* Clarity Teal */
  --color-accent: oklch(62% 0.22 37);         /* Spark Coral */
  --color-base-100: oklch(100% 0 0);          /* page surface */
  --color-base-content: oklch(15% 0.04 278);  /* body text */
}
```

The mirror constant `PROJECT_THEME_CSS` in `UI-Admin/src/project/project-style.ts` uses the same OKLCH values so the admin preview matches participant rendering exactly.

### Per-project derivations from a single picker

When a project admin picks a custom brand color, the layout emits an inline `<style>` block that derives a full coherent palette from that one pick using CSS relative-color syntax:

```css
body[data-project-theme] {
    --color-primary: @_primary;
    --color-primary-content: @_primaryContent;
    --color-primary-hover:  oklch(from @_primary calc(l - 0.06) c h);
    --color-primary-focus:  oklch(from @_primary calc(l - 0.12) c h);
    --color-accent:         oklch(from @_primary l c calc(h + 180));
}
```

What is happening:

- `oklch(from @_primary calc(l - 0.06) c h)` — same color, lightness lowered 6 points → hover state
- `oklch(from @_primary calc(l - 0.12) c h)` — lightness lowered 12 points → focus state
- `oklch(from @_primary l c calc(h + 180))` — same lightness and chroma, hue rotated 180° → complementary accent

The admin picks one hex. The browser computes four harmonious tokens. No designer needed.

### Soft surfaces via `color-mix`

For subtle backgrounds (idea-card hover, suggestion chips) we mix the primary with the page surface in OKLCH space:

```css
.chip-soft {
    background-color: color-mix(in oklch, var(--color-primary) 12%, var(--color-base-100));
}
```

The `in oklch` keyword tells the browser to interpolate perceptually, not in RGB. The result is a pastel tint that retains the hue identity of the brand color regardless of how saturated or dark the brand is.

### Light / dark adaptation

Every project preset uses `light-dark()` to nominate two OKLCH values — one for `synapse-light`, one for `synapse-dark` — so the same preset stays visually intentional when the participant toggles dark mode:

```css
[data-project-theme="playful"] {
    --color-primary: light-dark(
        oklch(62% 0.18 65),    /* deeper amber on white surfaces */
        oklch(78% 0.16 65)     /* lighter amber on near-black surfaces */
    );
}
```

Same hue, different lightness. The brand reads identically across modes.

## Why this matters for a multi-tenant youth platform

### One admin, many tenants

This platform is sold to schools, cities, and youth organisations. Each tenant has its own brand color. Each tenant's admin is non-technical — a youth worker, a teacher, a civic officer. They will not write CSS, they will not consult a designer, they will not learn what "primary-hover" means.

The OKLCH derivations let us give them **one color picker** and ship a coherent, accessible palette regardless of what they pick. Without OKLCH we would need either a designer per tenant (impossible) or a curated 12-swatch palette (sacrifices brand flexibility).

### Accessibility guarantees

Because L is perceptual, we can write contrast rules that hold across every brand color. The Business Layer rejects any pick whose WCAG contrast ratio against either the light or dark surface falls below 3:1 — and the foreground color on buttons (`--color-primary-content`) is auto-computed from the picked color's luminance so white-on-yellow never ships.

This is the same property that makes the `prefers-reduced-motion` reset work: a uniform rule that holds across all variations. OKLCH gives us uniformity over color the same way that reset gives us uniformity over motion.

### Future-proofing: wide-gamut color

OKLCH can describe colors outside sRGB — including the wider P3 gamut that modern phones, tablets, and Apple displays support. Today our presets stay inside sRGB for cross-device consistency, but we can extend specific tenants into P3 (e.g., a youth organisation with a fluorescent-pink brand) without changing the color model. With hex / RGB this would require a separate code path.

## Browser support

| Feature | Support baseline | Notes |
|---|---|---|
| `oklch()` color function | Chrome 111+, Safari 15.4+, Firefox 113+ (May 2023) | Universally supported on devices likely to access this platform |
| `color-mix(in oklch, ...)` | Chrome 111+, Safari 16.2+, Firefox 113+ (Dec 2022) | Used for soft chips and tinted surfaces |
| `oklch(from var(--x) ...)` (relative color) | Chrome 119+, Safari 16.4+, Firefox 128+ (Jul 2024) | Used for hover/focus/accent derivation. For older browsers we emit static hex fallbacks server-side via `@supports not` |
| `light-dark()` | Chrome 123+, Safari 17.5+, Firefox 120+ (May 2024) | Used per preset to nominate light + dark variants in one declaration |

Our supported-browser baseline (January 2026) covers all of the above on >97 % of devices. The `@supports not` fallback handles the remaining 3 %.

## How to read an OKLCH value

A handful of mental anchors will let you parse any value in this codebase:

| Range | Reads as |
|---|---|
| L 0–20 % | Near-black, body text on white |
| L 20–40 % | Dark surfaces, deep brand colors |
| L 40–60 % | Mid colors — most brand primaries live here |
| L 60–80 % | Light, soft pastels |
| L 80–100 % | Near-white, page surfaces |
| C 0 | Pure gray (no hue) |
| C 0.05–0.10 | Tinted neutrals (slate, warm gray) |
| C 0.15–0.25 | Standard brand saturation |
| C 0.30+ | Vivid, alarm-color territory |
| H 20–40 | Red / coral / orange |
| H 60–90 | Yellow / amber |
| H 130–170 | Green / teal |
| H 200–250 | Cyan / blue |
| H 260–300 | Indigo / violet (our default primary at H = 277) |
| H 320–360 | Magenta / pink |

So `oklch(52% 0.22 277)` reads as: mid-bright, well saturated, violet. Anyone on the team should be able to parse that without a color picker.

## When NOT to use OKLCH

For storage in the database we keep hex (`^#[0-9A-Fa-f]{6}$`). Reasons:

- Hex is what `<input type="color">` produces — no conversion at the form boundary.
- Hex is universally readable by external tools (logo files, brand books, design software).
- Storage size is smaller and not opaque to humans inspecting the database.

OKLCH is purely a **render-time** format in this codebase. The conversion happens once, server-side, when the inline `<style>` is emitted into `_Layout.cshtml`.

## Briefing summary for non-technical readers

If a teammate or client asks "why this strange color format", here is the elevator version:

> OKLCH lets us derive a full visual identity — buttons, hovers, accents, dark-mode variants — from a single brand color the admin picks, and guarantees it stays accessible across every variation. Without it, every tenant would need a designer; with it, every tenant gets a designer's palette automatically. We chose it specifically because this is a multi-tenant platform with non-technical admins.

## References

- [OKLCH playground (live picker)](https://oklch.com/)
- [Evil Martians — OKLCH in CSS: why we moved from RGB and HSL](https://evilmartians.com/chronicles/oklch-in-css-why-quit-rgb-hsl)
- [W3C — CSS Color Module Level 4](https://www.w3.org/TR/css-color-4/#ok-lab)
- [MDN — oklch()](https://developer.mozilla.org/en-US/docs/Web/CSS/color_value/oklch)
- [MDN — color-mix()](https://developer.mozilla.org/en-US/docs/Web/CSS/color_value/color-mix)
- Project docs: `Documentation/accessibility-and-inclusion.md`, `JonasAssignment/ThemingStrategy.md`
