import Quill from 'quill';
import 'quill/dist/quill.snow.css';

import { toast, bindDirtyTracking } from '../lib/notify';

function compressImage(file: File, maxWidth: number, quality: number): Promise<Blob> {
    return new Promise((resolve) => {
        const img = new Image();
        const url = URL.createObjectURL(file);
        img.onload = () => {
            URL.revokeObjectURL(url);
            const scale = Math.min(1, maxWidth / img.width);
            const canvas = document.createElement('canvas');
            canvas.width = img.width * scale;
            canvas.height = img.height * scale;
            canvas.getContext('2d')!.drawImage(img, 0, 0, canvas.width, canvas.height);
            canvas.toBlob((blob) => resolve(blob!), 'image/jpeg', quality);
        };
        img.src = url;
    });
}

function getErrorSpan(fieldName: string): HTMLElement | null {
    return document.querySelector<HTMLElement>(`[data-valmsg-for="${fieldName}"]`);
}

function bindBlurValidation(
    input: HTMLInputElement,
    errorSpan: HTMLElement | null,
    getMessage: (input: HTMLInputElement) => string | null
) {
    let dirty = false;
    function validate() {
        const msg = getMessage(input);
        if (errorSpan) errorSpan.textContent = msg ?? '';
        dirty = msg !== null;
    }
    input.addEventListener('blur', validate);
    input.addEventListener('input', () => { if (dirty) validate(); });
}

