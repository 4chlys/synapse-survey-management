// Auto-removes TempData success/error toasts after 5 seconds.

const TIMEOUT_MS = 5000;

export function initAlerts(): void {
    document.querySelectorAll<HTMLElement>('[data-auto-dismiss]').forEach(el => {
        setTimeout(() => el.remove(), TIMEOUT_MS);
    });
}
