type Platform = 'LinkedIn' | 'Facebook' | 'Instagram';

interface GenerateResponse {
    copy: string;
    suggestedHashtags: string[];
    characterCount: number;
    platform: Platform;
}

const PLATFORM_NOTE: Record<Platform, string> = {
    LinkedIn:  'LinkedIn share-knop opent je project-link. Plak de tekst handmatig in de composer.',
    Facebook:  'Facebook share-knop opent met je project-link. Plak de tekst in de composer.',
    Instagram: 'Instagram heeft geen web share-URL. Kopieer de tekst en post handmatig via je telefoon.',
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

    el.innerHTML = `
        <p class="text-xs font-semibold text-base-content/60 mb-3">Social media post</p>
        <div class="flex flex-col gap-2">
          <select class="select select-bordered select-xs" data-role="platform">
            <option value="LinkedIn">LinkedIn</option>
            <option value="Facebook">Facebook</option>
            <option value="Instagram">Instagram</option>
          </select>
          <textarea class="textarea textarea-bordered textarea-xs" rows="2"
                    placeholder="Optioneel: extra context voor de AI"
                    data-role="user-desc"></textarea>

          <div class="flex flex-col gap-1">
            <div class="flex gap-1">
              <input type="file" accept="image/*" class="file-input file-input-xs flex-1" data-role="image-file" />
              <button class="btn btn-ghost btn-xs whitespace-nowrap" data-role="ai-image">AI-afbeelding</button>
            </div>
            <img data-role="image-preview" class="hidden rounded max-h-20 w-full object-cover" alt="" />
          </div>

          <p class="text-[10px] text-base-content/40 italic" data-role="note"></p>

          <button class="btn btn-primary btn-xs" data-role="generate">Genereer post</button>

          <textarea class="textarea textarea-bordered textarea-xs hidden" rows="5"
                    data-role="output"></textarea>
          <p class="text-[10px] text-base-content/40 text-right hidden" data-role="char-count"></p>

          <div class="flex gap-1 hidden" data-role="actions">
            <button class="btn btn-outline btn-xs flex-1" data-role="copy">Kopieer tekst</button>
            <a class="btn btn-xs flex-1" target="_blank" rel="noopener" data-role="share">Open platform</a>
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

    const updateNote = () => { noteEl.textContent = PLATFORM_NOTE[platformSel.value as Platform]; };
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
            aiImageBtn.textContent = 'AI-afbeelding';
        }
    });

    generateBtn.addEventListener('click', async () => {
        generateBtn.disabled = true;
        generateBtn.textContent = 'Genereren…';
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
            charCount.textContent = `${fullCopy.length} tekens`;
            charCount.classList.remove('hidden');
            actionsEl.classList.remove('hidden');
            shareLink.href = buildShareUrl(data.platform, surveyUrl);
            shareLink.textContent = `Open in ${data.platform}`;
        } finally {
            generateBtn.disabled = false;
            generateBtn.textContent = 'Genereer post';
        }
    });

    copyBtn.addEventListener('click', () => {
        navigator.clipboard.writeText(outputEl.value).then(() => {
            copyBtn.textContent = 'Gekopieerd!';
            setTimeout(() => { copyBtn.textContent = 'Kopieer tekst'; }, 2000);
        });
    });
}
