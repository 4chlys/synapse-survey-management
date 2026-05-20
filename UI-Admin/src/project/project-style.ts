// Wires the project "Stijl" section: colour picker + "use theme default"
// checkbox both feed a single hidden input (PrimaryColor) so an empty value
// reaches the server when the user wants the theme's own colour.

const picker     = document.getElementById('primary-color-picker') as HTMLInputElement | null;
const hidden     = document.getElementById('PrimaryColor')         as HTMLInputElement | null;
const useDefault = document.getElementById('use-theme-color')      as HTMLInputElement | null;

if (picker && hidden && useDefault) {
    const initial = hidden.value.trim();
    if (initial === '') {
        useDefault.checked = true;
        picker.disabled = true;
    } else {
        useDefault.checked = false;
        picker.disabled = false;
        picker.value = initial;
    }

    useDefault.addEventListener('change', () => {
        if (useDefault.checked) {
            picker.disabled = true;
            hidden.value = '';
        } else {
            picker.disabled = false;
            hidden.value = picker.value;
        }
    });

    picker.addEventListener('input', () => {
        if (!useDefault.checked) hidden.value = picker.value;
    });
}

// Mirror of UI-User/src/main.css per-project-theme blocks. Keep in sync
// when UI-User adds or changes a theme. Injected into the shadow root so
// the preview renders identically to the participant page.
const PROJECT_THEME_CSS = `
[data-project-theme="default"] {
    --color-primary: oklch(52% 0.22 277);
    --color-primary-content: oklch(100% 0 0);
    --color-base-100: oklch(100% 0 0);
    --color-base-200: oklch(97% 0.01 277);
    --color-base-300: oklch(91% 0.02 277);
    --color-base-content: oklch(15% 0.04 278);
    --font-heading: 'Space Grotesk', ui-sans-serif, sans-serif;
    --radius-field: 0.75rem;
    --radius-box: 1rem;
}
[data-project-theme="playful"] {
    --color-primary: oklch(78% 0.16 80);
    --color-primary-content: oklch(20% 0.05 80);
    --color-base-100: oklch(100% 0 0);
    --color-base-200: oklch(97% 0.01 277);
    --color-base-300: oklch(91% 0.02 277);
    --color-base-content: oklch(15% 0.04 278);
    --font-heading: 'Nunito', ui-sans-serif, sans-serif;
    --radius-field: 1rem;
    --radius-box: 1.25rem;
}
[data-project-theme="professional"] {
    --color-primary: oklch(35% 0.04 250);
    --color-primary-content: oklch(100% 0 0);
    --color-base-100: oklch(100% 0 0);
    --color-base-200: oklch(97% 0.01 277);
    --color-base-300: oklch(91% 0.02 277);
    --color-base-content: oklch(15% 0.04 278);
    --font-heading: 'Source Sans 3', ui-sans-serif, sans-serif;
    --radius-field: 0.25rem;
    --radius-box: 0.375rem;
}
[data-project-theme="highcontrast"] {
    --color-primary: oklch(0% 0 0);
    --color-primary-content: oklch(100% 0 0);
    --color-base-100: oklch(100% 0 0);
    --color-base-200: oklch(100% 0 0);
    --color-base-300: oklch(80% 0 0);
    --color-base-content: oklch(0% 0 0);
    --color-warning: oklch(95% 0.20 100);
    --font-heading: 'Arial', sans-serif;
    --radius-field: 0;
    --radius-box: 0;
}
`;

// ── Live theme preview ─────────────────────────────────────────────
const themeSelect = document.getElementById('Theme')              as HTMLSelectElement | null;
const cssTextarea = document.getElementById('CustomCss')          as HTMLTextAreaElement | null;
const previewHost = document.getElementById('theme-preview-host') as HTMLDivElement | null;

interface PreviewRefs {
    shadow:     ShadowRoot;
    themeStyle: HTMLStyleElement;
    primaryStyle: HTMLStyleElement;
    userStyle:  HTMLStyleElement;
    wrapper:    HTMLDivElement;
}

