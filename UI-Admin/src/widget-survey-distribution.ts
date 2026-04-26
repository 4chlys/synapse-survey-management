export function initSurveyDistributionWidget(el: HTMLElement): void {
    const surveyUrl = el.dataset.surveyUrl ?? '';
    const qrSrc = `https://chart.googleapis.com/chart?cht=qr&chs=200x200&chl=${encodeURIComponent(surveyUrl)}`;

    el.innerHTML = `
        <p class="text-xs text-gray-400 mb-2">Survey-link</p>
        <div class="flex items-center gap-2 mb-3">
            <span class="text-gray-200 text-xs break-all flex-1">${surveyUrl}</span>
            <button id="copy-btn-${el.dataset.projectId}"
                class="bg-gray-700 hover:bg-gray-600 text-white text-xs px-2 py-1 rounded whitespace-nowrap transition">
                Kopieer
            </button>
        </div>
        <img src="${qrSrc}" alt="QR code" class="rounded mb-2" width="160" height="160" />
        <a href="${qrSrc}" download="qr-code.png" class="text-xs text-gray-400 hover:text-white underline">
            QR downloaden
        </a>
    `;

    const btn = el.querySelector<HTMLButtonElement>(`#copy-btn-${el.dataset.projectId}`);
    btn?.addEventListener('click', () => {
        navigator.clipboard.writeText(surveyUrl).then(() => {
            if (btn) btn.textContent = 'Gekopieerd!';
            setTimeout(() => { if (btn) btn.textContent = 'Kopieer'; }, 2000);
        });
    });
}
