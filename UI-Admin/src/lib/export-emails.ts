// Participant-email export: prefers showSaveFilePicker, falls back to the
// dropdown links that the .cshtml renders inside `[data-export-fallback]`.

interface SaveFilePickerOptions {
    suggestedName?: string;
    types?: { description: string; accept: Record<string, string[]> }[];
}
interface FileSystemFileHandle {
    name: string;
    createWritable(): Promise<WritableStream>;
}
declare global {
    interface Window {
        showSaveFilePicker?: (opts?: SaveFilePickerOptions) => Promise<FileSystemFileHandle>;
    }
}

export function initExportEmails(): void {
    const root = document.querySelector<HTMLElement>('[data-export-emails]');
    if (!root) return;

    const baseUrl = root.dataset.exportUrl;
    const trigger = root.querySelector<HTMLElement>('[data-export-trigger]');
    const fallback = root.querySelector<HTMLElement>('[data-export-fallback]');
    if (!baseUrl || !trigger || !fallback) return;

    const failedMsg = root.dataset.failedMessage ?? 'Export failed';
    const textFormatDesc = root.dataset.textFormatDescription ?? 'Plain text';

    const types: { description: string; accept: Record<string, string[]> }[] = [
        { description: 'CSV', accept: { 'text/csv': ['.csv'] } },
        { description: textFormatDesc, accept: { 'text/plain': ['.txt'] } },
        { description: 'Excel', accept: { 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': ['.xlsx'] } },
    ];
    const extToFormat: Record<string, string> = { '.csv': 'csv', '.txt': 'txt', '.xlsx': 'xlsx' };

    if (typeof window.showSaveFilePicker !== 'function') {
        trigger.addEventListener('click', () => fallback.classList.toggle('hidden'));
        return;
    }

    trigger.addEventListener('click', async () => {
        let handle: FileSystemFileHandle;
        try {
            handle = await window.showSaveFilePicker!({
                suggestedName: `participants-${root.dataset.projectId}`,
                types,
            });
        } catch (e) {
            if ((e as DOMException)?.name === 'AbortError') return;
            console.error(e);
            return;
        }
        const name = handle.name.toLowerCase();
        const ext = Object.keys(extToFormat).find(x => name.endsWith(x)) ?? '.csv';
        const format = extToFormat[ext];

        const resp = await fetch(`${baseUrl}&format=${format}`);
        if (!resp.ok) {
            alert(failedMsg);
            return;
        }

        const writable = await handle.createWritable();
        await resp.body!.pipeTo(writable);
    });
}
