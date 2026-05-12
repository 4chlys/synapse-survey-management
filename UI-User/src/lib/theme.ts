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
    // Cookie lets the server set data-theme before first paint, preventing flash
    document.cookie = `${STORAGE_KEY}=${theme}; path=/; max-age=31536000; SameSite=Lax`;

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
