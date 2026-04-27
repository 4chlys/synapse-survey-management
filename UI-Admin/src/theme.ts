const STORAGE_KEY = 'admin-theme';
const LIGHT = 'synapse-light';
const DARK = 'synapse-dark';

export function initTheme(): void {
    const saved = localStorage.getItem(STORAGE_KEY);
    const preferred = window.matchMedia('(prefers-color-scheme: dark)').matches ? DARK : LIGHT;
    document.documentElement.setAttribute('data-theme', saved ?? preferred);
    updateToggleIcon(saved ?? preferred);
}

export function toggleTheme(): void {
    const current = document.documentElement.getAttribute('data-theme') ?? LIGHT;
    const next = current === DARK ? LIGHT : DARK;
    document.documentElement.setAttribute('data-theme', next);
    localStorage.setItem(STORAGE_KEY, next);
    updateToggleIcon(next);
}

function updateToggleIcon(theme: string): void {
    const btn = document.getElementById('theme-toggle');
    if (btn) btn.setAttribute('aria-label', theme === DARK ? 'Schakel naar licht' : 'Schakel naar donker');
    const icon = document.getElementById('theme-icon');
    if (icon) icon.textContent = theme === DARK ? '☀️' : '🌙';
}

document.addEventListener('DOMContentLoaded', () => {
    initTheme();
    document.getElementById('theme-toggle')?.addEventListener('click', toggleTheme);
});
