// Wires the project "Stijl" section: colour picker + "use theme default"
// checkbox both feed a single hidden input (PrimaryColor) so an empty value
// reaches the server when the user wants the theme's own colour.

import type { PreviewRefs } from './types';

// Mirror of UI-User/src/main.css per-project-theme blocks. Keep in sync
// when UI-User adds or changes a theme. Injected into the shadow root so
// the preview renders identically to the participant page.
const PROJECT_THEME_CSS = `
:host { color-scheme: light dark; }
[data-project-theme="minimal"] {
    --color-primary: light-dark(oklch(52% 0.22 277), oklch(64% 0.22 277));
    --color-primary-content: light-dark(oklch(100% 0 0), oklch(100% 0 0));
    --color-secondary: light-dark(oklch(61% 0.16 162), oklch(65% 0.14 162));
    --color-secondary-content: oklch(100% 0 0);
    --color-accent: light-dark(oklch(62% 0.22 37), oklch(67% 0.20 37));
    --color-accent-content: oklch(100% 0 0);
    --color-base-100: light-dark(oklch(100% 0 0), oklch(15% 0.04 278));
    --color-base-200: light-dark(oklch(97% 0.01 277), oklch(20% 0.04 278));
    --color-base-300: light-dark(oklch(91% 0.02 277), oklch(27% 0.04 278));
    --color-base-content: light-dark(oklch(15% 0.04 278), oklch(97% 0.01 277));
    --font-heading: 'Space Grotesk', ui-sans-serif, sans-serif;
    --radius-field: 0.75rem;
    --radius-box: 1rem;
}
[data-project-theme="friendly"] {
    --color-primary: light-dark(oklch(62% 0.18 65), oklch(78% 0.16 65));
    --color-primary-content: light-dark(oklch(100% 0 0), oklch(15% 0.04 278));
    --color-secondary: light-dark(oklch(64% 0.16 180), oklch(72% 0.14 180));
    --color-secondary-content: oklch(100% 0 0);
    --color-accent: light-dark(oklch(64% 0.22 350), oklch(72% 0.20 350));
    --color-accent-content: oklch(100% 0 0);
    --color-base-100: light-dark(oklch(100% 0 0), oklch(15% 0.04 278));
    --color-base-200: light-dark(oklch(97% 0.01 277), oklch(20% 0.04 278));
    --color-base-300: light-dark(oklch(91% 0.02 277), oklch(27% 0.04 278));
    --color-base-content: light-dark(oklch(15% 0.04 278), oklch(97% 0.01 277));
    --font-heading: 'Nunito', ui-sans-serif, sans-serif;
    --radius-field: 1rem;
    --radius-box: 1.25rem;
    --tap-target: 3rem;
}
[data-project-theme="bold"] {
    --color-primary: light-dark(oklch(55% 0.21 32), oklch(70% 0.18 32));
    --color-primary-content: light-dark(oklch(100% 0 0), oklch(18% 0.03 32));
    --color-secondary: light-dark(oklch(52% 0.18 250), oklch(64% 0.17 250));
    --color-secondary-content: oklch(100% 0 0);
    --color-accent: light-dark(oklch(60% 0.19 320), oklch(70% 0.18 320));
    --color-accent-content: oklch(100% 0 0);
    --color-base-100: light-dark(oklch(100% 0 0), oklch(15% 0.04 278));
    --color-base-200: light-dark(oklch(97% 0.01 277), oklch(20% 0.04 278));
    --color-base-300: light-dark(oklch(91% 0.02 277), oklch(27% 0.04 278));
    --color-base-content: light-dark(oklch(15% 0.04 278), oklch(97% 0.01 277));
    --font-heading: 'Space Grotesk', ui-sans-serif, sans-serif;
    --radius-field: 0.125rem;
    --radius-box: 0.25rem;
}
[data-project-theme="formal"] {
    --color-primary: light-dark(oklch(35% 0.04 250), oklch(55% 0.06 250));
    --color-primary-content: light-dark(oklch(100% 0 0), oklch(100% 0 0));
    --color-secondary: light-dark(oklch(48% 0.08 200), oklch(60% 0.08 200));
    --color-secondary-content: oklch(100% 0 0);
    --color-accent: light-dark(oklch(55% 0.12 70), oklch(68% 0.12 70));
    --color-accent-content: light-dark(oklch(100% 0 0), oklch(15% 0.04 278));
    --color-base-100: light-dark(oklch(100% 0 0), oklch(15% 0.04 278));
    --color-base-200: light-dark(oklch(97% 0.01 277), oklch(20% 0.04 278));
    --color-base-300: light-dark(oklch(91% 0.02 277), oklch(27% 0.04 278));
    --color-base-content: light-dark(oklch(15% 0.04 278), oklch(97% 0.01 277));
    --font-heading: 'Source Sans 3', ui-sans-serif, sans-serif;
    --radius-field: 0.25rem;
    --radius-box: 0.375rem;
}
[data-project-theme="highcontrast"] {
    --color-primary: light-dark(oklch(0% 0 0), oklch(95% 0 0));
    --color-primary-content: light-dark(oklch(100% 0 0), oklch(15% 0.04 278));
    --color-base-100: light-dark(oklch(100% 0 0), oklch(8% 0 0));
    --color-base-200: light-dark(oklch(100% 0 0), oklch(8% 0 0));
    --color-base-300: light-dark(oklch(80% 0 0), oklch(20% 0 0));
    --color-base-content: light-dark(oklch(0% 0 0), oklch(100% 0 0));
    --color-secondary: light-dark(oklch(0% 0 0), oklch(95% 0 0));
    --color-secondary-content: light-dark(oklch(100% 0 0), oklch(15% 0.04 278));
    --color-accent: light-dark(oklch(0% 0 0), oklch(95% 0 0));
    --color-accent-content: light-dark(oklch(100% 0 0), oklch(15% 0.04 278));
    --color-warning: oklch(95% 0.20 100);
    --font-heading: 'Arial', sans-serif;
    --radius-field: 0;
    --radius-box: 0;
}
`;

