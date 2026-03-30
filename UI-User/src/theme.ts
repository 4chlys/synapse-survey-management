const STORAGE_KEY = 'synapse-theme';
const LIGHT = 'synapse-light';
const DARK = 'synapse-dark';

export function initTheme(): void {
    const saved = localStorage.getItem(STORAGE_KEY) ?? LIGHT;
    applyTheme(saved);

    const btn = document.getElementById('theme-toggle');
    if (!btn) return;

    btn.addEventListener('click', () => {
        const current = document.documentElement.getAttribute('data-theme');
        applyTheme(current === DARK ? LIGHT : DARK);
    });
}

function applyTheme(theme: string): void {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem(STORAGE_KEY, theme);

    const sun  = document.getElementById('icon-sun');
    const moon = document.getElementById('icon-moon');
    if (!sun || !moon) return;

    if (theme === DARK) {
        sun.classList.remove('hidden');
        moon.classList.add('hidden');
    } else {
        sun.classList.add('hidden');
        moon.classList.remove('hidden');
    }
}
