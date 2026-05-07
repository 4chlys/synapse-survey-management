const MAX_BYTES: Record<number, number> = {
    1: 5 * 1024 * 1024,   // Image
    2: 25 * 1024 * 1024,  // Video
    3: 20 * 1024 * 1024,  // Audio
};

const YOUTUBE_VIMEO_PATTERN = /^https?:\/\/(www\.youtube\.com|youtu\.be|vimeo\.com|player\.vimeo\.com)\//i;
const VIDEO_DIRECT_PATTERN  = /\.(mp4|webm)$/i;
const AUDIO_PATTERN         = /\.(mp3|m4a|wav|ogg)$/i;

const EMBED_PLACEHOLDERS: Record<number, string> = {
    1: 'https://example.com/afbeelding.jpg',
    2: 'https://www.youtube.com/watch?v=... of https://example.com/video.mp4',
    3: 'https://example.com/audio.mp3',
};

const EMBED_LABELS: Record<number, string> = {
    1: 'Afbeelding URL',
    2: 'Video URL (YouTube, Vimeo of directe .mp4/.webm link)',
    3: 'Audio URL (.mp3, .m4a, .wav of .ogg)',
};

function validateEmbedUrl(url: string, type: number): string | null {
    if (!url.startsWith('https://') && !url.startsWith('http://'))
        return 'URL moet beginnen met https://';
    if (type === 1) return null; // any URL accepted for images
    if (type === 2) {
        if (YOUTUBE_VIMEO_PATTERN.test(url) || VIDEO_DIRECT_PATTERN.test(url)) return null;
        return 'Voer een YouTube/Vimeo URL in of een directe .mp4/.webm link.';
    }
    if (type === 3) {
        if (AUDIO_PATTERN.test(url)) return null;
        return 'URL moet eindigen op .mp3, .m4a, .wav of .ogg.';
    }
    return null;
}

function getAntiForgeryToken(): string {
    return (document.querySelector('input[name="__RequestVerificationToken"]') as HTMLInputElement | null)?.value ?? '';
}

