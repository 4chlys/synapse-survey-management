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

This breaks derived palettes. If we wrote "hover state = primary minus 10% lightness", the result would look correct in blue and wrong in yellow. We saw exactly this in the audit: the original Playful preset (now `Friendly`) at `hsl`-equivalent L=78% rendered as nearly-invisible pale yellow on white because HSL's L is not what the eye reads.

### OKLCH — perceptual, predictable

OKLCH's L is **perceptual lightness**. Two colors with the same L look equally bright to the eye, regardless of hue. That means a derivation like "hover = primary with L − 0.06" produces a visually consistent darkening for any brand color.

This is the property that makes one-color-picker theming work.

## Where we use OKLCH in this codebase

### Every theme token

`UI-User/src/main.css` defines daisyUI's two base themes (`synapse-light`, `synapse-dark`) and five project presets (`minimal`, `friendly`, `bold`, `formal`, `highcontrast`). Every color token is OKLCH.

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
[data-project-theme="friendly"] {
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

## Server-side OKLab math — the relative-color fallback

The `oklch(from var(--color-primary) calc(l - 0.06) c h)` syntax only works on Chrome 119+, Safari 16.4+, Firefox 128+. For older browsers we ship the same derived colors as static hex values inside a `@supports not (color: oklch(from red l c h))` block. Those hex values are computed **once, server-side**, in C# — not at request time per element, not in the browser.

### Where the code lives

| File | Purpose |
|---|---|
| `BL/Helpers/ColorHelpers.cs` | Authoritative implementation. WCAG luminance / contrast helpers + the OKLab derivation (`ShiftLightness`, `ComplementaryHue`). Used by `ProjectManager` for save-time contrast guards. |
| `UI-User/Helpers/ColorHelpers.cs` | Duplicate of the same helpers. Exists because `UI-User` does NOT reference `BL` directly — it talks to the platform over HTTP. The duplication is intentional and must stay in sync. |
| `UI-User/Extensions/ControllerLayoutExtensions.cs` | Calls `ShiftLightness(-0.06)`, `ShiftLightness(-0.12)`, and `ComplementaryHue` on the picked `PrimaryColor`. Exposes the three hex fallbacks via `ViewData["ProjectPrimaryHover" / "ProjectPrimaryFocus" / "ProjectAccent"]`. |
| `UI-User/Views/Shared/_Layout.cshtml` | Emits the `<style>` block containing the relative-color OKLCH rules plus the `@supports not` block with the precomputed hex fallbacks. |
| `UI-Admin/src/project/project-style.ts` | TypeScript mirror used in the admin preview shadow root. Must produce byte-identical hex to the C# helpers so WYSIWYG holds. |

### The pipeline at a glance

Going from a stored hex (`#RRGGBB`) to an adjusted hex follows a fixed five-step round-trip. Every node below maps to one block of code in `ColorHelpers.cs` and `project-style.ts`:

```
        #RRGGBB                            #RRGGBB
           │                                   ▲
           ▼                                   │
     sRGB ∈ [0, 1]                       sRGB ∈ [0, 1]
           │                                   ▲
           │  gamma decode                     │  gamma encode
           ▼                                   │
     linear sRGB                          linear sRGB
           │                                   ▲
           │  × M₁                             │  × M₁⁻¹
           ▼                                   │
        LMS cones                          LMS cones
           │                                   ▲
           │  ∛  (perceptual compression)      │  ( · )³
           ▼                                   │
     cube-rooted LMS                    cube-rooted LMS
           │                                   ▲
           │  × M₂                             │  × M₂⁻¹
           ▼                                   │
       OKLab (L, a, b) ─── adjust ─── OKLab (L, a, b)
                              │
                              ├─ ShiftLightness(Δ):    L ← clamp(L + Δ, 0, 1)
                              └─ ComplementaryHue:     (a, b) ← (−a, −b)
```

The adjustments happen in OKLab because lightness and hue are linearly separable there — exactly the property that hex, HSL, and linear sRGB do not have.

### Step 1 — gamma decode (sRGB → linear sRGB)

Display values are gamma-encoded to spread perceptual resolution across the low end. We undo that before doing any color math:

$$
c_{\text{linear}} =
\begin{cases}
  \dfrac{c}{12.92} & c \leq 0.04045 \\[4pt]
  \left(\dfrac{c + 0.055}{1.055}\right)^{2.4} & c > 0.04045
\end{cases}
$$

Applied independently to each of `r`, `g`, `b` in `[0, 1]`.

### Step 2 — linear sRGB → LMS cone responses

LMS approximates how the human retina's three cone cells respond to light. The transformation is a fixed 3×3 matrix from Björn Ottosson's OKLab definition:

$$
\begin{bmatrix} l_{\text{LMS}} \\ m_{\text{LMS}} \\ s_{\text{LMS}} \end{bmatrix}
=
\begin{bmatrix}
  0.4122214708 & 0.5363325363 & 0.0514459929 \\
  0.2119034982 & 0.6806995451 & 0.1073969566 \\
  0.0883024619 & 0.2817188376 & 0.6299787005
\end{bmatrix}
\cdot
\begin{bmatrix} r \\ g \\ b \end{bmatrix}
$$

We call this matrix $\mathbf{M_1}$. Note the rows sum to roughly 1 — pure white in linear sRGB lands at LMS ≈ (1, 1, 1).

### Step 3 — perceptual compression (cube root)

The cone response is then cube-rooted, channel-wise. This is where "perceptual" enters the model: the cube root approximates how the visual cortex compresses luminance.

$$
l' = \sqrt[3]{l_{\text{LMS}}}, \quad
m' = \sqrt[3]{m_{\text{LMS}}}, \quad
s' = \sqrt[3]{s_{\text{LMS}}}
$$

### Step 4 — cube-rooted LMS → OKLab

A second fixed 3×3 matrix maps the compressed LMS triple to the (L, a, b) axes:

$$
\begin{bmatrix} L \\ a \\ b \end{bmatrix}
=
\begin{bmatrix}
  0.2104542553 &  0.7936177850 & -0.0040720468 \\
  1.9779984951 & -2.4285922050 &  0.4505937099 \\
  0.0259040371 &  0.7827717662 & -0.8086757660
\end{bmatrix}
\cdot
\begin{bmatrix} l' \\ m' \\ s' \end{bmatrix}
$$

We call this matrix $\mathbf{M_2}$. The L axis is perceptual lightness in `[0, 1]`; the `a` axis runs green ↔ red; the `b` axis runs blue ↔ yellow.

### Step 5 — adjust in OKLab

This is the entire reason we did the trip. With L isolated, "hover" and "focus" are one subtraction each:

$$
\text{ShiftLightness}(\Delta): \quad L' = \text{clamp}(L + \Delta, \; 0, \; 1)
$$

And the complementary hue, which in polar form would be $h \mathrel{+}= 180°$, becomes a sign flip in Cartesian a/b (skipping the `atan2` round-trip entirely):

$$
\text{ComplementaryHue}: \quad (a', b') = (-a, \; -b)
$$

### Step 6 — back to hex

The inverse trip uses the inverse matrices and the inverse gamma curve. The two inverses are also constants from Ottosson's definition:

$$
\mathbf{M_2^{-1}} =
\begin{bmatrix}
  1 &  0.3963377774 &  0.2158037573 \\
  1 & -0.1055613458 & -0.0638541728 \\
  1 & -0.0894841775 & -1.2914855480
\end{bmatrix}
\qquad
\mathbf{M_1^{-1}} =
\begin{bmatrix}
   4.0767416621 & -3.3077115913 &  0.2309699292 \\
  -1.2684380046 &  2.6097574011 & -0.3413193965 \\
  -0.0041960863 & -0.7034186147 &  1.7076147010
\end{bmatrix}
$$

Cubing the LMS row undoes the cube-root compression, and the inverse gamma curve produces final sRGB values:

$$
c_{\text{srgb}} =
\begin{cases}
  12.92 \cdot c_{\text{linear}} & c_{\text{linear}} \leq 0.0031308 \\[4pt]
  1.055 \cdot c_{\text{linear}}^{\,1/2.4} - 0.055 & c_{\text{linear}} > 0.0031308
\end{cases}
$$

Channels are clamped to `[0, 1]` before encoding (a 180° rotation can push a vivid color out of the sRGB gamut), then scaled to `[0, 255]` and formatted as two hex digits each.

### Why the magic numbers Δ = −0.06 and Δ = −0.12

OKLab's L is normalised to `[0, 1]`, matching the way modern CSS reads OKLCH lightness as either a decimal or a percentage. The hover token drops L by 0.06 (≈ 6 percentage points) and the focus token drops it by a further 0.06. These were tuned against the existing daisyUI primaries:

- Light enough to leave a clearly perceptible step between primary → hover → focus,
- Dark enough not to wash the primary out at first contact,
- Large enough that the difference survives the cube-root rounding back to sRGB even on already-dark primaries.

Going beyond `−0.12` on a primary that started near `L = 0.20` (e.g. high-contrast dark) clips into pure black, at which point the picker is rejected upstream by Stage 7's contrast guard — so we never need to handle "no further darkening possible" in the helper itself. The clamp at `[0, 1]` is defensive only.

### Why fallbacks are computed in C#, not JS, on the participant page

The participant page must render correctly on first paint, without waiting for a JS bundle, on browsers that may not support relative-color OKLCH. Computing the fallback in the controller and inlining it as a `@supports not` rule means:

1. No layout shift — the browser knows the resolved color before any JS runs.
2. No `requestAnimationFrame` flash where the page paints with the wrong color and corrects itself.
3. Older browsers (3 % of our traffic at the January 2026 baseline) get pixel-correct rendering.
4. Modern browsers ignore the `@supports not` block entirely and use the live OKLCH derivation, which stays in sync if the admin updates the primary later (CSS recomputes; the server doesn't have to re-render).

The admin **preview** in `UI-Admin` is a different story: it lives behind JS already (the picker drives a shadow root), so we just compute the hex in TypeScript and emit it directly — no `@supports` needed inside the preview.

### Keeping the three implementations in sync

There is no compile-time link between `BL/Helpers/ColorHelpers.cs`, `UI-User/Helpers/ColorHelpers.cs`, and `UI-Admin/src/project/project-style.ts` — treat divergence as a bug. The cheap regression test is to pick a saturated brand color in the admin preview, compare hover/focus/accent rendering side-by-side with the participant page, and check they match exactly. If any of the eighteen matrix coefficients above is mistyped in one file, this test will surface it on the first try.

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

> OKLCH lets us derive a full visual identity — buttons, hovers, accents, dark-mode variants — from a single brand color the admin picks, and guarantees it stays accessible across every variation. Without it, every tenant would need a designer; with it, every tenant gets a designer's palette automatically. We chose it specifically because this is a multi-tenant platform with non-technical admins.

## References

- [OKLCH playground (live picker)](https://oklch.com/)
- [Evil Martians — OKLCH in CSS: why we moved from RGB and HSL](https://evilmartians.com/chronicles/oklch-in-css-why-quit-rgb-hsl)
- [W3C — CSS Color Module Level 4](https://www.w3.org/TR/css-color-4/#ok-lab)
- [MDN — oklch()](https://developer.mozilla.org/en-US/docs/Web/CSS/color_value/oklch)
- [MDN — color-mix()](https://developer.mozilla.org/en-US/docs/Web/CSS/color_value/color-mix)
- Project docs: `Documentation/accessibility-and-inclusion.md`
