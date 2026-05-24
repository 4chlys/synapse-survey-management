const STORAGE_KEY = 'synapse-theme';
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
        // Explicit prior choice — enforce it (also re-sets data-theme + cookie).
        applyTheme(saved);
    } else {
        // No choice yet — the OS preference rules via daisyUI's prefersdark in
        // pure CSS. Don't write data-theme/cookie/localStorage; only mirror the
        // effective theme onto the toggle, and keep it synced if the OS flips.
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
    localStorage.setItem(STORAGE_KEY, theme);
    // Cookie lets the server set data-theme before first paint, preventing flash
    document.cookie = `${STORAGE_KEY}=${theme}; path=/; max-age=31536000; SameSite=Lax`;
    syncToggle(theme);
}

function syncToggle(theme: string): void {
    const sun  = document.getElementById('icon-sun');
    const moon = document.getElementById('icon-moon');
    const knob = document.getElementById('toggle-knob');
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
