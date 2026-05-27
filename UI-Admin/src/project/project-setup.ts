import Quill from 'quill';
import 'quill/dist/quill.snow.css';

import { toast, bindDirtyTracking } from '../lib/notify';
import type { LangCtx } from '../lib/project-types';

const MAX_INTRO = 5000;
const QUILL_EMPTY = '<p><br></p>';
const ACTIVE_TAB_CLASSES = ['bg-base-100', 'text-primary', 'shadow-sm', 'ring-1', 'ring-base-300'];

// Module-level state set by initProjectSetup()
let _minInput: HTMLInputElement | null = null;
let _maxInput: HTMLInputElement | null = null;
let _ageError: HTMLElement | null = null;
let _imageUrlInput: HTMLInputElement | null = null;
let _imagePreview: HTMLImageElement | null = null;
let _imagePlaceholder: HTMLElement | null = null;
let _clearBtn: HTMLButtonElement | null = null;
let _langs: LangCtx[] = [];
let _langCheckboxes: HTMLInputElement[] = [];
let _langRadios: HTMLInputElement[] = [];

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
): void {
    let dirty = false;
    function validate() {
        const msg = getMessage(input);
        if (errorSpan) errorSpan.textContent = msg ?? '';
        dirty = msg !== null;
    }
    input.addEventListener('blur', validate);
    input.addEventListener('input', () => { if (dirty) validate(); });
}

function checkAgeRange(): boolean {
    const min = _minInput?.value ? parseInt(_minInput.value, 10) : null;
    const max = _maxInput?.value ? parseInt(_maxInput.value, 10) : null;
    return min !== null && max !== null && min > max;
}

function setImage(url: string): void {
    if (_imageUrlInput) _imageUrlInput.value = url;
    if (_imagePreview) { _imagePreview.src = url; _imagePreview.classList.remove('hidden'); }
    _imagePlaceholder?.classList.add('hidden');
    _clearBtn?.classList.remove('hidden');
}

function clearImage(): void {
    if (_imageUrlInput) _imageUrlInput.value = '';
    if (_imagePreview) { _imagePreview.src = ''; _imagePreview.classList.add('hidden'); }
    _imagePlaceholder?.classList.remove('hidden');
    _clearBtn?.classList.add('hidden');
}

async function uploadFile(file: File): Promise<void> {
    const compressed = await compressImage(file, 1200, 0.8);
    const formData = new FormData();
    formData.append('file', compressed, 'image.jpg');
    const response = await fetch('/Project/UploadImage', { method: 'POST', body: formData });
    if (!response.ok) return;
    const data = await response.json() as { url: string };
    setImage(data.url);
}

function isSupported(code: string): boolean {
    const cb = _langCheckboxes.find(c => c.value === code);
    return cb?.checked ?? false;
}

function updateStatusDot(l: LangCtx): void {
    const dot = l.tab.querySelector<HTMLElement>('[data-translation-status]');
    if (!dot) return;
    const introHtml = l.quill?.root.innerHTML ?? l.introHidden.value;
    const introContent = introHtml === QUILL_EMPTY ? '' : introHtml;
    const complete = l.nameInput.value.trim().length > 0
        && l.topicInput.value.trim().length > 0
        && introContent.length > 0
        && introContent.length <= MAX_INTRO;
    dot.classList.toggle('bg-success', complete);
    dot.classList.toggle('bg-warning', !complete);
    dot.classList.toggle('bg-base-300', false);
}

function updateIntroCounter(l: LangCtx): void {
    if (!l.introCounter || !l.quill) return;
    const html = l.quill.root.innerHTML;
    const len = html === QUILL_EMPTY ? 0 : html.length;
    l.introCounter.textContent = `${len} / ${MAX_INTRO}`;
    l.introCounter.classList.toggle('text-red-400', len > MAX_INTRO);
    l.introCounter.classList.toggle('text-base-content/40', len <= MAX_INTRO);
}

// Segmented-control active state: raised white pill on the muted strip.
function activateTab(code: string): void {
    _langs.forEach(l => {
        const active = l.code === code;
        l.pane.hidden = !active;
        ACTIVE_TAB_CLASSES.forEach(c => l.tab.classList.toggle(c, active));
        l.tab.setAttribute('aria-selected', active ? 'true' : 'false');
    });
}

function ensureVisibleActiveTab(): void {
    // If the currently-active tab belongs to an unchecked language, jump
    // to the first supported one.
    const active = _langs.find(l => l.tab.getAttribute('aria-selected') === 'true');
    if (active && isSupported(active.code)) return;
    const first = _langs.find(l => isSupported(l.code));
    if (first) activateTab(first.code);
}

