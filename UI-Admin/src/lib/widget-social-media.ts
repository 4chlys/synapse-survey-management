type Platform = 'LinkedIn' | 'Facebook' | 'Instagram';

interface GenerateResponse {
    copy: string;
    suggestedHashtags: string[];
    characterCount: number;
    platform: Platform;
}

const PLATFORM_ICONS: Record<Platform, string> = {
    LinkedIn: `<svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor"><path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433a2.062 2.062 0 01-2.063-2.065 2.064 2.064 0 112.063 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/></svg>`,
    Facebook: `<svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor"><path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/></svg>`,
    Instagram: `<svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zM12 0C8.741 0 8.333.014 7.053.072 2.695.272.273 2.69.073 7.052.014 8.333 0 8.741 0 12c0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98C8.333 23.986 8.741 24 12 24c3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98C15.668.014 15.259 0 12 0zm0 5.838a6.162 6.162 0 100 12.324 6.162 6.162 0 000-12.324zM12 16a4 4 0 110-8 4 4 0 010 8zm6.406-11.845a1.44 1.44 0 100 2.881 1.44 1.44 0 000-2.881z"/></svg>`,
};

const PLATFORM_COLORS: Record<Platform, string> = {
    LinkedIn: '#0A66C2',
    Facebook: '#1877F2',
    Instagram: '#E4405F',
};

function buildShareUrl(platform: Platform, surveyUrl: string): string {
    const enc = encodeURIComponent(surveyUrl);
    switch (platform) {
        case 'LinkedIn':  return `https://www.linkedin.com/sharing/share-offsite/?url=${enc}`;
        case 'Facebook':  return `https://www.facebook.com/sharer/sharer.php?u=${enc}`;
        case 'Instagram': return 'https://www.instagram.com/';
    }
}

