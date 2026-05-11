import {t} from '../lib/i18n';

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

export function wireImageDescribeButton(form: HTMLFormElement, projectId: number): void {
    const container = form.querySelector<HTMLElement>('[data-image-upload]');
    if (!container) return;

    const fileInput = container.querySelector<HTMLInputElement>('input[type="file"]')!;
    const describeBtn = container.querySelector<HTMLButtonElement>('[data-action="describe-image"]');
    const textarea = form.querySelector<HTMLTextAreaElement>('textarea[name="description"]');

    if (!describeBtn || !textarea) return;

    const pageContainer = document.getElementById('ideation-page') ??
        document.querySelector<HTMLElement>('[data-category-id]');
    const categoryName = pageContainer?.dataset.categoryName ?? '';
    const categoryDescription = pageContainer?.dataset.categoryDescription ?? '';
    const languageSelect = form.querySelector<HTMLSelectElement>('select[name="languageCode"]');

    // Show/hide describe button based on file selection
    fileInput.addEventListener('change', () => {
        const hasFile = (fileInput.files?.length ?? 0) > 0;
        describeBtn.classList.toggle('hidden', !hasFile);
    });

    // Also hide when image is removed
    const removeBtn = container.querySelector<HTMLButtonElement>('[data-action="remove-image"]');
    removeBtn?.addEventListener('click', () => {
        describeBtn.classList.add('hidden');
    });

    describeBtn.addEventListener('click', async () => {
        const file = fileInput.files?.[0];
        if (!file) return;

        const originalLabel = describeBtn.textContent;
        describeBtn.disabled = true;
        describeBtn.textContent = t('imageDescribeLoading');

        try {
            const compressed = await compressImage(file, 1200, 0.8);
            const compressedFile = new File([compressed], 'image.jpg', {type: 'image/jpeg'});

            const formData = new FormData();
            formData.append('file', compressedFile, compressedFile.name);

            const params = new URLSearchParams({categoryName, categoryDescription});
            const selectedLanguage = languageSelect?.value ?? '';
            if (selectedLanguage) params.set('language', selectedLanguage);
            const res = await fetch(`/${projectId}/api/IdeaImageApi/describe?${params}`, {
                method: 'POST',
                headers: {RequestVerificationToken: getCsrfToken()},
                body: formData,
            });

            if (!res.ok) {
                const data = await res.json().catch(() => ({})) as { reason?: string };
                alert(data.reason ?? t('imageDescribeFailed'));
                return;
            }

            const data = await res.json() as { description: string };
            textarea.value = data.description;
            textarea.focus();
        } catch {
            alert(t('imageDescribeFailed'));
        } finally {
            describeBtn.disabled = false;
            describeBtn.textContent = originalLabel;
        }
    });
}
