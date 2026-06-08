export function getAntiForgeryToken(): string {
    return document.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]')?.value ?? '';
}

// Triggers a file download without navigating the top window. Using a `download`
// anchor (instead of `window.location.href`) avoids firing `beforeunload`, which on
// the dashboard would tear down the chart widgets before the navigation is cancelled.
export function downloadFile(url: string): void {
    const a = document.createElement('a');
    a.href = url;
    a.download = '';
    a.style.display = 'none';
    document.body.appendChild(a);
    a.click();
    a.remove();
}

// Renders the chart onto a white background and triggers a PNG download.
export function downloadCanvasPng(canvas: HTMLCanvasElement, fileName: string): void {
    const offscreen = document.createElement('canvas');
    offscreen.width = canvas.width;
    offscreen.height = canvas.height;
    const ctx = offscreen.getContext('2d')!;
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, offscreen.width, offscreen.height);
    ctx.drawImage(canvas, 0, 0);
    const link = document.createElement('a');
    link.download = fileName;
    link.href = offscreen.toDataURL('image/png');
    link.click();
}

// Builds the ghost download-icon button used by the chart widgets.
export function createDownloadButton(onClick: () => void): HTMLButtonElement {
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'btn btn-ghost btn-xs absolute top-2 right-2';
    btn.title = 'Download PNG';
    btn.innerHTML = '<svg width="14" height="14" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24"><path d="M12 3v12m0 0l-4-4m4 4l4-4M4 17v2a2 2 0 002 2h12a2 2 0 002-2v-2"/></svg>';
    btn.addEventListener('click', onClick);
    return btn;
}

// Attaches a download button bound to a fixed canvas (for widgets whose canvas persists).
export function attachDownloadButton(container: HTMLElement, canvas: HTMLCanvasElement, fileName: string): void {
    const btn = createDownloadButton(() => downloadCanvasPng(canvas, fileName));
    if (!container.style.position) container.style.position = 'relative';
    container.appendChild(btn);
}
