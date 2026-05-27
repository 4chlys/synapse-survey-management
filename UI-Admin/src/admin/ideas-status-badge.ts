let _statusSelect: HTMLSelectElement | null = null;
let _container: HTMLElement | null = null;

function syncActiveState(): void {
    const currentVal = _statusSelect?.value ?? '';
    _container?.querySelectorAll<HTMLButtonElement>('[data-status-filter]').forEach(btn => {
        btn.dataset.active = btn.dataset.statusFilter === currentVal && currentVal !== ''
            ? 'true'
            : 'false';
    });
}

export function initIdeasStatusBadges(): void {
    _container = document.querySelector<HTMLElement>('[data-status-badges]');
    if (!_container) return;

    const form = document.querySelector<HTMLFormElement>('[data-ideas-filter]');
    if (!form) return;

    _statusSelect = form.querySelector<HTMLSelectElement>('select[name="status"]');
    if (!_statusSelect) return;

    _container.querySelectorAll<HTMLButtonElement>('[data-status-filter]').forEach(btn => {
        btn.addEventListener('click', () => {
            const val = btn.dataset.statusFilter ?? '';
            _statusSelect!.value = _statusSelect!.value === val ? '' : val;
            syncActiveState();
            form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
        });
    });
}