export function initSocialMediaWidget(el: HTMLElement): void {
    const projectId = el.dataset.projectId ?? '';
    const surveyUrl = el.dataset.surveyUrl ?? '';

    const i18n = {
        title:         el.dataset.i18nTitle           ?? '',
        descPlaceholder: el.dataset.i18nDescPlaceholder ?? '',
        aiImage:       el.dataset.i18nAiImage         ?? '',
        generate:      el.dataset.i18nGenerate        ?? '',
        generating:    el.dataset.i18nGenerating      ?? '',
        copyText:      el.dataset.i18nCopyText        ?? '',
        copied:        el.dataset.i18nCopied          ?? '',
        openPlatform:  el.dataset.i18nOpenPlatform    ?? '',
        openInFmt:     el.dataset.i18nOpenIn          ?? '{0}',
        charCountFmt:  el.dataset.i18nCharCount       ?? '{0}',
    };
    const platformNotes: Record<Platform, string> = {
        LinkedIn:  el.dataset.i18nNoteLinkedin  ?? '',
        Facebook:  el.dataset.i18nNoteFacebook  ?? '',
        Instagram: el.dataset.i18nNoteInstagram ?? '',
    };

    el.innerHTML = `
        <!-- Step 1: Input -->
        <div data-role="step-input">
            <div class="flex items-center gap-2 mb-3">
                <div class="flex items-center gap-1.5" data-role="platform-icons">
                    <button type="button" data-platform="LinkedIn" class="social-platform-btn social-platform-btn--active" style="--platform-color: ${PLATFORM_COLORS.LinkedIn}">
                        ${PLATFORM_ICONS.LinkedIn}
                    </button>
                    <button type="button" data-platform="Facebook" class="social-platform-btn" style="--platform-color: ${PLATFORM_COLORS.Facebook}">
                        ${PLATFORM_ICONS.Facebook}
                    </button>
                    <button type="button" data-platform="Instagram" class="social-platform-btn" style="--platform-color: ${PLATFORM_COLORS.Instagram}">
                        ${PLATFORM_ICONS.Instagram}
                    </button>
                </div>
                <span class="text-xs font-semibold text-base-content/50 uppercase tracking-wider flex-1">${i18n.title}</span>
            </div>

            <input type="hidden" data-role="platform" value="LinkedIn" />

            <textarea class="textarea textarea-bordered textarea-sm w-full" rows="2"
                      placeholder="${i18n.descPlaceholder}"
                      data-role="user-desc"></textarea>

            <p class="text-[10px] text-base-content/40 italic mt-1.5 min-h-[1rem]" data-role="note"></p>

            <button class="btn btn-primary btn-sm w-full mt-2 gap-1.5" data-role="generate">
                <svg width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M12 2L2 7l10 5 10-5-10-5z"/><path d="M2 17l10 5 10-5"/><path d="M2 12l10 5 10-5"/></svg>
                ${i18n.generate}
            </button>
        </div>

        <!-- Step 2: Result (hidden until generated) -->
        <div class="hidden" data-role="step-result">
            <div class="flex items-center justify-between mb-3">
                <div class="flex items-center gap-2">
                    <span data-role="result-platform-icon" class="text-base-content/60"></span>
                    <span class="text-xs font-semibold text-base-content/50 uppercase tracking-wider" data-role="result-platform-name"></span>
                </div>
                <button class="btn btn-ghost btn-xs gap-1" data-role="regenerate">
                    <svg width="12" height="12" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 11-2.12-9.36L23 10"/></svg>
                    ${i18n.generate}
                </button>
            </div>

            <textarea class="textarea textarea-bordered textarea-sm w-full font-mono text-xs" rows="6"
                      data-role="output"></textarea>

            <p class="text-[10px] text-base-content/30 tabular-nums mt-1 text-right" data-role="char-count"></p>

            <!-- Image attachment (post-generation) -->
            <div class="flex items-center gap-2 mt-3 pt-3 border-t border-base-300">
                <input type="file" accept="image/*" class="file-input file-input-xs file-input-bordered flex-1" data-role="image-file" />
                <button class="btn btn-ghost btn-xs gap-1" data-role="ai-image">
                    <svg width="12" height="12" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/></svg>
                    ${i18n.aiImage}
                </button>
            </div>
            <img data-role="image-preview" class="hidden rounded-lg mt-2 max-h-28 w-full object-cover" alt="" />

            <!-- Actions -->
            <div class="flex gap-2 mt-3">
                <button class="btn btn-outline btn-sm flex-1 gap-1.5" data-role="copy">
                    <svg width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><rect x="9" y="9" width="13" height="13" rx="2"/><path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1"/></svg>
                    ${i18n.copyText}
                </button>
                <a class="btn btn-primary btn-sm flex-1 gap-1.5" target="_blank" rel="noopener" data-role="share">
                    <svg width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M18 13v6a2 2 0 01-2 2H5a2 2 0 01-2-2V8a2 2 0 012-2h6"/><polyline points="15 3 21 3 21 9"/><line x1="10" y1="14" x2="21" y2="3"/></svg>
                    ${i18n.openPlatform}
                </a>
            </div>
        </div>
    `;

    const $ = <T extends HTMLElement>(role: string) =>
        el.querySelector<T>(`[data-role="${role}"]`)!;

    const stepInput   = $<HTMLDivElement>('step-input');
    const stepResult  = $<HTMLDivElement>('step-result');
    const platformInput = $<HTMLInputElement>('platform');
    const platformBtns = el.querySelectorAll<HTMLButtonElement>('[data-platform]');
    const userDesc    = $<HTMLTextAreaElement>('user-desc');
    const noteEl      = $<HTMLParagraphElement>('note');
    const generateBtn = $<HTMLButtonElement>('generate');
    const regenerateBtn = $<HTMLButtonElement>('regenerate');
    const outputEl    = $<HTMLTextAreaElement>('output');
    const charCount   = $<HTMLParagraphElement>('char-count');
    const resultPlatformIcon = $<HTMLSpanElement>('result-platform-icon');
    const resultPlatformName = $<HTMLSpanElement>('result-platform-name');
    const fileInput   = $<HTMLInputElement>('image-file');
    const aiImageBtn  = $<HTMLButtonElement>('ai-image');
    const imagePrev   = $<HTMLImageElement>('image-preview');
    const copyBtn     = $<HTMLButtonElement>('copy');
    const shareLink   = $<HTMLAnchorElement>('share');

    function setActivePlatform(p: Platform): void {
        platformInput.value = p;
        platformBtns.forEach(btn => {
            btn.classList.toggle('social-platform-btn--active', btn.dataset.platform === p);
        });
        noteEl.textContent = platformNotes[p];
    }

    platformBtns.forEach(btn => {
        btn.addEventListener('click', () => setActivePlatform(btn.dataset.platform as Platform));
    });
    setActivePlatform('LinkedIn');

    async function generate(): Promise<void> {
        generateBtn.disabled = true;
        regenerateBtn.disabled = true;
        const activeBtn = stepResult.classList.contains('hidden') ? generateBtn : regenerateBtn;
        const origHtml = activeBtn.innerHTML;
        activeBtn.textContent = i18n.generating;

        try {
            const platform = platformInput.value as Platform;
            const res = await fetch(`/api/projects/${projectId}/social/generate`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ platform, userDescription: userDesc.value }),
            });
            if (!res.ok) return;
            const data = await res.json() as GenerateResponse;
            const fullCopy = data.suggestedHashtags.length
                ? `${data.copy}\n\n${data.suggestedHashtags.join(' ')}`
                : data.copy;

            outputEl.value = fullCopy;
            charCount.textContent = i18n.charCountFmt.replace('{0}', String(fullCopy.length));
            resultPlatformIcon.innerHTML = PLATFORM_ICONS[platform];
            resultPlatformIcon.style.color = PLATFORM_COLORS[platform];
            resultPlatformName.textContent = platform;
            shareLink.href = buildShareUrl(data.platform, surveyUrl);
            shareLink.textContent = i18n.openInFmt.replace('{0}', data.platform);

            stepInput.classList.add('hidden');
            stepResult.classList.remove('hidden');
        } finally {
            generateBtn.disabled = false;
            regenerateBtn.disabled = false;
            activeBtn.innerHTML = origHtml;
        }
    }

    generateBtn.addEventListener('click', generate);
    regenerateBtn.addEventListener('click', generate);

    outputEl.addEventListener('input', () => {
        charCount.textContent = i18n.charCountFmt.replace('{0}', String(outputEl.value.length));
    });

    fileInput.addEventListener('change', async () => {
        const file = fileInput.files?.[0];
        if (!file) return;
        const fd = new FormData();
        fd.append('file', file);
        const res = await fetch(`/api/projects/${projectId}/social/upload-image`,
            { method: 'POST', body: fd });
        if (!res.ok) return;
        const { url } = await res.json() as { url: string };
        imagePrev.src = url;
        imagePrev.classList.remove('hidden');
    });

    aiImageBtn.addEventListener('click', async () => {
        aiImageBtn.disabled = true;
        const origHtml = aiImageBtn.innerHTML;
        aiImageBtn.textContent = '…';
        try {
            const res = await fetch(`/api/projects/${projectId}/social/image`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ postCopy: outputEl.value }),
            });
            if (!res.ok) return;
            const { imageUrl } = await res.json() as { imageUrl: string };
            imagePrev.src = imageUrl;
            imagePrev.classList.remove('hidden');
        } finally {
            aiImageBtn.disabled = false;
            aiImageBtn.innerHTML = origHtml;
        }
    });

    copyBtn.addEventListener('click', () => {
        navigator.clipboard.writeText(outputEl.value).then(() => {
            const orig = copyBtn.innerHTML;
            copyBtn.innerHTML = `<svg width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M5 13l4 4L19 7"/></svg> ${i18n.copied}`;
            setTimeout(() => { copyBtn.innerHTML = orig; }, 2000);
        });
    });
}
