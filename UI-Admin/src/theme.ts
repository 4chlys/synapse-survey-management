const STORAGE_KEY = 'admin-theme';
const LIGHT = 'synapse-light';
const DARK = 'synapse-dark';

export function initTheme(): void {
    const saved = localStorage.getItem(STORAGE_KEY);
    const valid = (saved === LIGHT || saved === DARK) ? saved : null;
    const preferred = window.matchMedia('(prefers-color-scheme: dark)').matches ? DARK : LIGHT;
    const theme = valid ?? preferred;
    document.documentElement.setAttribute('data-theme', theme);
    updateToggleIcon(theme);
}

export function toggleTheme(): void {
    const current = document.documentElement.getAttribute('data-theme') ?? LIGHT;
    const next = current === DARK ? LIGHT : DARK;
    document.documentElement.setAttribute('data-theme', next);
    try { localStorage.setItem(STORAGE_KEY, next); } catch { /* quota or privacy block */ }
    updateToggleIcon(next);
}

function updateToggleIcon(theme: string): void {
    const btn = document.getElementById('theme-toggle');
    if (btn) btn.setAttribute('aria-label', theme === DARK ? 'Schakel naar licht' : 'Schakel naar donker');
    const icon = document.getElementById('theme-icon');
    if (icon) icon.textContent = theme === DARK ? '☀️' : '🌙';
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
