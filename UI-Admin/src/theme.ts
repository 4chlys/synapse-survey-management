const STORAGE_KEY = 'admin-theme';
const LIGHT = 'synapse-light';
const DARK = 'synapse-dark';

const darkQuery = window.matchMedia('(prefers-color-scheme: dark)');

/** The theme currently in effect: the explicit data-theme if set, else the OS preference. */
function effectiveTheme(): string {
    const attr = document.documentElement.getAttribute('data-theme');
    if (attr === LIGHT || attr === DARK) return attr;
    return darkQuery.matches ? DARK : LIGHT;
}

export function initTheme(): void {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved === LIGHT || saved === DARK) {
        applyTheme(saved);
    } else {
        syncToggle(effectiveTheme());
        darkQuery.addEventListener('change', () => {
            if (!localStorage.getItem(STORAGE_KEY)) syncToggle(effectiveTheme());
        });
    }

    const btn = document.getElementById('theme-toggle');
    if (!btn) return;

    btn.addEventListener('click', () => {
        applyTheme(effectiveTheme() === DARK ? LIGHT : DARK);
    });
}

function applyTheme(theme: string): void {
    document.documentElement.setAttribute('data-theme', theme);
    try {
        localStorage.setItem(STORAGE_KEY, theme);
    } catch { /* quota or privacy block */ }
    // Cookie lets the server set data-theme before first paint, preventing flash
    document.cookie = `${STORAGE_KEY}=${theme}; path=/; max-age=31536000; SameSite=Lax`;
    syncToggle(theme);
}

function syncToggle(theme: string): void {
    const btn = document.getElementById('theme-toggle');
    const sun = document.getElementById('icon-sun');
    const moon = document.getElementById('icon-moon');
    const knob = document.getElementById('toggle-knob');
    if (btn) btn.setAttribute('aria-label', theme === DARK ? 'Schakel naar licht' : 'Schakel naar donker');
    if (!sun || !moon) return;

    if (theme === DARK) {
        sun.classList.remove('hidden');
        moon.classList.add('hidden');
        knob?.classList.add('translate-x-7');
        knob?.classList.remove('translate-x-0');
    } else {
        sun.classList.add('hidden');
        moon.classList.remove('hidden');
        knob?.classList.remove('translate-x-7');
        knob?.classList.add('translate-x-0');
    }
}
