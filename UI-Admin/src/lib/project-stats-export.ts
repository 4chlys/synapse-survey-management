import { downloadFile } from './utils';

export function initProjectStatsExport(): void {
    const modal = document.getElementById('project-export-modal') as HTMLDialogElement | null;
    const dlBtn = document.getElementById('project-export-download') as HTMLButtonElement | null;
    if (!modal || !dlBtn) return;

    const baseUrl = modal.dataset.exportUrl;
    if (!baseUrl) return;

    const formatSelect = document.getElementById('project-export-format') as HTMLSelectElement | null;
    const graphicsCheckbox = document.querySelector<HTMLInputElement>('[data-project-graphics]');

    formatSelect?.addEventListener('input', () => {
        if (graphicsCheckbox) graphicsCheckbox.disabled = formatSelect.value !== 'xlsx';
    });

    dlBtn.addEventListener('click', () => {
        const checked = Array.from(
            document.querySelectorAll<HTMLInputElement>('[data-project-section]:checked')
        ).map(cb => cb.dataset.projectSection);

        if (checked.length === 0) {
            alert('Selecteer minstens één sectie.');
            return;
        }

        const format = formatSelect?.value ?? 'xlsx';
        const graphicsEnabled = graphicsCheckbox !== null && graphicsCheckbox.checked && !graphicsCheckbox.disabled;
        const sections = checked.join(',');
        const separator = baseUrl.includes('?') ? '&' : '?';
        downloadFile(`${baseUrl}${separator}format=${encodeURIComponent(format)}&sections=${encodeURIComponent(sections)}&includeGraphics=${graphicsEnabled}`);
        modal.close();
    });
}
