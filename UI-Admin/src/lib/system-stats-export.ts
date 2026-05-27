export function initSystemStatsExport(): void {
    const modal = document.getElementById('system-export-modal') as HTMLDialogElement | null;
    const dlBtn = document.getElementById('system-export-download') as HTMLButtonElement | null;
    if (!modal || !dlBtn) return;

    const baseUrl = modal.dataset.exportUrl;
    if (!baseUrl) return;

    const formatSelect = document.getElementById('system-export-format') as HTMLSelectElement | null;
    const graphicsCheckbox = document.querySelector<HTMLInputElement>('[data-system-graphics]');
    const sectionCheckboxes = document.querySelectorAll<HTMLInputElement>('[data-system-section]');

    formatSelect?.addEventListener('input', () => {
        if (graphicsCheckbox) graphicsCheckbox.disabled = formatSelect.value !== 'xlsx';
        if (sectionCheckboxes.length > 0) {
            const disabled = formatSelect.value === 'png-zip';
            sectionCheckboxes.forEach(cb => { cb.disabled = disabled; });
        }
    });

    dlBtn.addEventListener('click', () => {
        const format = formatSelect?.value ?? 'xlsx';
        const checked = Array.from(
            document.querySelectorAll<HTMLInputElement>('[data-system-section]:checked')
        ).map(cb => cb.dataset.systemSection);

        if (format !== 'png-zip' && checked.length === 0) {
            alert('Selecteer minstens één sectie.');
            return;
        }

        const graphicsEnabled = graphicsCheckbox !== null && graphicsCheckbox.checked && !graphicsCheckbox.disabled;
        const sections = checked.join(',');
        const separator = baseUrl.includes('?') ? '&' : '?';
        window.location.href = `${baseUrl}${separator}format=${encodeURIComponent(format)}&sections=${encodeURIComponent(sections)}&includeGraphics=${graphicsEnabled}`;
        modal.close();
    });
}
