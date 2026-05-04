const MAX_SIZE_BYTES = 5 * 1024 * 1024;
const ALLOWED_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp', 'image/gif']);

function getCsrfToken(): string {
    return document.querySelector<HTMLMetaElement>('meta[name="__RequestVerificationToken"]')?.content ?? '';
}

function compressImage(file: File, maxWidth: number, quality: number): Promise<Blob> {
    return new Promise((resolve) => {
        const img = new Image();
        const url = URL.createObjectURL(file);
        img.onload = () => {
            URL.revokeObjectURL(url);
            const scale = Math.min(1, maxWidth / img.width);
            const canvas = document.createElement('canvas');
            canvas.width = Math.round(img.width * scale);
            canvas.height = Math.round(img.height * scale);
            canvas.getContext('2d')!.drawImage(img, 0, 0, canvas.width, canvas.height);
            canvas.toBlob((blob) => resolve(blob!), 'image/jpeg', quality);
        };
        img.src = url;
    });
}

export function wireIdeaImageField(form: HTMLFormElement, projectId: number): void {
    const container = form.querySelector<HTMLElement>('[data-image-upload]');
    if (!container) return;

    const fileInput = container.querySelector<HTMLInputElement>('input[type="file"]')!;
    const browseBtn = container.querySelector<HTMLButtonElement>('[data-action="browse-image"]')!;
    const removeBtn = container.querySelector<HTMLButtonElement>('[data-action="remove-image"]')!;
    const filenameEl = container.querySelector<HTMLElement>('[data-image-filename]')!;
    const previewContainer = container.querySelector<HTMLElement>('[data-image-preview]')!;
    const previewImg = previewContainer.querySelector<HTMLImageElement>('img')!;
    const errorEl = container.querySelector<HTMLElement>('[data-image-error]')!;
    const urlInput = form.querySelector<HTMLInputElement>('input[name="imageUrl"]')!;
    const tokenInput = form.querySelector<HTMLInputElement>('input[name="imageModerationToken"]')!;

    browseBtn.addEventListener('click', () => fileInput.click());

    fileInput.addEventListener('change', () => {
        const file = fileInput.files?.[0];
        clearError();
        clearImage();

        if (!file) return;

        if (!ALLOWED_TYPES.has(file.type)) {
            showError('Alleen afbeeldingen (jpeg, png, webp, gif) zijn toegestaan.');
            fileInput.value = '';
            return;
        }
        if (file.size > MAX_SIZE_BYTES) {
            showError('De afbeelding mag maximaal 5 MB zijn.');
            fileInput.value = '';
            return;
        }

        filenameEl.textContent = file.name;
        previewImg.src = URL.createObjectURL(file);
        previewContainer.classList.remove('hidden');
        removeBtn.classList.remove('hidden');
        // Clear previously uploaded URL/token — user must upload again on next submit
        urlInput.value = '';
        tokenInput.value = '';
    });

    removeBtn.addEventListener('click', () => {
        clearImage();
        fileInput.value = '';
    });

    function clearImage(): void {
        if (previewImg.src.startsWith('blob:')) URL.revokeObjectURL(previewImg.src);
        previewImg.src = '';
        previewContainer.classList.add('hidden');
        removeBtn.classList.add('hidden');
        filenameEl.textContent = '';
        urlInput.value = '';
        tokenInput.value = '';
    }

    function showError(msg: string): void {
        errorEl.textContent = msg;
        errorEl.classList.remove('hidden');
    }

    function clearError(): void {
        errorEl.textContent = '';
        errorEl.classList.add('hidden');
    }
}

export type ImageUploadOutcome =
    | { status: 'ok' | 'skipped' }
    | { status: 'rejected'; reason: string }
    | { status: 'error'; reason: string };