export function wireQuestionMediaPanel(
    prefix: string,
    projectId: number,
    onUpload?: (url: string) => void
): void {
    const typeSelect    = document.getElementById(`${prefix}MediaType`) as HTMLSelectElement | null;
    const embedToggle   = document.getElementById(`${prefix}media-source-toggle`) as HTMLElement | null;
    const embedCb       = document.getElementById(`${prefix}MediaIsEmbed`) as HTMLInputElement | null;
    const uploadBlock   = document.getElementById(`${prefix}media-upload-block`) as HTMLElement | null;
    const embedBlock    = document.getElementById(`${prefix}media-embed-block`) as HTMLElement | null;
    const embedLabel    = document.getElementById(`${prefix}media-embed-label`) as HTMLElement | null;
    const fileInput     = document.getElementById(`${prefix}MediaFile`) as HTMLInputElement | null;
    const urlHidden     = document.getElementById(`${prefix}MediaUrl`) as HTMLInputElement | null;
    const embedUrlInput = document.getElementById(`${prefix}MediaEmbedUrl`) as HTMLInputElement | null;
    const uploadError   = document.getElementById(`${prefix}media-upload-error`) as HTMLElement | null;
    const embedError    = document.getElementById(`${prefix}media-embed-error`) as HTMLElement | null;
    const preview       = document.getElementById(`${prefix}media-preview`) as HTMLElement | null;
    const textArea      = document.getElementById(`${prefix}MediaText`) as HTMLTextAreaElement | null;
    const counter       = document.getElementById(`${prefix}media-text-counter`) as HTMLElement | null;

    function currentType(): number {
        return parseInt(typeSelect?.value ?? '0', 10);
    }

    function updateVisibility(): void {
        const type = currentType();
        const isEmbed = embedCb?.checked ?? false;

        if (!embedToggle || !uploadBlock || !embedBlock) return;

        if (type === 0) {
            embedToggle.classList.add('hidden');
            uploadBlock.classList.add('hidden');
            uploadBlock.style.display = '';
            embedBlock.classList.add('hidden');
            return;
        }

        // All media types support both upload and URL
        embedToggle.classList.remove('hidden');

        // Update placeholder and label per type
        if (embedUrlInput) embedUrlInput.placeholder = EMBED_PLACEHOLDERS[type] ?? 'https://...';
        if (embedLabel) embedLabel.textContent = EMBED_LABELS[type] ?? 'URL';

        if (isEmbed) {
            uploadBlock.classList.add('hidden');
            uploadBlock.style.display = '';
            embedBlock.classList.remove('hidden');
        } else {
            uploadBlock.classList.remove('hidden');
            uploadBlock.style.display = 'flex';
            embedBlock.classList.add('hidden');
        }
    }

    typeSelect?.addEventListener('change', () => {
        // Clear state on type change
        if (urlHidden) urlHidden.value = '';
        if (preview) preview.innerHTML = '';
        if (uploadError) { uploadError.textContent = ''; uploadError.classList.add('hidden'); }
        if (embedError)  { embedError.textContent  = ''; embedError.classList.add('hidden'); }
        if (embedUrlInput) embedUrlInput.value = '';
        if (fileInput) fileInput.value = '';
        updateVisibility();
    });

    embedCb?.addEventListener('change', () => {
        if (urlHidden) urlHidden.value = '';
        if (preview) preview.innerHTML = '';
        updateVisibility();
    });

    fileInput?.addEventListener('change', async () => {
        const file = fileInput.files?.[0];
        if (!file) return;

        const type = currentType();
        const max  = MAX_BYTES[type] ?? 0;
        if (file.size > max) {
            if (uploadError) {
                uploadError.textContent = `Bestand is te groot (max ${max / (1024 * 1024)} MB).`;
                uploadError.classList.remove('hidden');
            }
            fileInput.value = '';
            return;
        }
        if (uploadError) uploadError.classList.add('hidden');

        const fd = new FormData();
        fd.append('file', file);

        try {
            const res = await fetch(
                `/api/admin/projects/${projectId}/questions/media/upload?mediaType=${type}`,
                {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() },
                    body: fd,
                }
            );
            if (!res.ok) {
                const msg = await res.text();
                if (uploadError) { uploadError.textContent = msg || 'Upload mislukt.'; uploadError.classList.remove('hidden'); }
                return;
            }
            const data = await res.json() as { url: string };
            if (urlHidden) urlHidden.value = data.url;
            if (preview) renderPreview(type, data.url, preview, () => {
                if (urlHidden) urlHidden.value = '';
                if (fileInput) fileInput.value = '';
                preview!.innerHTML = '';
            });
            onUpload?.(data.url);
        } catch {
            if (uploadError) { uploadError.textContent = 'Upload mislukt.'; uploadError.classList.remove('hidden'); }
        }
    });

    embedUrlInput?.addEventListener('input', () => {
        const url = embedUrlInput.value.trim();
        const type = currentType();
        if (!url) {
            if (urlHidden) urlHidden.value = '';
            if (preview) preview.innerHTML = '';
            if (embedError) embedError.classList.add('hidden');
            return;
        }

        const error = validateEmbedUrl(url, type);
        if (error) {
            if (embedError) { embedError.textContent = error; embedError.classList.remove('hidden'); }
            if (urlHidden) urlHidden.value = '';
            if (preview) preview.innerHTML = '';
        } else {
            if (embedError) embedError.classList.add('hidden');
            if (urlHidden) urlHidden.value = url;
            if (preview) renderPreview(type, url, preview, () => {
                if (urlHidden) urlHidden.value = '';
                if (embedUrlInput) embedUrlInput.value = '';
                preview!.innerHTML = '';
            });
        }
    });

    textArea?.addEventListener('input', () => {
        if (counter) counter.textContent = `${textArea.value.length} / 2000`;
    });

    updateVisibility();
}