// Mirror of UI-User/src/main.css daisyUI synapse-light / synapse-dark @plugin blocks.
// Keep in sync when theme tokens change. Injected into the shadow root so
// light-dark() resolves correctly for the forced-mode preview.
const SYNAPSE_BASE_THEMES_CSS = `
[data-theme="synapse-light"] {
    color-scheme: light;
    --color-base-100: oklch(100% 0 0);
    --color-base-200: oklch(97% 0.01 277);
    --color-base-300: oklch(91% 0.02 277);
    --color-base-content: oklch(15% 0.04 278);
    --color-primary: oklch(52% 0.22 277);
    --color-primary-content: oklch(100% 0 0);
    --color-secondary: oklch(61% 0.16 162);
    --color-secondary-content: oklch(100% 0 0);
    --color-accent: oklch(62% 0.22 37);
    --color-accent-content: oklch(100% 0 0);
    --color-neutral: oklch(47% 0.04 278);
    --color-neutral-content: oklch(97% 0.01 277);
    --color-info: oklch(61% 0.14 230);
    --color-info-content: oklch(98% 0.01 230);
    --color-success: oklch(61% 0.16 162);
    --color-success-content: oklch(100% 0 0);
    --color-warning: oklch(75% 0.16 80);
    --color-warning-content: oklch(20% 0.05 80);
    --color-error: oklch(62% 0.22 37);
    --color-error-content: oklch(100% 0 0);
    --radius-selector: 0.75rem;
    --radius-field: 0.75rem;
    --radius-box: 1rem;
}
[data-theme="synapse-dark"] {
    color-scheme: dark;
    --color-base-100: oklch(15% 0.04 278);
    --color-base-200: oklch(20% 0.04 278);
    --color-base-300: oklch(27% 0.04 278);
    --color-base-content: oklch(97% 0.01 277);
    --color-primary: oklch(64% 0.22 277);
    --color-primary-content: oklch(100% 0 0);
    --color-secondary: oklch(65% 0.14 162);
    --color-secondary-content: oklch(100% 0 0);
    --color-accent: oklch(67% 0.20 37);
    --color-accent-content: oklch(100% 0 0);
    --color-neutral: oklch(55% 0.04 278);
    --color-neutral-content: oklch(97% 0.01 277);
    --color-info: oklch(65% 0.14 230);
    --color-info-content: oklch(98% 0.01 230);
    --color-success: oklch(65% 0.14 162);
    --color-success-content: oklch(100% 0 0);
    --color-warning: oklch(78% 0.14 80);
    --color-warning-content: oklch(20% 0.05 80);
    --color-error: oklch(67% 0.20 37);
    --color-error-content: oklch(100% 0 0);
    --radius-selector: 0.75rem;
    --radius-field: 0.75rem;
    --radius-box: 1rem;
}
`;