document.addEventListener('DOMContentLoaded', () => {
    const minInput = document.getElementById('min-age-input') as HTMLInputElement | null;
    const maxInput = document.getElementById('max-age-input') as HTMLInputElement | null;
    const ageError = document.getElementById('age-range-error');

    function checkAgeRange(): boolean {
        const min = minInput?.value ? parseInt(minInput.value, 10) : null;
        const max = maxInput?.value ? parseInt(maxInput.value, 10) : null;
        return min !== null && max !== null && min > max;
    }

    const mainForm = document.getElementById('project-name-input')
        ? document.querySelector<HTMLFormElement>('form[method="post"]')
        : null;
    if (mainForm) {
        bindDirtyTracking(mainForm);
        if (new URLSearchParams(window.location.search).get('saved') === '1')
            toast('Opgeslagen', 'success');

        mainForm.addEventListener('submit', (e) => {
            if (checkAgeRange()) {
                e.preventDefault();
                ageError?.classList.remove('hidden');
                minInput?.focus();
            } else {
                ageError?.classList.add('hidden');
            }
        });
    }

    const nameInput = document.getElementById('project-name-input') as HTMLInputElement | null;
    const browseBtn = document.getElementById('browse-btn') as HTMLButtonElement | null;
    const fileInput = document.getElementById('image-file-input') as HTMLInputElement | null;
    const imagePreview = document.getElementById('image-preview') as HTMLImageElement | null;
    const imagePlaceholder = document.getElementById('image-placeholder') as SVGElement | null;
    const imageUrlInput = document.getElementById('ImageUrl') as HTMLInputElement | null;

    // Open file picker when Bladeren is clicked
    browseBtn?.addEventListener('click', () => fileInput?.click());

    // Upload selected file to server
    fileInput?.addEventListener('change', async () => {
        const file = fileInput.files?.[0];
        if (!file) return;

        const compressed = await compressImage(file, 1200, 0.8);

        const formData = new FormData();
        formData.append('file', compressed, 'image.jpg');

        const response = await fetch('/Project/UploadImage', { method: 'POST', body: formData });
        if (!response.ok) return;

        const data = await response.json() as { url: string };
        if (imageUrlInput) imageUrlInput.value = data.url;
        if (imagePreview) {
            imagePreview.src = data.url;
            imagePreview.classList.remove('hidden');
        }
        imagePlaceholder?.classList.add('hidden');
    });

    // Generate image with AI
    const generateBtn = document.getElementById('generate-btn') as HTMLButtonElement | null;

    generateBtn?.addEventListener('click', async () => {
        const name = nameInput?.value?.trim();
        if (!name) {
            nameInput?.focus();
            return;
        }

        const originalText = generateBtn.textContent!;
        generateBtn.disabled = true;
        generateBtn.textContent = generateBtn.dataset.generatingText ?? 'Generating…';

        try {
            const response = await fetch(`/Project/GenerateImage?name=${encodeURIComponent(name)}`, { method: 'POST' });
            if (!response.ok) return;

            const data = await response.json() as { url: string };
            if (imageUrlInput) imageUrlInput.value = data.url;
            if (imagePreview) {
                imagePreview.src = data.url;
                imagePreview.classList.remove('hidden');
            }
            imagePlaceholder?.classList.add('hidden');
        } catch {
            // silently fail
        } finally {
            generateBtn.disabled = false;
            generateBtn.textContent = originalText;
        }
    });

    // Pre-fill image preview on page load if a URL is already stored
    if (imageUrlInput?.value) {
        if (imagePreview) {
            imagePreview.src = imageUrlInput.value;
            imagePreview.classList.remove('hidden');
        }
        imagePlaceholder?.classList.add('hidden');
    }

    // Quill rich text editor for Introduction
    const editorEl = document.getElementById('intro-editor');
    const introInput = document.getElementById('Introduction') as HTMLInputElement | null;
    if (editorEl && introInput) {
        const quill = new Quill(editorEl, {
            theme: 'snow',
            placeholder: 'Schrijf een korte introductie van je project voor de deelnemers',
            modules: {
                toolbar: [
                    ['bold', 'italic', 'underline'],
                    [{ list: 'ordered' }, { list: 'bullet' }],
                    ['link'],
                    ['clean']
                ]
            }
        });

        const introCounter = document.getElementById('intro-char-count');
        const MAX_INTRO = 5000;

        function updateIntroCounter() {
            if (!introCounter) return;
            const html = quill.root.innerHTML;
            const len = html === '<p><br></p>' ? 0 : html.length;
            introCounter.textContent = `${len} / ${MAX_INTRO}`;
            introCounter.classList.toggle('text-red-400', len > MAX_INTRO);
            introCounter.classList.toggle('text-gray-500', len <= MAX_INTRO);
        }

        quill.on('text-change', updateIntroCounter);
        updateIntroCounter();

        editorEl.closest('form')?.addEventListener('submit', (e) => {
            const html = quill.root.innerHTML;
            const content = html === '<p><br></p>' ? '' : html;
            introInput.value = content;
            if (content.length > MAX_INTRO) {
                e.preventDefault();
                introCounter?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            }
        });
    }

    // Blur-time field validation
    if (nameInput) bindBlurValidation(nameInput, getErrorSpan('Name'), (inp) => {
        if (!inp.value.trim()) return 'Projectnaam is verplicht.';
        if (inp.value.length > 100) return 'Projectnaam mag maximaal 100 tekens zijn.';
        return null;
    });

    const shortFormInput = document.getElementById('project-shortform-input') as HTMLInputElement | null;
    if (shortFormInput) bindBlurValidation(shortFormInput, getErrorSpan('ShortForm'), (inp) => {
        if (inp.value && !/^[a-z0-9](-?[a-z0-9])*$/.test(inp.value))
            return 'Alleen kleine letters, cijfers en koppeltekens zijn toegestaan.';
        return null;
    });

    const topicInput = document.querySelector<HTMLInputElement>('input[name="Topic"]');
    if (topicInput) bindBlurValidation(topicInput, getErrorSpan('Topic'), (inp) => {
        if (inp.value.length > 100) return 'Onderwerp mag maximaal 100 tekens zijn.';
        return null;
    });

    function validateAgeBlur() {
        const invalid = checkAgeRange();
        ageError?.classList.toggle('hidden', !invalid);
        const minSpan = getErrorSpan('MinAge');
        if (minSpan && minInput)
            minSpan.textContent = (minInput.value && !minInput.checkValidity())
                ? 'Leeftijd moet tussen 0 en 120 zijn.' : '';
    }
    minInput?.addEventListener('blur', validateAgeBlur);
    maxInput?.addEventListener('blur', validateAgeBlur);

    // Sections: add / remove
    const sectionsContainer = document.getElementById('sections-container');
    const addSectionBtn = document.getElementById('add-section');
    if (sectionsContainer && addSectionBtn) {
        let idx = parseInt(sectionsContainer.dataset.sectionCount ?? '0', 10);
        addSectionBtn.addEventListener('click', () => {
            const row = document.createElement('div');
            row.className = 'flex gap-2 items-center section-row';
            row.innerHTML = `<input type="text" name="Sections[${idx}]" maxlength="20" class="input input-bordered input-sm flex-1" placeholder="Sectienaam" /><button type="button" class="btn btn-ghost btn-sm text-error remove-section">\u00d7</button>`;
            sectionsContainer.appendChild(row);
            idx++;
        });
        sectionsContainer.addEventListener('click', (e) => {
            if ((e.target as HTMLElement).classList.contains('remove-section'))
                (e.target as HTMLElement).closest('.section-row')?.remove();
        });
    }
});
