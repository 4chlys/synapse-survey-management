function initIdeasStatusBadges(): void {
    const container = document.querySelector<HTMLElement>('[data-status-badges]');
    if (!container) return;

    const form = document.querySelector<HTMLFormElement>('[data-ideas-filter]');
    if (!form) return;

    const statusSelect = form.querySelector<HTMLSelectElement>('select[name="status"]');
    if (!statusSelect) return;

    function syncActiveState(): void {
        const currentVal = statusSelect!.value;
        container!.querySelectorAll<HTMLButtonElement>('[data-status-filter]').forEach(btn => {
            btn.dataset.active = btn.dataset.statusFilter === currentVal && currentVal !== ''
                ? 'true'
                : 'false';
        });
    }

    container.querySelectorAll<HTMLButtonElement>('[data-status-filter]').forEach(btn => {
        btn.addEventListener('click', () => {
            const val = btn.dataset.statusFilter ?? '';
            // toggle: clicking the active filter clears it
            statusSelect.value = statusSelect.value === val ? '' : val;
            syncActiveState();
            form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
        });
    });
}

document.addEventListener('DOMContentLoaded', initIdeasStatusBadges);