function syncRadiosToCheckboxes(): void {
    _langRadios.forEach(r => {
        r.disabled = !isSupported(r.value);
    });
    const checkedRadio = _langRadios.find(r => r.checked && !r.disabled);
    if (!checkedRadio) {
        const promote = _langRadios.find(r => !r.disabled);
        if (promote) promote.checked = true;
    }
}

function syncCrossLanguageRow(): void {
    const row = document.getElementById('cross-language-row');
    if (!row) return;
    const count = _langCheckboxes.filter(c => c.checked).length;
    row.hidden = count <= 1;
}

function syncDefaultBadges(): void {
    const selected = _langRadios.find(r => r.checked && !r.disabled)?.value;
    document.querySelectorAll<HTMLElement>('[data-default-badge]').forEach(b => {
        b.hidden = b.dataset.defaultBadge !== selected;
    });
}

function validateAgeBlur(): void {
    const invalid = checkAgeRange();
    _ageError?.classList.toggle('hidden', !invalid);
    const minSpan = getErrorSpan('MinAge');
    if (minSpan && _minInput)
        minSpan.textContent = (_minInput.value && !_minInput.checkValidity())
            ? 'Leeftijd moet tussen 0 en 120 zijn.' : '';
}

function sectionLangDisplayName(code: string): string {
    if (code === 'nl') return 'Nederlands';
    if (code === 'en') return 'English';
    return code.toUpperCase();
}

