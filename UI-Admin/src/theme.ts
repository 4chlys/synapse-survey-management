const STORAGE_KEY = 'admin-theme';
const LIGHT = 'synapse-light';
const DARK = 'synapse-dark';

function applyTheme(theme: string): void {
    document.documentElement.setAttribute('data-theme', theme);
    try { localStorage.setItem(STORAGE_KEY, theme); } catch { /* quota or privacy block */ }
    syncToggle(theme);
}

function syncToggle(theme: string): void {
    const btn  = document.getElementById('theme-toggle');
    const sun  = document.getElementById('icon-sun');
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

export function initTheme(): void {
    const saved = localStorage.getItem(STORAGE_KEY);
    const valid = (saved === LIGHT || saved === DARK) ? saved : null;
    const preferred = window.matchMedia('(prefers-color-scheme: dark)').matches ? DARK : LIGHT;
    const theme = valid ?? preferred;
    document.documentElement.setAttribute('data-theme', theme);
    syncToggle(theme);
}

export function toggleTheme(): void {
    const current = document.documentElement.getAttribute('data-theme') ?? LIGHT;
    applyTheme(current === DARK ? LIGHT : DARK);
}

function setup(): void {
    initTheme();
    document.getElementById('theme-toggle')?.addEventListener('click', toggleTheme);
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', setup);
} else {
    setup();
}