const CSS_VARIABLES: ReadonlyArray<{ name: string; hint: string }> = [
    { name: '--color-primary',         hint: 'Hoofdkleur (knoppen, accenten)' },
    { name: '--color-primary-content', hint: 'Tekstkleur op primaire vlakken' },
    { name: '--color-secondary',       hint: 'Secundaire kleur' },
    { name: '--color-accent',          hint: 'Accentkleur' },
    { name: '--color-base-100',        hint: 'Hoofdoppervlak' },
    { name: '--color-base-200',        hint: 'Sectie-achtergrond' },
    { name: '--color-base-content',    hint: 'Hoofdtekstkleur' },
    { name: '--color-warning',         hint: 'Waarschuwingskleur' },
    { name: '--color-error',           hint: 'Foutkleur' },
    { name: '--font-heading',          hint: 'Lettertype voor koppen' },
    { name: '--radius-field',          hint: 'Hoekradius inputs/knoppen' },
    { name: '--radius-box',            hint: 'Hoekradius kaarten' },
];

// Module-level refs set by initProjectStyle()
let picker: HTMLInputElement | null = null;
let hidden: HTMLInputElement | null = null;
let useDefault: HTMLInputElement | null = null;
let contrastWarning: HTMLElement | null = null;
let contrastWarningText: HTMLSpanElement | null = null;
let themeSelect: HTMLSelectElement | null = null;
let cssTextarea: HTMLTextAreaElement | null = null;
let lightRefs: PreviewRefs | null = null;
let darkRefs: PreviewRefs | null = null;
let syntaxHint: HTMLElement | null = null;

// ── Client-side contrast advisory ──────────────────────────────────
// Mirror of BL/Helpers/ColorHelpers.cs. Server validation is authoritative;
// this just shows the admin the problem before they hit submit.
function srgbToLinear(c: number): number {
    return c <= 0.04045 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
}

function relativeLuminance(hex: string): number {
    const r = parseInt(hex.slice(1, 3), 16) / 255;
    const g = parseInt(hex.slice(3, 5), 16) / 255;
    const b = parseInt(hex.slice(5, 7), 16) / 255;
    return 0.2126 * srgbToLinear(r) + 0.7152 * srgbToLinear(g) + 0.0722 * srgbToLinear(b);
}

function contrastRatio(hex1: string, hex2: string): number {
    const l1 = relativeLuminance(hex1);
    const l2 = relativeLuminance(hex2);
    return (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05);
}

function updateContrastWarning(hex: string): void {
    if (!contrastWarning || !contrastWarningText) return;
    if (!hex || !/^#[0-9A-Fa-f]{6}$/.test(hex)) {
        contrastWarning.classList.add('hidden');
        return;
    }
    const lightBase = contrastWarning.dataset.lightBase ?? '#FFFFFF';
    const darkBase  = contrastWarning.dataset.darkBase  ?? '#1A1430';
    const minRatio  = parseFloat(contrastWarning.dataset.minRatio ?? '3');
    const failsLight = contrastRatio(hex, lightBase) < minRatio;
    const failsDark  = contrastRatio(hex, darkBase)  < minRatio;
    if (!failsLight && !failsDark) {
        contrastWarning.classList.add('hidden');
        return;
    }
    const msg = failsLight && failsDark
        ? contrastWarning.dataset.msgBoth
        : failsLight
            ? contrastWarning.dataset.msgLight
            : contrastWarning.dataset.msgDark;
    contrastWarningText.textContent = msg ?? '';
    contrastWarning.classList.remove('hidden');
}

