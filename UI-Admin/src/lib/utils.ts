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
