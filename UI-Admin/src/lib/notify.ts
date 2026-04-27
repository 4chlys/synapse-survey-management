const CONTAINER_ID = 'toast-container';

function getContainer(): HTMLElement {
    let el = document.getElementById(CONTAINER_ID);
    if (!el) {
        el = document.createElement('div');
        el.id = CONTAINER_ID;
        el.className = 'fixed bottom-4 right-4 flex flex-col gap-2 z-50';
        document.body.appendChild(el);
    }
    return el;
}

export function toast(message: string, kind: 'success' | 'error', durationMs = 2500): void {
    const container = getContainer();
    const chip = document.createElement('div');
    const colorClass = kind === 'success'
        ? 'bg-gray-800 border-gray-500 text-white'
        : 'bg-red-900 border-red-500 text-white';
    chip.className = `border px-4 py-2 text-xs transition-opacity duration-300 ${colorClass}`;
    chip.textContent = message;
    container.appendChild(chip);

    setTimeout(() => {
        chip.style.opacity = '0';
        setTimeout(() => chip.remove(), 300);
    }, durationMs);
}

export function bindDirtyTracking(form: HTMLFormElement): void {
    window.addEventListener('beforeunload', (e: BeforeUnloadEvent) => {
        if (form.dataset.dirty === 'true') e.preventDefault();
    });
    form.addEventListener('input', () => { form.dataset.dirty = 'true'; });
    form.addEventListener('change', () => { form.dataset.dirty = 'true'; });
    form.addEventListener('submit', () => { form.dataset.dirty = 'false'; });
}
