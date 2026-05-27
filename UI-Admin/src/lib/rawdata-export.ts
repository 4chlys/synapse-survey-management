export function initRawdataExport(): void {
    const modal = document.getElementById('rawdata-modal') as HTMLDialogElement | null;
    const dlBtn = document.getElementById('rawdata-download') as HTMLButtonElement | null;
    if (!modal || !dlBtn) return;

    const baseUrl = modal.dataset.exportUrl;
    if (!baseUrl) return;

    dlBtn.addEventListener('click', () => {
        const checked = Array.from(
            document.querySelectorAll<HTMLInputElement>('[data-rawdata-table]:checked')
        ).map(cb => cb.dataset.rawdataTable);

        if (checked.length === 0) {
            alert('Selecteer minstens één gegevenstype.');
            return;
        }

        const format = (document.getElementById('rawdata-format') as HTMLSelectElement).value;
        const tables = checked.join(',');
        const separator = baseUrl.includes('?') ? '&' : '?';
        window.location.href = `${baseUrl}${separator}format=${format}&tables=${encodeURIComponent(tables)}`;
        modal.close();
    });
}