function buildPreview(host: HTMLDivElement, mode: 'light' | 'dark'): PreviewRefs {
    const shadow = host.attachShadow({ mode: 'open' });

    const themeStyle = document.createElement('style');
    themeStyle.textContent = SYNAPSE_BASE_THEMES_CSS + PROJECT_THEME_CSS + `
        .preview-root {
            display: block;
            padding: 1rem;
            background: var(--color-base-200);
            color: var(--color-base-content);
            font-family: var(--font-heading);
            border-radius: var(--radius-box);
        }
        /* Idea card — mirrors #ideas-list > a on the participant IdeaHub page */
        #ideas-list {
            margin-bottom: 0.75rem;
        }
        #ideas-list a {
            display: block;
            background: var(--color-base-100);
            color: var(--color-base-content);
            padding: 0.75rem 1rem;
            border-radius: var(--radius-field);
            border: 1px solid var(--color-base-300);
            text-decoration: none;
        }
        .preview-heading {
            font-weight: 700;
            font-size: 1.125rem;
            margin: 0 0 0.25rem 0;
        }
        .preview-body { margin: 0; font-size: 0.875rem; opacity: 0.85; }
        /* Accent strip — mirrors .accent-strip on IdeaHub and thank-you pages */
        .accent-strip {
            background: var(--color-primary);
            height: 2px;
            width: 2rem;
            border-radius: 9999px;
            margin-bottom: 0.5rem;
        }
        .preview-actions { display: flex; gap: 0.5rem; align-items: center; margin-top: 0.75rem; }
        /* Primary button — mirrors .btn.btn-primary (daisyUI) on participant pages */
        .btn.btn-primary {
            background: var(--color-primary);
            color: var(--color-primary-content);
            border: none;
            border-radius: var(--radius-field);
            padding: 0.4rem 0.85rem;
            font-size: 0.875rem;
            font-weight: 500;
            cursor: pointer;
        }
        /* Survey chat bubble — mirrors .chat-bubble-left on the chatform page */
        .chat-bubble-left {
            background: var(--color-base-100);
            border: 1px solid var(--color-base-300);
            color: var(--color-base-content);
            border-radius: var(--radius-box);
            border-top-left-radius: 0;
            padding: 0.5rem 0.85rem;
            font-size: 0.875rem;
            margin-top: 0.5rem;
            display: inline-block;
        }
    `;

    const primaryStyle = document.createElement('style');
    const userStyle = document.createElement('style');

    const wrapper = document.createElement('div');
    wrapper.className = 'preview-root';
    wrapper.setAttribute('data-project-theme', host.dataset.initialTheme ?? 'minimal');
    wrapper.setAttribute('data-theme', mode === 'dark' ? 'synapse-dark' : 'synapse-light');
    wrapper.innerHTML = `
        <div id="ideas-list">
            <a>
                <div class="accent-strip"></div>
                <h3 class="preview-heading">Voorbeeldproject</h3>
                <p class="preview-body">Dit is hoe deelnemers de stijl zien.</p>
            </a>
        </div>
        <div class="preview-actions">
            <button class="btn btn-primary" type="button">Idee toevoegen</button>
        </div>
        <div class="chat-bubble-left">Wat vind jij hiervan?</div>
    `;

    shadow.append(themeStyle, primaryStyle, userStyle, wrapper);
    return { shadow, themeStyle, primaryStyle, userStyle, wrapper };
}

// Mirror of UI-User/Helpers/ColorHelpers.cs — keep threshold in sync (0.45).
function hexRelativeLuminance(hex: string): number {
    const r = parseInt(hex.slice(1, 3), 16) / 255;
    const g = parseInt(hex.slice(3, 5), 16) / 255;
    const b = parseInt(hex.slice(5, 7), 16) / 255;
    const lin = (c: number): number => c <= 0.04045 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
    return 0.2126 * lin(r) + 0.7152 * lin(g) + 0.0722 * lin(b);
}

