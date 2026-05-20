# Accessibility and inclusion

This platform collects input from people aged roughly 12–30, across cities, schools, and youth organisations. A participant who cannot use the app is a missed voice. Accessibility is not a feature here — it is the entire point of the product.

This document maps every accessibility and inclusion mechanism the app currently implements, why it exists, and where it lives in the code.

## Guiding principles

1. **The participant's environment is authoritative.** If the user has reduced-motion on at the OS level, dark mode on at the OS level, or a specific language, the app respects that without asking.
2. **Multiple input modalities.** Every action must be reachable by keyboard, by touch, and (where it makes sense) by voice. No path is mouse-only.
3. **No login = no per-user settings.** Participants are anonymous behind a session cookie. The app cannot remember preferences across devices, so it must read the right signal from the browser every time.
4. **Visual content is content.** Color carries no information that is not also conveyed by shape, text, or icon. Contrast and font sizing are tested against WCAG AA at minimum.

## Reduced motion

`prefers-reduced-motion` is an OS-level setting that the browser exposes to CSS. The app reads it and neutralises every animation when set.

```css
@media (prefers-reduced-motion: reduce) {
    *, *::before, *::after {
        animation-duration: 0.01ms !important;
        animation-iteration-count: 1 !important;
        transition-duration: 0.01ms !important;
        scroll-behavior: auto !important;
    }
}
```

Lives in `UI-User/src/main.css`. The `!important` is the documented project exception — accessibility resets must beat any per-element animation declaration.

### Who has it on

| Group | Why |
|---|---|
| Users with vestibular disorders (motion sickness, vertigo) | Motion triggers physical symptoms |
| Many users with ADHD / autism | Animation is cognitively distracting |
| Users on battery-saver mode | Windows and some Android OEMs auto-enable it |
| Older users on inherited devices | Apple defaults it on for new iCloud-restored devices over a threshold |
| Anyone who hit it accidentally | And never turned it back off |

Industry estimates 2–5 % of users globally. For a 12–30 youth platform probably 1–3 %, still hundreds of participants over a long-running survey.

### How users enable it

| Platform | Path |
|---|---|
| Windows 10 / 11 | Settings → Accessibility → Visual effects → "Animation effects" off |
| macOS | System Settings → Accessibility → Display → "Reduce motion" |
| iOS / iPadOS | Settings → Accessibility → Motion → "Reduce Motion" |
| Android | Settings → Accessibility → "Remove animations" |
| Firefox override | `about:config` → `ui.prefersReducedMotion` = 1 |
| Chrome DevTools (test) | Rendering panel → "Emulate CSS media feature prefers-reduced-motion" |

The app does NOT expose an in-app toggle. The OS signal is authoritative; duplicating it would force users to find it twice and remember it on every site.

### What it neutralises in this codebase

- `survey-slide` — card-reel translate
- `survey-card` — vacuum-scale entrance / exit
- `page-enter` — slide-up on view mount
- `chat-bubble-left` / `chat-bubble-right` / `typing-dot` — chat animations
- `landing-float-up` — floating background cards on landing
- `orbit-spin` — traveling-border CTA hover
- `slide-up`, `fade-up`, `shake` — small-element keyframes

Content still changes — only the motion is stripped.

## High-contrast theme

A project admin can set `Theme = ProjectTheme.HighContrast` on a project. This is a project-level choice — not a per-user toggle — used when the audience or context demands maximum legibility (school accessibility programmes, vision-impaired participants, projection on a wall, etc.).

The HighContrast block in `UI-User/src/main.css`:

- **Pure-black primary** (`oklch(0% 0 0)`), **pure-white surfaces** (`oklch(100% 0 0)`), pure-black text.
- **Bright-yellow warnings** (`oklch(95% 0.20 100)`) so they remain distinguishable from semantic blue/red.
- **Arial font** — system-installed everywhere, no web-font load, predictable letter shapes.
- **Zero border-radius** — sharper edges read more clearly for users with low vision.
- **Forced-opaque text** — Tailwind utilities like `text-base-content/60` are translucent by default; under HighContrast they are reset to full opacity so low-emphasis text becomes high-emphasis.
- **3px focus outline** with `outline-offset: 2px` — keyboard-focused element is unmissable.