export function hydrateMediaPanel(prefix: string, mediaUrl: string, mediaType: number, mediaIsEmbed: boolean, mediaText: string): void {
    const typeSelect  = document.getElementById(`${prefix}MediaType`) as HTMLSelectElement | null;
    const embedCb     = document.getElementById(`${prefix}MediaIsEmbed`) as HTMLInputElement | null;
    const urlHidden   = document.getElementById(`${prefix}MediaUrl`) as HTMLInputElement | null;
    const fileInput   = document.getElementById(`${prefix}MediaFile`) as HTMLInputElement | null;
    const embedInput  = document.getElementById(`${prefix}MediaEmbedUrl`) as HTMLInputElement | null;
    const embedToggle = document.getElementById(`${prefix}media-source-toggle`) as HTMLElement | null;
    const uploadBlock = document.getElementById(`${prefix}media-upload-block`) as HTMLElement | null;
    const embedBlock  = document.getElementById(`${prefix}media-embed-block`) as HTMLElement | null;
    const embedLabel  = document.getElementById(`${prefix}media-embed-label`) as HTMLElement | null;
    const textArea    = document.getElementById(`${prefix}MediaText`) as HTMLTextAreaElement | null;
    const counter     = document.getElementById(`${prefix}media-text-counter`) as HTMLElement | null;
    const preview     = document.getElementById(`${prefix}media-preview`) as HTMLElement | null;

    // Set form values
    if (typeSelect) typeSelect.value = mediaType ? String(mediaType) : '';
    if (embedCb)    embedCb.checked = mediaIsEmbed;
    if (urlHidden)  urlHidden.value = mediaUrl ?? '';
    if (textArea)   textArea.value  = mediaText ?? '';
    if (counter && textArea) counter.textContent = `${textArea.value.length} / 2000`;
    if (embedInput) embedInput.value = (mediaIsEmbed && mediaUrl) ? mediaUrl : '';

    // Apply visibility directly — do NOT dispatch events; the change handlers clear state
    if (embedToggle && uploadBlock && embedBlock) {
        if (!mediaType) {
            embedToggle.classList.add('hidden');
            uploadBlock.classList.add('hidden');
            uploadBlock.style.display = '';
            embedBlock.classList.add('hidden');
        } else {
            embedToggle.classList.remove('hidden');
            if (embedInput) embedInput.placeholder = EMBED_PLACEHOLDERS[mediaType] ?? 'https://...';
            if (embedLabel) embedLabel.textContent = EMBED_LABELS[mediaType] ?? 'URL';
            if (mediaIsEmbed) {
                uploadBlock.classList.add('hidden');
                uploadBlock.style.display = '';
                embedBlock.classList.remove('hidden');
            } else {
                uploadBlock.classList.remove('hidden');
                uploadBlock.style.display = 'flex';
                embedBlock.classList.add('hidden');
            }
        }
    }

    // Render preview (works for both uploaded and URL media)
    if (mediaUrl && preview) renderPreview(mediaType, mediaUrl, preview, () => {
        if (urlHidden)  urlHidden.value  = '';
        if (fileInput)  fileInput.value  = '';
        if (embedInput) embedInput.value = '';
        if (preview)    preview.innerHTML = '';
    });
}

function renderPreview(type: number, url: string, container: HTMLElement, onClear?: () => void): void {
    container.innerHTML = '';

    const wrapper = document.createElement('div');
    wrapper.className = type === 1 ? 'relative inline-block' : 'relative';

    if (type === 1) {
        const img = document.createElement('img');
        img.src = url;
        img.className = 'rounded-lg max-h-40 object-contain';
        wrapper.appendChild(img);
    } else if (type === 2) {
        if (YOUTUBE_VIMEO_PATTERN.test(url)) {
            const p = document.createElement('p');
            p.className = 'text-xs text-base-content/60 italic pr-8';
            p.textContent = '▶ Video URL: ' + url;
            wrapper.appendChild(p);
        } else {
            const video = document.createElement('video');
            video.src = url;
            video.controls = true;
            video.preload = 'metadata';
            video.className = 'rounded-lg max-h-40 w-full';
            wrapper.appendChild(video);
        }
    } else if (type === 3) {
        const audio = document.createElement('audio');
        audio.src = url;
        audio.controls = true;
        audio.preload = 'metadata';
        audio.className = 'w-full';
        wrapper.appendChild(audio);
    }

    if (onClear) {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.title = 'Verwijder media';
        btn.className = 'absolute top-1 right-1 btn btn-xs btn-circle btn-neutral opacity-70 hover:opacity-100 z-10';
        btn.textContent = '✕';
        btn.addEventListener('click', onClear);
        wrapper.appendChild(btn);
    }

    container.appendChild(wrapper);
}