function contrastingForeground(hex: string): string {
    return hexRelativeLuminance(hex) > 0.45 ? '#000000' : '#FFFFFF';
}

// OKLab derivation mirror — must produce identical hex to BL/UI-User ColorHelpers
// ShiftLightness / ComplementaryHue. Used to drive the preview palette without
// relying on relative-color OKLCH support inside the shadow root.
function linearToSrgb(c: number): number {
    return c <= 0.0031308 ? 12.92 * c : 1.055 * Math.pow(c, 1.0 / 2.4) - 0.055;
}

function hexToOkLab(hex: string): { L: number; a: number; b: number } {
    const r = srgbToLinear(parseInt(hex.slice(1, 3), 16) / 255);
    const g = srgbToLinear(parseInt(hex.slice(3, 5), 16) / 255);
    const b = srgbToLinear(parseInt(hex.slice(5, 7), 16) / 255);

    const lLms = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b;
    const mLms = 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b;
    const sLms = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b;

    const l_ = Math.cbrt(lLms);
    const m_ = Math.cbrt(mLms);
    const s_ = Math.cbrt(sLms);

    return {
        L: 0.2104542553 * l_ + 0.7936177850 * m_ - 0.0040720468 * s_,
        a: 1.9779984951 * l_ - 2.4285922050 * m_ + 0.4505937099 * s_,
        b: 0.0259040371 * l_ + 0.7827717662 * m_ - 0.8086757660 * s_,
    };
}

function okLabToHex(L: number, a: number, b: number): string {
    const l_ = L + 0.3963377774 * a + 0.2158037573 * b;
    const m_ = L - 0.1055613458 * a - 0.0638541728 * b;
    const s_ = L - 0.0894841775 * a - 1.2914855480 * b;

    const lLms = l_ * l_ * l_;
    const mLms = m_ * m_ * m_;
    const sLms = s_ * s_ * s_;

    const r =  4.0767416621 * lLms - 3.3077115913 * mLms + 0.2309699292 * sLms;
    const g = -1.2684380046 * lLms + 2.6097574011 * mLms - 0.3413193965 * sLms;
    const bb = -0.0041960863 * lLms - 0.7034186147 * mLms + 1.7076147010 * sLms;

    const clamp = (c: number): number => Math.min(1, Math.max(0, c));
    const toHex = (c: number): string => Math.round(linearToSrgb(clamp(c)) * 255).toString(16).padStart(2, '0').toUpperCase();
    return `#${toHex(r)}${toHex(g)}${toHex(bb)}`;
}

function shiftLightness(hex: string, deltaL: number): string {
    const { L, a, b } = hexToOkLab(hex);
    return okLabToHex(Math.min(1, Math.max(0, L + deltaL)), a, b);
}

function complementaryHue(hex: string): string {
    const { L, a, b } = hexToOkLab(hex);
    return okLabToHex(L, -a, -b);
}

function applyPreview(): void {
    // Option value and text are both the enum member name ("Minimal","HighContrast"…);
    // lower-cased so it matches the CSS [data-project-theme="…"] selectors.
    const themeValue = themeSelect?.options[themeSelect.selectedIndex]?.text.toLowerCase() ?? 'minimal';

    // Use a <style> rule (not an inline style) so CustomCss can still override
    // --color-primary, matching the real page where both are stylesheet rules
    // and CustomCss comes last.
    const color = hidden?.value.trim() ?? '';
    let primaryStyleContent = '';
    if (color !== '') {
        const fg     = contrastingForeground(color);
        const hover  = shiftLightness(color, -0.06);
        const focus  = shiftLightness(color, -0.12);
        const accent = complementaryHue(color);
        primaryStyleContent =
            `[data-project-theme]{` +
            `--color-primary:${color};` +
            `--color-primary-content:${fg};` +
            `--color-primary-hover:${hover};` +
            `--color-primary-focus:${focus};` +
            `--color-accent:${accent}}`;
    }

    const userStyleContent = cssTextarea?.value ?? '';

    for (const refs of [lightRefs, darkRefs]) {
        if (!refs) continue;
        refs.wrapper.setAttribute('data-project-theme', themeValue);
        refs.primaryStyle.textContent = primaryStyleContent;
        refs.userStyle.textContent = userStyleContent;
    }
}