```css
[data-project-theme="highcontrast"] :focus-visible {
    outline: 3px solid var(--color-primary);
    outline-offset: 2px;
}
```

## Color contrast guard

Every project admin can pick a custom primary brand color. The Business Layer rejects any pick that fails WCAG AA (3:1) contrast against either the light surface or the dark surface, so an admin cannot accidentally ship buttons that are unreadable for half the users.

Lives in `BL/Implementation/ProjectManager.cs`, next to the existing hex-format regex. The error surfaces in both Dutch and English via `Validation.PrimaryColor.LowContrast` in `Resources/SharedResource.{nl,en}.resx`.

Additionally, the foreground color used on primary buttons (`--color-primary-content`) is auto-computed from the picked color's perceived luminance — pick a dark brand color, get white text; pick a light one, get black text. The admin never has to think about it.

## Dark mode

The user toggles light/dark from the header at any time. The state is stored in a cookie (`synapse-theme`) so it persists across page loads. The toggle reads the OS preference (`prefers-color-scheme`) on first visit, then switches to the user's explicit choice once they interact.

Both daisyUI themes (`synapse-light`, `synapse-dark`) and every project preset are tuned for both modes. A project's brand stays recognisable in dark mode — only the lightness shifts, not the hue.

## Multi-language i18n

The platform ships full Dutch and English translations across:

- All shared resource strings (`Resources/SharedResource.{nl,en}.resx`)
- Per-view localised strings
- Per-project content (project name, intro, questions, categories) — admins can author every text in every supported language
- AI assistance — moderation, idea refinement, comment refinement all operate in the participant's language
- Speech-to-text — transcription runs against the participant's culture, so Dutch is transcribed as Dutch

Language is switched via the header pill (segmented control with `aria-current="true"` on the active locale) or via the participant's stored language preference. All form validation messages are also localised.

Cultures are wired in `UI-User/Program.cs` via `AddSupportedCultures` / `AddSupportedUICultures`.

## Keyboard and screen-reader support

### ARIA attributes

The app uses semantic ARIA across the interactive surface:

| Attribute | Where |
|---|---|
| `aria-label` | Theme toggle, language switcher, microphone button, conversation history, image describe, cancel buttons |
| `aria-live="polite"` | Chat message log, speech-to-text status text, validation feedback |
| `aria-current="true"` | Active language pill |
| `sr-only` text | Theme toggle's hidden text label backing the icon-only button |
| `alt` attributes | Every uploaded idea image, every organisation logo |

### Focus management

Every focusable element shows a `focus-visible` ring. Chat suggestion chips use a custom ring (`focus-visible:ring-2 focus-visible:ring-primary/40`) so the focused chip stands out without clashing with the brand color.

Under HighContrast theme, the focus ring is bumped to a 3px solid outline so it is unmissable.

### Tab order

The app uses native HTML form elements (`<button>`, `<input>`, `<select>`, `<textarea>`) wherever possible, so the browser supplies tab order for free. Custom interactive elements have explicit `type="button"` to prevent accidental form-submit traps.

## Voice input (speech-to-text)

Many participants cannot type — motor impairment, dyslexia, slow typing on mobile, or simply preferring to talk. Every text input that takes free-form participant content has a microphone button next to it (see `Views/Shared/_MicButton.cshtml`).

Tapping the mic:

1. Asks for microphone permission (browser-handled — no platform prompt).
2. Records audio.
3. Sends to the moderation pipeline for transcription in the participant's culture.
4. Drops the transcribed text into the input.

The participant can edit or discard the transcription before submitting. Mic state is announced via `aria-live` so screen-reader users know when recording has started, stopped, or failed. Errors (no microphone, permission denied, unsupported browser, nothing heard, too long, unsupported format) all have dedicated localised messages.

## AI assistance for expression

Some participants — especially younger ones, second-language speakers, or those with dyslexia — find it hard to phrase their ideas clearly. The app offers AI-driven refinement:

- **Idea support popup** — participant types a rough idea, the AI suggests a clearer version. Participant chooses between their version and the suggestion. The AI does not replace the participant's voice; it offers a parallel option.
- **Comment support popup** — same flow for comments on ideas.
- **Image describe** — participants can upload an image with an idea, and the AI proposes a textual description of what's in the image. Useful for participants whose vocabulary doesn't match the visual, and produces alt text for downstream accessibility.

All AI flows are opt-in. The participant always sees both versions and explicitly picks one.

## Mobile and touch

- **Minimum 16px input font on small screens** so iOS Safari does not zoom on focus and shove the layout sideways. Lives in `UI-User/src/main.css`.
- **`100dvh` viewport units** instead of `100vh`, so fixed positioning tracks the actual visible viewport when the iOS / Android address bar shows or hides. Avoids the survey header sliding under address-bar chrome.
- **Touch-target padding** on all primary actions meets the 44px (iOS HIG) / 48dp (Material) minimum.
- **Scroll lock on landing and survey pages** delegates scroll to inner containers so the fixed header can never be visually crossed by scrolling content.

## Age-appropriate UX

Project admins set a `MinAge` and `MaxAge` on each project (`Domain/Project/Entities/Project.cs`). The presets at `[data-project-theme="..."]` are tuned to different sub-audiences across the 12–30 range:

- **Playful** — softer rounded corners, casual heading font, bright primaries. Designed for 12–17.
- **Default** — neutral modern minimal. Designed for 16–24.
- **Professional** — sharper corners, denser layout, muted brand. Designed for 22–30 or formal civic contexts.
- **HighContrast** — orthogonal accessibility mode, available for any age band.

A non-technical admin picks one preset; the visual character flows to every page without further configuration.

## Privacy and consent

Accessibility includes informed consent. A privacy icon sits in every page header, linking to the project's privacy policy in the participant's language. Participants are anonymous by default; contact info is opt-in via a clear popup.

## Testing checklist

Before shipping any UI change, walk through:

1. **Keyboard-only.** Reach every action with Tab, Shift+Tab, Enter, Space, arrow keys. No focus traps.
2. **Screen reader.** Verify labels and live regions announce sensibly (VoiceOver on macOS / NVDA on Windows / TalkBack on Android).
3. **Reduced motion.** Toggle Chrome DevTools' reduced-motion emulation, reload, verify content swaps but nothing animates.
4. **High contrast.** Switch a test project to `HighContrast`, verify focus rings, opaque text, readable buttons.
5. **Dark mode.** Toggle via the header, verify every surface, brand color, and image looks intentional.
6. **Languages.** Switch the language pill, verify the page reloads in the new culture with no missing strings.
7. **Mobile viewport.** Test on a real iOS Safari and Android Chrome — confirm no zoom on input focus, no header obscured by address bar.
8. **Color blindness.** Use a Chrome DevTools emulator (Rendering → Emulate vision deficiencies) for protanopia, deuteranopia, and tritanopia — verify nothing critical relies on color alone.

## What we do NOT do (yet)

Honest gaps, kept here so they don't get forgotten:

- **Skip-to-content link** — first-tab "jump to main content" link is not yet implemented.
- **Headings audit** — heading levels (`<h1>` … `<h6>`) are not yet audited per view; a screen-reader user navigating by headings may not get a perfectly clean outline.
- **Form-error summary** — validation errors appear next to fields but there is no top-of-form summary linking to each invalid field.
- **Captions / transcripts** — there is no video or audio content yet, but if introduced (e.g., a video question type), captions must ship with it.

These are tracked separately and intentionally left to future work, not assumed done.

## References

- [W3C — Web Content Accessibility Guidelines (WCAG) 2.1](https://www.w3.org/WAI/WCAG21/quickref/)
- [MDN — prefers-reduced-motion](https://developer.mozilla.org/en-US/docs/Web/CSS/@media/prefers-reduced-motion)
- [MDN — ARIA](https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA)
- [Inclusive Components — Heydon Pickering](https://inclusive-components.design/)