function buildPreview(host: HTMLDivElement): PreviewRefs {
    const shadow = host.attachShadow({ mode: 'open' });

    const themeStyle = document.createElement('style');
    themeStyle.textContent = PROJECT_THEME_CSS + `
        .preview-root {
            display: block;
            padding: 1rem;
            background: var(--color-base-200);
            color: var(--color-base-content);
            font-family: var(--font-heading);
            border-radius: var(--radius-box);
        }
        .preview-card {
            background: var(--color-base-100);
            color: var(--color-base-content);
            padding: 0.75rem 1rem;
            border-radius: var(--radius-box);
            margin-bottom: 0.75rem;
            border: 1px solid var(--color-base-300);
        }
        .preview-heading {
            font-weight: 700;
            font-size: 1.125rem;
            margin: 0 0 0.25rem 0;
        }
        .preview-body { margin: 0; font-size: 0.875rem; opacity: 0.85; }
        .preview-actions { display: flex; gap: 0.5rem; align-items: center; }
        .btn-primary {
            background: var(--color-primary);
            color: var(--color-primary-content);
            border: none;
            border-radius: var(--radius-field);
            padding: 0.4rem 0.85rem;
            font-size: 0.875rem;
            font-weight: 500;
            cursor: pointer;
        }
        .chip {
            background: var(--color-base-100);
            border: 1px solid var(--color-base-300);
            color: var(--color-base-content);
            border-radius: var(--radius-field);
            padding: 0.2rem 0.5rem;
            font-size: 0.75rem;
        }
    `;

    const primaryStyle = document.createElement('style');
    const userStyle = document.createElement('style');

    const wrapper = document.createElement('div');
    wrapper.className = 'preview-root';
    wrapper.setAttribute('data-project-theme', host.dataset.initialTheme ?? 'default');
    wrapper.innerHTML = `
        <div class="preview-card">
            <h3 class="preview-heading">Voorbeeldproject</h3>
            <p class="preview-body">Dit is hoe deelnemers de stijl zien.</p>
        </div>
        <div class="preview-actions">
            <button class="btn-primary" type="button">Primaire knop</button>
            <span class="chip">Chip</span>
        </div>
    `;

    shadow.append(themeStyle, primaryStyle, userStyle, wrapper);
    return { shadow, themeStyle, primaryStyle, userStyle, wrapper };
}

const refs = previewHost ? buildPreview(previewHost) : null;

function applyPreview(): void {
    if (!refs) return;
    // GetEnumSelectList sets option values to integer strings ("0","1"…);
    // the option text is the enum member name ("Default","Playful"…).
    const themeValue = themeSelect?.options[themeSelect.selectedIndex]?.text.toLowerCase() ?? 'default';
    refs.wrapper.setAttribute('data-project-theme', themeValue);

    // Use a <style> rule (not an inline style) so CustomCss can still override
    // --color-primary, matching the real page where both are stylesheet rules
    // and CustomCss comes last.
    const color = hidden?.value.trim() ?? '';
    refs.primaryStyle.textContent = color === '' ? '' : `[data-project-theme]{--color-primary:${color}}`;

    refs.userStyle.textContent = cssTextarea?.value ?? '';
}

themeSelect?.addEventListener('change', applyPreview);
picker?.addEventListener('input', applyPreview);
useDefault?.addEventListener('change', applyPreview);
cssTextarea?.addEventListener('input', applyPreview);
applyPreview();

// ── Custom-CSS helper ──────────────────────────────────────────────
const variablesEl = document.querySelector<HTMLDivElement>('[data-helper-group="variables"]');
const snippetsEl  = document.querySelector<HTMLDivElement>('[data-helper-group="snippets"]');
const syntaxHint  = document.getElementById('customcss-syntax-hint');

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

const SNIPPETS: ReadonlyArray<{ label: string; body: string }> = [
    {
        label: 'Override primaire kleur',
        body: '[data-project-theme] {\n    --color-primary: #16A34A;\n}\n',
    },
    {
        label: 'Aangepaste knopstijl',
        body: '[data-project-theme] .btn-primary {\n    border-radius: 9999px;\n}\n',
    },
    {
        label: 'Aangepaste kaart',
        body: '[data-project-theme] .preview-card {\n    box-shadow: 0 4px 12px rgba(0,0,0,0.08);\n}\n',
    },
    {
        label: 'Koplettertype',
        body: '[data-project-theme] {\n    --font-heading: "Georgia", serif;\n}\n',
    },
];

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

cssTextarea?.addEventListener('input', updateSyntaxHint);
updateSyntaxHint();