function insertAtCaret(text: string): void {
    if (!cssTextarea) return;
    const start = cssTextarea.selectionStart ?? cssTextarea.value.length;
    const end   = cssTextarea.selectionEnd   ?? cssTextarea.value.length;
    const before = cssTextarea.value.slice(0, start);
    const after  = cssTextarea.value.slice(end);
    const needsLeadingNewline = before.length > 0 && !before.endsWith('\n');
    const insertion = (needsLeadingNewline ? '\n' : '') + text;
    cssTextarea.value = before + insertion + after;
    const caret = before.length + insertion.length;
    cssTextarea.setSelectionRange(caret, caret);
    cssTextarea.focus();
    cssTextarea.dispatchEvent(new Event('input', { bubbles: true }));
}

function makeChip(label: string, title: string, onClick: () => void): HTMLButtonElement {
    const b = document.createElement('button');
    b.type = 'button';
    b.className = 'btn btn-xs btn-outline normal-case font-mono';
    b.textContent = label;
    b.title = title;
    b.addEventListener('click', onClick);
    return b;
}

function updateSyntaxHint(): void {
    if (!cssTextarea || !syntaxHint) return;
    const value = cssTextarea.value;
    if (value.trim().length === 0) {
        syntaxHint.textContent = '';
        syntaxHint.classList.remove('text-error', 'text-success');
        return;
    }
    const opens  = (value.match(/\{/g) ?? []).length;
    const closes = (value.match(/\}/g) ?? []).length;
    if (opens !== closes) {
        const template = syntaxHint.dataset.unbalancedLabel ?? '';
        syntaxHint.textContent = template
            .replace('{0}', String(opens))
            .replace('{1}', String(closes));
        syntaxHint.classList.add('text-error');
        syntaxHint.classList.remove('text-success');
    } else {
        syntaxHint.textContent = syntaxHint.dataset.balancedLabel ?? '';
        syntaxHint.classList.add('text-success');
        syntaxHint.classList.remove('text-error');
    }
}