export function initProjectSetup(): void {
    _minInput  = document.getElementById('min-age-input') as HTMLInputElement | null;
    _maxInput  = document.getElementById('max-age-input') as HTMLInputElement | null;
    _ageError  = document.getElementById('age-range-error');

    const mainForm = document.getElementById('project-form') as HTMLFormElement | null;
    if (mainForm) {
        bindDirtyTracking(mainForm);
        if (new URLSearchParams(window.location.search).get('saved') === '1')
            toast('Opgeslagen', 'success');

        mainForm.addEventListener('submit', (e) => {
            if (checkAgeRange()) {
                e.preventDefault();
                _ageError?.classList.remove('hidden');
                _minInput?.focus();
            } else {
                _ageError?.classList.add('hidden');
            }
        });
    }

    const browseBtn      = document.getElementById('browse-btn') as HTMLButtonElement | null;
    const fileInput      = document.getElementById('image-file-input') as HTMLInputElement | null;
    _imagePreview        = document.getElementById('image-preview') as HTMLImageElement | null;
    _imagePlaceholder    = document.getElementById('image-placeholder') as HTMLElement | null;
    _imageUrlInput       = document.getElementById('ImageUrl') as HTMLInputElement | null;
    const dropZone       = document.getElementById('image-drop-zone') as HTMLElement | null;
    const dragOverlay    = document.getElementById('image-drag-overlay') as HTMLElement | null;
    _clearBtn            = document.getElementById('image-clear-btn') as HTMLButtonElement | null;

    browseBtn?.addEventListener('click', () => fileInput?.click());

    // Clicking the drop zone also opens the file picker
    dropZone?.addEventListener('click', (e) => {
        if ((e.target as HTMLElement).closest('#image-clear-btn')) return;
        fileInput?.click();
    });

    fileInput?.addEventListener('change', async () => {
        const file = fileInput.files?.[0];
        if (file) await uploadFile(file);
    });

    if (dropZone && fileInput) {
        let dragDepth = 0;
        dropZone.addEventListener('dragenter', (e) => {
            e.preventDefault();
            if (++dragDepth === 1) dragOverlay?.classList.remove('hidden');
        });
        dropZone.addEventListener('dragleave', () => {
            if (--dragDepth === 0) dragOverlay?.classList.add('hidden');
        });
        dropZone.addEventListener('dragover', (e) => { e.preventDefault(); });
        dropZone.addEventListener('drop', async (e) => {
            e.preventDefault();
            dragDepth = 0;
            dragOverlay?.classList.add('hidden');
            const file = e.dataTransfer?.files?.[0];
            if (file?.type.startsWith('image/')) await uploadFile(file);
        });
    }

    _clearBtn?.addEventListener('click', (e) => {
        e.stopPropagation();
        clearImage();
    });

    // Pre-fill image preview on page load if a URL is already stored
    if (_imageUrlInput?.value) {
        if (_imagePreview) { _imagePreview.src = _imageUrlInput.value; _imagePreview.classList.remove('hidden'); }
        _imagePlaceholder?.classList.add('hidden');
    }

    // Multi-language setup
    const tabStrip = document.getElementById('translation-tabs');
    const panes = Array.from(document.querySelectorAll<HTMLElement>('[data-translation-pane]'));
    _langCheckboxes = Array.from(document.querySelectorAll<HTMLInputElement>('[data-language-checkbox]'));
    _langRadios     = Array.from(document.querySelectorAll<HTMLInputElement>('[data-language-default]'));

    _langs = panes
        .map(pane => {
            const code = pane.dataset.translationPane!;
            const tab = tabStrip?.querySelector<HTMLButtonElement>(`[data-translation-tab="${code}"]`);
            const nameInput = pane.querySelector<HTMLInputElement>('[data-translation-field="name"]');
            const topicInput = pane.querySelector<HTMLInputElement>('[data-translation-field="topic"]');
            const introHidden = pane.querySelector<HTMLInputElement>('[data-translation-field="introduction"]');
            const introCounter = document.querySelector<HTMLElement>(`[data-intro-char-count="${code}"]`);
            const editorEl = pane.querySelector<HTMLElement>(`[data-intro-editor="${code}"]`);
            const quill = editorEl ? new Quill(editorEl, {
                theme: 'snow',
                placeholder: editorEl.dataset.introPlaceholder ?? '',
                modules: {
                    toolbar: [
                        ['bold', 'italic', 'underline'],
                        [{ list: 'ordered' }, { list: 'bullet' }],
                        ['link'],
                        ['clean']
                    ]
                }
            }) : null;
            // Bump min-height so the editor reads as a proper text area on first paint.
            if (quill) (quill.root as HTMLElement).style.minHeight = '12rem';
            return tab && nameInput && topicInput && introHidden
                ? { code, pane, tab, nameInput, topicInput, introHidden, introCounter, quill }
                : null;
        })
        .filter((x): x is LangCtx => x !== null);

    _langs.forEach(l => {
        l.tab.addEventListener('click', () => activateTab(l.code));
        l.quill?.on('text-change', () => {
            updateIntroCounter(l);
            updateStatusDot(l);
        });
        l.nameInput.addEventListener('input', () => updateStatusDot(l));
        l.topicInput.addEventListener('input', () => updateStatusDot(l));
        updateIntroCounter(l);
        updateStatusDot(l);
    });

    _langCheckboxes.forEach(cb => {
        cb.addEventListener('change', () => {
            // Prevent unchecking the last supported language.
            if (!cb.checked && _langCheckboxes.filter(c => c.checked).length === 0) {
                cb.checked = true;
                return;
            }
            const tab = tabStrip?.querySelector<HTMLButtonElement>(`[data-translation-tab="${cb.value}"]`);
            if (tab) tab.hidden = !cb.checked;
            syncRadiosToCheckboxes();
            syncDefaultBadges();
            syncCrossLanguageRow();
            ensureVisibleActiveTab();
        });
    });

    _langRadios.forEach(r => r.addEventListener('change', syncDefaultBadges));

    syncRadiosToCheckboxes();
    syncDefaultBadges();
    syncCrossLanguageRow();
    ensureVisibleActiveTab();

    // Generate image with AI
    const generateBtn = document.getElementById('generate-btn') as HTMLButtonElement | null;

    generateBtn?.addEventListener('click', async () => {
        // Image generation uses the active tab's content as the prompt.
        const activeLang = _langs.find(l => l.tab.getAttribute('aria-selected') === 'true') ?? _langs[0];
        const activeName = activeLang?.nameInput;
        const name = activeName?.value?.trim();
        if (!name) { activeName?.focus(); return; }

        const originalText = generateBtn.textContent!;
        generateBtn.disabled = true;
        generateBtn.textContent = generateBtn.dataset.generatingText ?? 'Generating…';

        try {
            const topic = activeLang?.topicInput.value?.trim();
            const introHtml = activeLang?.quill?.root.innerHTML;
            const intro = (introHtml && introHtml !== QUILL_EMPTY) ? introHtml.trim() : '';

            const response = await fetch('/Project/GenerateImage', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    name,
                    topic: topic || null,
                    intro: intro || null,
                }),
            });
            if (!response.ok) {
                const body = await response.json().catch(() => ({})) as { error?: string };
                toast(body.error ?? generateBtn.dataset.errorText ?? '', 'error');
                return;
            }

            const data = await response.json() as { url: string };
            setImage(data.url);
        } catch {
            toast(generateBtn.dataset.errorText ?? '', 'error');
        } finally {
            generateBtn.disabled = false;
            generateBtn.textContent = originalText;
        }
    });

    // Submit-time validation + Quill→hidden-input sync.
    mainForm?.addEventListener('submit', (e) => {
        const submitter = (e as SubmitEvent).submitter as HTMLButtonElement | null;
        const requireFull = submitter?.name === 'submitAction' && submitter?.value === 'next';

        // Sync Quill HTML into hidden inputs for every language.
        for (const l of _langs) {
            if (!l.quill) continue;
            const html = l.quill.root.innerHTML;
            l.introHidden.value = html === QUILL_EMPTY ? '' : html;
        }

        const supported = _langs.filter(l => isSupported(l.code));
        for (const l of supported) {
            const introOverflows = l.introHidden.value.length > MAX_INTRO;
            const missingName = !l.nameInput.value.trim();
            const missingTopic = requireFull && !l.topicInput.value.trim();
            const missingIntro = requireFull && !l.introHidden.value;
            if (missingName || missingTopic || missingIntro || introOverflows) {
                e.preventDefault();
                activateTab(l.code);
                if (missingName) l.nameInput.focus();
                else if (missingTopic) l.topicInput.focus();
                else l.quill?.focus();
                return;
            }
        }
    });

    const shortFormInput = document.getElementById('project-shortform-input') as HTMLInputElement | null;
    if (shortFormInput) bindBlurValidation(shortFormInput, getErrorSpan('ShortForm'), (inp) => {
        if (inp.value && !/^[a-z0-9](-?[a-z0-9])*$/.test(inp.value))
            return 'Alleen kleine letters, cijfers en koppeltekens zijn toegestaan.';
        return null;
    });

    _minInput?.addEventListener('blur', validateAgeBlur);
    _maxInput?.addEventListener('blur', validateAgeBlur);

    // Sections: add / remove
    const sectionsContainer = document.getElementById('sections-container');
    const addSectionBtn = document.getElementById('add-section');
    if (sectionsContainer && addSectionBtn) {
        let idx = parseInt(sectionsContainer.dataset.sectionCount ?? '0', 10);
        const availableLangs = (sectionsContainer.dataset.availableLanguages ?? 'nl').split(',');
        const multiLang = availableLangs.length > 1;
        const sectionNamePlaceholders: Record<string, string> =
            JSON.parse(sectionsContainer.dataset.sectionNamePlaceholders ?? '{}');

        addSectionBtn.addEventListener('click', () => {
            const hasEmptyRow = [...sectionsContainer.querySelectorAll<HTMLElement>('.section-row')]
                .some(row => {
                    const inputs = [...row.querySelectorAll<HTMLInputElement>('input.section-name-input')];
                    return inputs.length > 0 && inputs.every(i => !i.value.trim());
                });
            if (hasEmptyRow) {
                const firstEmptyInput = sectionsContainer.querySelector<HTMLInputElement>(
                    '.section-row input.section-name-input:not([value])'
                ) ?? [...sectionsContainer.querySelectorAll<HTMLInputElement>('.section-row input.section-name-input')]
                    .find(i => !i.value.trim());
                if (firstEmptyInput) {
                    firstEmptyInput.focus();
                    firstEmptyInput.classList.add('input-error');
                    firstEmptyInput.addEventListener('input', () => firstEmptyInput.classList.remove('input-error'), { once: true });
                }
                return;
            }
            const row = document.createElement('div');
            row.className = 'flex gap-2 items-end section-row';
            let inner = '';
            availableLangs.forEach((langCode, j) => {
                inner += `<div class="flex flex-col gap-1 flex-1">`;
                if (multiLang) inner += `<span class="text-xs text-base-content/50">${sectionLangDisplayName(langCode)}</span>`;
                inner += `<input type="hidden" name="Sections[${idx}].Translations[${j}].LanguageCode" value="${langCode}" />`;
                inner += `<input type="text" name="Sections[${idx}].Translations[${j}].Name" maxlength="20" class="input input-bordered input-sm w-full section-name-input" placeholder="${sectionNamePlaceholders[langCode] ?? ''}" data-lang="${langCode}" />`;
                inner += `</div>`;
            });
            inner += `<button type="button" class="btn btn-ghost btn-sm text-error remove-section">×</button>`;
            row.innerHTML = inner;
            sectionsContainer.appendChild(row);
            row.querySelector<HTMLInputElement>('input.section-name-input')?.focus();
            idx++;
        });
        sectionsContainer.addEventListener('click', (e) => {
            if ((e.target as HTMLElement).classList.contains('remove-section'))
                (e.target as HTMLElement).closest('.section-row')?.remove();
        });
    }

    // Strip fully-empty section rows before submit
    mainForm?.addEventListener('submit', () => {
        sectionsContainer?.querySelectorAll<HTMLElement>('.section-row').forEach(row => {
            const inputs = [...row.querySelectorAll<HTMLInputElement>('input.section-name-input')];
            if (inputs.length > 0 && inputs.every(i => !i.value.trim())) row.remove();
        });
    });
}
