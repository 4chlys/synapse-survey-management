const CONTAINER_ID = 'toast-container';

function getContainer(): HTMLElement {
    let el = document.getElementById(CONTAINER_ID);
    if (!el) {
        el = document.createElement('div');
        el.id = CONTAINER_ID;
        el.className = 'fixed top-4 right-4 flex flex-col gap-2 z-50';
        document.body.appendChild(el);
    }
    return el;
}

const ICON_SUCCESS = '<svg width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24" class="flex-shrink-0"><path d="M5 13l4 4L19 7"/></svg>';
const ICON_ERROR = '<svg width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24" class="flex-shrink-0"><circle cx="12" cy="12" r="10"/><path d="M12 8v4M12 16h.01"/></svg>';

export function toast(message: string, kind: 'success' | 'error', durationMs = 2500): void {
    const container = getContainer();
    const chip = document.createElement('div');
    const alertClass = kind === 'success' ? 'alert-success' : 'alert-error';
    const icon = kind === 'success' ? ICON_SUCCESS : ICON_ERROR;
    chip.className = `alert ${alertClass} max-w-sm shadow-lg flex gap-2 transition-opacity duration-300`;
    chip.innerHTML = `${icon}<span>${message}</span>`;
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