export function initProjectStyle(): void {
    picker     = document.getElementById('primary-color-picker') as HTMLInputElement | null;
    hidden     = document.getElementById('PrimaryColor')         as HTMLInputElement | null;
    useDefault = document.getElementById('use-theme-color')      as HTMLInputElement | null;
    contrastWarning = document.getElementById('primary-color-contrast-warning');
    contrastWarningText = contrastWarning?.querySelector<HTMLSpanElement>('[data-warning-text]') ?? null;
    themeSelect  = document.getElementById('Theme')     as HTMLSelectElement   | null;
    cssTextarea  = document.getElementById('CustomCss') as HTMLTextAreaElement | null;
    syntaxHint   = document.getElementById('customcss-syntax-hint');

    const previewHostLight = document.getElementById('theme-preview-light') as HTMLDivElement | null;
    const previewHostDark  = document.getElementById('theme-preview-dark')  as HTMLDivElement | null;
    const variablesEl = document.querySelector<HTMLDivElement>('[data-helper-group="variables"]');
    const snippetsEl  = document.querySelector<HTMLDivElement>('[data-helper-group="snippets"]');

    lightRefs = previewHostLight ? buildPreview(previewHostLight, 'light') : null;
    darkRefs  = previewHostDark  ? buildPreview(previewHostDark,  'dark')  : null;

    if (picker && hidden && useDefault) {
        const initial = hidden.value.trim();
        if (initial === '') {
            useDefault.checked = true;
            picker.disabled = true;
        } else {
            useDefault.checked = false;
            picker.disabled = false;
            picker.value = initial;
            updateContrastWarning(initial);
        }

        useDefault.addEventListener('change', () => {
            if (useDefault!.checked) {
                picker!.disabled = true;
                hidden!.value = '';
                updateContrastWarning('');
            } else {
                picker!.disabled = false;
                hidden!.value = picker!.value;
                updateContrastWarning(picker!.value);
            }
        });

        picker.addEventListener('input', () => {
            if (!useDefault!.checked) hidden!.value = picker!.value;
            updateContrastWarning(useDefault!.checked ? '' : picker!.value);
        });

        // Topic swatches: a click just fills the picker with a curated hex. Dispatching
        // a synthetic 'input' reuses every existing picker listener (hidden value,
        // contrast warning, preview) — no duplicated wiring.
        document.getElementById('primary-color-swatches')
            ?.querySelectorAll<HTMLButtonElement>('[data-swatch-color]')
            .forEach(btn => btn.addEventListener('click', () => {
                const hex = btn.dataset.swatchColor;
                if (!hex) return;
                useDefault!.checked = false;
                picker!.disabled = false;
                picker!.value = hex;
                picker!.dispatchEvent(new Event('input', { bubbles: true }));
            }));
    }

    themeSelect?.addEventListener('change', applyPreview);
    picker?.addEventListener('input', applyPreview);
    useDefault?.addEventListener('change', applyPreview);
    cssTextarea?.addEventListener('input', applyPreview);
    applyPreview();

    // ── Custom-CSS helper ──────────────────────────────────────────────
    const SNIPPETS: ReadonlyArray<{ label: string; body: string }> = [
        {
            label: snippetsEl?.dataset.labelPrimaryColor ?? 'Override primaire kleur',
            body: '[data-project-theme] {\n    --color-primary: #16A34A;\n}\n',
        },
        {
            label: snippetsEl?.dataset.labelRoundedButtons ?? 'Afgeronde knoppen',
            body: '[data-project-theme] .btn.btn-primary {\n    border-radius: 9999px;\n}\n',
        },
        {
            label: snippetsEl?.dataset.labelIdeaCardShadow ?? 'Schaduw op ideekaarten',
            body: '[data-project-theme] #ideas-list a {\n    box-shadow: 0 4px 12px rgba(0,0,0,0.08);\n}\n',
        },
        {
            label: snippetsEl?.dataset.labelHeadingFont ?? 'Koplettertype',
            body: '[data-project-theme] {\n    --font-heading: "Georgia", serif;\n}\n',
        },
        {
            label: snippetsEl?.dataset.labelRoundedButtonsEverywhere ?? 'Afgeronde knoppen overal',
            body: '[data-project-theme] {\n    --radius-field: 9999px;\n}\n',
        },
        {
            label: snippetsEl?.dataset.labelBrandGradientHeader ?? 'Kleurverloop in kopbal',
            body: '[data-project-theme] header {\n    background: linear-gradient(135deg, var(--color-primary), var(--color-primary-hover));\n}\n',
        },
        {
            label: snippetsEl?.dataset.labelStrongerFocusRing ?? 'Sterkere focusring',
            body: '[data-project-theme] :focus-visible {\n    outline: 3px solid var(--color-primary);\n}\n',
        },
    ];

    if (variablesEl) {
        for (const v of CSS_VARIABLES) {
            variablesEl.appendChild(makeChip(v.name, v.hint, () => insertAtCaret(`${v.name}: ;`)));
        }
    }

    if (snippetsEl) {
        for (const s of SNIPPETS) {
            snippetsEl.appendChild(makeChip(s.label, s.body, () => insertAtCaret(s.body)));
        }
    }

    cssTextarea?.addEventListener('input', updateSyntaxHint);
    updateSyntaxHint();

    const resetBtn = document.getElementById('customcss-reset') as HTMLButtonElement | null;
    resetBtn?.addEventListener('click', () => {
        const msg = resetBtn.dataset.confirm ?? '';
        if (msg && !confirm(msg)) return;
        if (!cssTextarea) return;
        cssTextarea.value = '';
        cssTextarea.dispatchEvent(new Event('input', { bubbles: true }));
    });
}
