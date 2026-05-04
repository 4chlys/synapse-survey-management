export function initSurveyDistributionWidget(el: HTMLElement): void {
    const surveyUrl = el.dataset.surveyUrl ?? '';
    const projectId = el.dataset.projectId ?? '';
    const qrSrc = `https://chart.googleapis.com/chart?cht=qr&chs=200x200&chl=${encodeURIComponent(surveyUrl)}`;

    // Build structure with innerHTML for trusted static markup only
    el.innerHTML = `
        <p class="text-xs text-base-content/50 mb-2">Survey-link</p>
        <div id="url-row-${projectId}" class="flex items-center gap-2 mb-3">
            <span id="url-text-${projectId}" class="text-base-content text-xs break-all flex-1"></span>
            <button id="copy-btn-${projectId}"
                class="btn btn-ghost btn-xs whitespace-nowrap">
                Kopieer
            </button>
        </div>
        <img id="qr-img-${projectId}" alt="QR code" class="rounded mb-2" width="160" height="160" />
        <a id="qr-link-${projectId}" download="qr-code.png" class="text-xs text-base-content/50 hover:text-base-content underline">
            QR downloaden
        </a>
    `;

    // Set user-controlled values via DOM properties (XSS-safe)
    const urlText = el.querySelector<HTMLElement>(`#url-text-${projectId}`);
    if (urlText) urlText.textContent = surveyUrl;

    const img = el.querySelector<HTMLImageElement>(`#qr-img-${projectId}`);
    if (img) img.src = qrSrc;

    const link = el.querySelector<HTMLAnchorElement>(`#qr-link-${projectId}`);
    if (link) link.href = qrSrc;

    const btn = el.querySelector<HTMLButtonElement>(`#copy-btn-${projectId}`);
    btn?.addEventListener('click', () => {
        navigator.clipboard.writeText(surveyUrl).then(() => {
            if (btn) btn.textContent = 'Gekopieerd!';
            setTimeout(() => { if (btn) btn.textContent = 'Kopieer'; }, 2000);
        });
    });
}