export function showImageRejectionPopup(reason: string): Promise<'submitFlagged' | 'cancel'> {
    return new Promise(resolve => {
        const overlay = document.getElementById('image-rejection-overlay')!;
        const reasonEl = document.getElementById('image-rejection-reason')!;
        const btnSubmit = document.getElementById('image-rejection-btn-submit')!;
        const btnCancel = document.getElementById('image-rejection-btn-cancel')!;
        const btnX = document.getElementById('image-rejection-btn-x');

        reasonEl.textContent = reason;
        overlay.classList.remove('hidden');
        overlay.classList.add('flex');

        function close() {
            overlay.classList.add('hidden');
            overlay.classList.remove('flex');
            btnSubmit.removeEventListener('click', onSubmit);
            btnCancel.removeEventListener('click', onCancel);
            btnX?.removeEventListener('click', onCancel);
        }

        function onSubmit() { close(); resolve('submitFlagged'); }
        function onCancel() { close(); resolve('cancel'); }

        btnSubmit.addEventListener('click', onSubmit, { once: true });
        btnCancel.addEventListener('click', onCancel, { once: true });
        btnX?.addEventListener('click', onCancel, { once: true });
    });
}

export function clearIdeaImageField(form: HTMLFormElement): void {
    const container = form.querySelector<HTMLElement>('[data-image-upload]');
    if (!container) return;
    const fileInput = container.querySelector<HTMLInputElement>('input[type="file"]')!;
    const removeBtn = container.querySelector<HTMLButtonElement>('[data-action="remove-image"]')!;
    const filenameEl = container.querySelector<HTMLElement>('[data-image-filename]')!;
    const previewContainer = container.querySelector<HTMLElement>('[data-image-preview]')!;
    const previewImg = previewContainer.querySelector<HTMLImageElement>('img')!;
    const urlInput = form.querySelector<HTMLInputElement>('input[name="imageUrl"]')!;
    const tokenInput = form.querySelector<HTMLInputElement>('input[name="imageModerationToken"]')!;

    if (previewImg.src.startsWith('blob:')) URL.revokeObjectURL(previewImg.src);
    previewImg.src = '';
    previewContainer.classList.add('hidden');
    removeBtn.classList.add('hidden');
    filenameEl.textContent = '';
    fileInput.value = '';
    urlInput.value = '';
    tokenInput.value = '';
}

export async function uploadSelectedImage(form: HTMLFormElement, projectId: number): Promise<ImageUploadOutcome> {
    const container = form.querySelector<HTMLElement>('[data-image-upload]');
    if (!container) return { status: 'skipped' };

    const fileInput = container.querySelector<HTMLInputElement>('input[type="file"]')!;
    const urlInput = form.querySelector<HTMLInputElement>('input[name="imageUrl"]')!;
    const tokenInput = form.querySelector<HTMLInputElement>('input[name="imageModerationToken"]')!;
    const errorEl = container.querySelector<HTMLElement>('[data-image-error]')!;

    const file = fileInput.files?.[0];

    // If no file or we already have a committed URL+token, skip upload
    if (!file) return { status: 'skipped' };
    if (urlInput.value && tokenInput.value) return { status: 'ok' };

    const compressed = await compressImage(file, 1200, 0.8);
    const compressedFile = new File([compressed], 'image.jpg', { type: 'image/jpeg' });

    const formData = new FormData();
    formData.append('file', compressedFile, compressedFile.name);

    try {
        const res = await fetch(`/${projectId}/api/IdeaImageApi/upload`, {
            method: 'POST',
            headers: { RequestVerificationToken: getCsrfToken() },
            body: formData,
        });

        if (!res.ok) {
            const data = await res.json().catch(() => ({})) as { reason?: string };
            const reason = data.reason ?? 'Het uploaden van de afbeelding is mislukt.';
            errorEl.textContent = reason;
            errorEl.classList.remove('hidden');
            return { status: 'error', reason };
        }

        const data = await res.json() as { url: string; imageModerationToken: string; isAcceptable: boolean; reason?: string };
        urlInput.value = data.url;
        tokenInput.value = data.imageModerationToken;

        if (data.isAcceptable) {
            return { status: 'ok' };
        }
        // AI rejected — hidden inputs carry the Flagged token; caller decides whether to proceed
        return { status: 'rejected', reason: data.reason ?? 'De afbeelding is niet toegestaan.' };
    } catch {
        const reason = 'Het uploaden van de afbeelding is mislukt.';
        errorEl.textContent = reason;
        errorEl.classList.remove('hidden');
        return { status: 'error', reason };
    }
}
