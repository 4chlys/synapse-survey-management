type Platform = 'LinkedIn' | 'Facebook' | 'Instagram';

interface GenerateResponse {
    copy: string;
    suggestedHashtags: string[];
    characterCount: number;
    platform: Platform;
}

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

    // Localized strings come from the view via data-i18n-* attributes; the {0} placeholder
    // in OpenIn/CharCount is substituted at runtime.
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
        <p class="text-xs font-semibold text-base-content/60 mb-3">${i18n.title}</p>
        <div class="flex flex-col gap-2">
          <select class="select select-bordered select-xs" data-role="platform">
            <option value="LinkedIn">LinkedIn</option>
            <option value="Facebook">Facebook</option>
            <option value="Instagram">Instagram</option>
          </select>
          <textarea class="textarea textarea-bordered textarea-xs" rows="2"
                    placeholder="${i18n.descPlaceholder}"
                    data-role="user-desc"></textarea>

          <div class="flex flex-col gap-1">
            <div class="flex gap-1">
              <input type="file" accept="image/*" class="file-input file-input-xs flex-1" data-role="image-file" />
              <button class="btn btn-ghost btn-xs whitespace-nowrap" data-role="ai-image">${i18n.aiImage}</button>
            </div>
            <img data-role="image-preview" class="hidden rounded max-h-20 w-full object-cover" alt="" />
          </div>

          <p class="text-[10px] text-base-content/40 italic" data-role="note"></p>

          <button class="btn btn-primary btn-xs" data-role="generate">${i18n.generate}</button>

          <textarea class="textarea textarea-bordered textarea-xs hidden" rows="5"
                    data-role="output"></textarea>
          <p class="text-[10px] text-base-content/40 text-right hidden" data-role="char-count"></p>

          <div class="flex gap-1 hidden" data-role="actions">
            <button class="btn btn-outline btn-xs flex-1" data-role="copy">${i18n.copyText}</button>
            <a class="btn btn-xs flex-1" target="_blank" rel="noopener" data-role="share">${i18n.openPlatform}</a>
          </div>
        </div>
    `;

    const $  = <T extends HTMLElement>(role: string) =>
        el.querySelector<T>(`[data-role="${role}"]`)!;

    const platformSel = $<HTMLSelectElement>('platform');
    const userDesc    = $<HTMLTextAreaElement>('user-desc');
    const fileInput   = $<HTMLInputElement>('image-file');
    const aiImageBtn  = $<HTMLButtonElement>('ai-image');
    const imagePrev   = $<HTMLImageElement>('image-preview');
    const noteEl      = $<HTMLParagraphElement>('note');
    const generateBtn = $<HTMLButtonElement>('generate');
    const outputEl    = $<HTMLTextAreaElement>('output');
    const charCount   = $<HTMLParagraphElement>('char-count');
    const actionsEl   = $<HTMLDivElement>('actions');
    const copyBtn     = $<HTMLButtonElement>('copy');
    const shareLink   = $<HTMLAnchorElement>('share');

    const updateNote = () => { noteEl.textContent = platformNotes[platformSel.value as Platform]; };
    platformSel.addEventListener('change', updateNote);
    updateNote();

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
        aiImageBtn.textContent = '…';
        try {
            const res = await fetch(`/api/projects/${projectId}/social/image`, { method: 'POST' });
            if (!res.ok) return;
            const { imageUrl } = await res.json() as { imageUrl: string };
            imagePrev.src = imageUrl;
            imagePrev.classList.remove('hidden');
        } finally {
            aiImageBtn.disabled = false;
            aiImageBtn.textContent = i18n.aiImage;
        }
    });

    generateBtn.addEventListener('click', async () => {
        generateBtn.disabled = true;
        generateBtn.textContent = i18n.generating;
        try {
            const res = await fetch(`/api/projects/${projectId}/social/generate`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ platform: platformSel.value, userDescription: userDesc.value }),
            });
            if (!res.ok) return;
            const data = await res.json() as GenerateResponse;
            const fullCopy = data.suggestedHashtags.length
                ? `${data.copy}\n\n${data.suggestedHashtags.join(' ')}`
                : data.copy;
            outputEl.value = fullCopy;
            outputEl.classList.remove('hidden');
            charCount.textContent = i18n.charCountFmt.replace('{0}', String(fullCopy.length));
            charCount.classList.remove('hidden');
            actionsEl.classList.remove('hidden');
            shareLink.href = buildShareUrl(data.platform, surveyUrl);
            shareLink.textContent = i18n.openInFmt.replace('{0}', data.platform);
        } finally {
            generateBtn.disabled = false;
            generateBtn.textContent = i18n.generate;
        }
    });

    copyBtn.addEventListener('click', () => {
        navigator.clipboard.writeText(outputEl.value).then(() => {
            copyBtn.textContent = i18n.copied;
            setTimeout(() => { copyBtn.textContent = i18n.copyText; }, 2000);
        });
    });
}
