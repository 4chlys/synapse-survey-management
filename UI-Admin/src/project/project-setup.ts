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

    const mainForm = document.getElementById('project-form') as HTMLFormElement | null;
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

    const browseBtn = document.getElementById('browse-btn') as HTMLButtonElement | null;
    const fileInput = document.getElementById('image-file-input') as HTMLInputElement | null;
    const imagePreview = document.getElementById('image-preview') as HTMLImageElement | null;
    const imagePlaceholder = document.getElementById('image-placeholder') as HTMLElement | null;
    const imageUrlInput = document.getElementById('ImageUrl') as HTMLInputElement | null;
    const dropZone = document.getElementById('image-drop-zone') as HTMLElement | null;
    const dragOverlay = document.getElementById('image-drag-overlay') as HTMLElement | null;
    const clearBtn = document.getElementById('image-clear-btn') as HTMLButtonElement | null;

    function setImage(url: string) {
        if (imageUrlInput) imageUrlInput.value = url;
        if (imagePreview) { imagePreview.src = url; imagePreview.classList.remove('hidden'); }
        imagePlaceholder?.classList.add('hidden');
        clearBtn?.classList.remove('hidden');
    }

    function clearImage() {
        if (imageUrlInput) imageUrlInput.value = '';
        if (imagePreview) { imagePreview.src = ''; imagePreview.classList.add('hidden'); }
        imagePlaceholder?.classList.remove('hidden');
        clearBtn?.classList.add('hidden');
    }

    async function uploadFile(file: File) {
        const compressed = await compressImage(file, 1200, 0.8);
        const formData = new FormData();
        formData.append('file', compressed, 'image.jpg');
        const response = await fetch('/Project/UploadImage', { method: 'POST', body: formData });
        if (!response.ok) return;
        const data = await response.json() as { url: string };
        setImage(data.url);
    }

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

    // Drag & drop onto the drop zone
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

    clearBtn?.addEventListener('click', (e) => {
        e.stopPropagation();
        clearImage();
    });

    // Generate image with AI
    const generateBtn = document.getElementById('generate-btn') as HTMLButtonElement | null;

    generateBtn?.addEventListener('click', async () => {
        // Image generation uses the active tab's content as the prompt.
        const activeLang = langs.find(l => l.tab.getAttribute('aria-selected') === 'true') ?? langs[0];
        const activeName = activeLang?.nameInput;
        const name = activeName?.value?.trim();
        if (!name) { activeName?.focus(); return; }

        const originalText = generateBtn.textContent!;
        generateBtn.disabled = true;
        generateBtn.textContent = generateBtn.dataset.generatingText ?? 'Generating…';

        try {
            // Send as JSON body to avoid query-string length limits.
            // The intro field can hold up to 5000 chars of Quill HTML; URI-encoding
            // that would exceed the ~8 KB limit many proxies enforce.
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

    // Pre-fill image preview on page load if a URL is already stored
    if (imageUrlInput?.value) {
        if (imagePreview) { imagePreview.src = imageUrlInput.value; imagePreview.classList.remove('hidden'); }
        imagePlaceholder?.classList.add('hidden');
    }

    // Multi-language setup: language picker, tab strip, per-language Quill,
    // status dots, and submit-time per-language validation.
    const MAX_INTRO = 5000;
    const QUILL_EMPTY = '<p><br></p>';
    const tabStrip = document.getElementById('translation-tabs');
    const panes = Array.from(document.querySelectorAll<HTMLElement>('[data-translation-pane]'));
    const langCheckboxes = Array.from(document.querySelectorAll<HTMLInputElement>('[data-language-checkbox]'));
    const langRadios = Array.from(document.querySelectorAll<HTMLInputElement>('[data-language-default]'));

    type LangCtx = {
        code: string;
        pane: HTMLElement;
        tab: HTMLButtonElement;
        nameInput: HTMLInputElement;
        topicInput: HTMLInputElement;
        introHidden: HTMLInputElement;
        introCounter: HTMLElement | null;
        quill: Quill | null;
    };

    const langs: LangCtx[] = panes
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
                placeholder: 'Schrijf een korte introductie van je project voor de deelnemers',
                modules: {
                    toolbar: [
                        ['bold', 'italic', 'underline'],
                        [{ list: 'ordered' }, { list: 'bullet' }],
                        ['link'],
                        ['clean']
                    ]
                }
            }) : null;
            // Quill's default .ql-editor min-height is ~7em which looks cramped;
            // bump it so the editor reads as a proper text area on first paint.
            if (quill) (quill.root as HTMLElement).style.minHeight = '12rem';
            return tab && nameInput && topicInput && introHidden
                ? { code, pane, tab, nameInput, topicInput, introHidden, introCounter, quill }
                : null;
        })
        .filter((x): x is LangCtx => x !== null);

    function isSupported(code: string): boolean {
        const cb = langCheckboxes.find(c => c.value === code);
        return cb?.checked ?? false;
    }

    function updateStatusDot(l: LangCtx) {
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

    function updateIntroCounter(l: LangCtx) {
        if (!l.introCounter || !l.quill) return;
        const html = l.quill.root.innerHTML;
        const len = html === QUILL_EMPTY ? 0 : html.length;
        l.introCounter.textContent = `${len} / ${MAX_INTRO}`;
        l.introCounter.classList.toggle('text-red-400', len > MAX_INTRO);
        l.introCounter.classList.toggle('text-base-content/40', len <= MAX_INTRO);
    }

    // Segmented-control active state: raised white pill on the muted strip.
    const ACTIVE_TAB_CLASSES = ['bg-base-100', 'text-primary', 'shadow-sm', 'ring-1', 'ring-base-300'];
    function activateTab(code: string) {
        langs.forEach(l => {
            const active = l.code === code;
            l.pane.hidden = !active;
            ACTIVE_TAB_CLASSES.forEach(c => l.tab.classList.toggle(c, active));
            l.tab.setAttribute('aria-selected', active ? 'true' : 'false');
        });
    }

    function ensureVisibleActiveTab() {
        // If the currently-active tab belongs to an unchecked language, jump
        // to the first supported one.
        const active = langs.find(l => l.tab.getAttribute('aria-selected') === 'true');
        if (active && isSupported(active.code)) return;
        const first = langs.find(l => isSupported(l.code));
        if (first) activateTab(first.code);
    }

    function syncRadiosToCheckboxes() {
        // Disable the "default" radio of any unchecked language; if the
        // currently-selected default got disabled, promote the first
        // remaining supported language to default.
        langRadios.forEach(r => {
            r.disabled = !isSupported(r.value);
        });
        const checkedRadio = langRadios.find(r => r.checked && !r.disabled);
        if (!checkedRadio) {
            const promote = langRadios.find(r => !r.disabled);
            if (promote) promote.checked = true;
        }
    }

    function syncCrossLanguageRow() {
        const row = document.getElementById('cross-language-row');
        if (!row) return;
        const count = langCheckboxes.filter(c => c.checked).length;
        row.hidden = count <= 1;
    }

    function syncDefaultBadges() {
        // The "· default" badge sits on each tab; show it on whichever
        // language's radio is currently selected.
        const selected = langRadios.find(r => r.checked && !r.disabled)?.value;
        document.querySelectorAll<HTMLElement>('[data-default-badge]').forEach(b => {
            b.hidden = b.dataset.defaultBadge !== selected;
        });
    }

    langs.forEach(l => {
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

    langCheckboxes.forEach(cb => {
        cb.addEventListener('change', () => {
            // Prevent unchecking the last supported language.
            if (!cb.checked && langCheckboxes.filter(c => c.checked).length === 0) {
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

    langRadios.forEach(r => r.addEventListener('change', syncDefaultBadges));

    syncRadiosToCheckboxes();
    syncDefaultBadges();
    syncCrossLanguageRow();
    ensureVisibleActiveTab();

    // Submit-time validation + Quill→hidden-input sync. The form has the
    // wizard "next" button which submits with name=submitAction value=next;
    // when "next" is in flight, Topic and Introduction also become required.
    mainForm?.addEventListener('submit', (e) => {
        const submitter = (e as SubmitEvent).submitter as HTMLButtonElement | null;
        const requireFull = submitter?.name === 'submitAction' && submitter?.value === 'next';

        // Sync Quill HTML into hidden inputs for every language, regardless of
        // checked state — typed-but-unchecked data is preserved in the POST and
        // discarded server-side, so a later re-check restores it.
        for (const l of langs) {
            if (!l.quill) continue;
            const html = l.quill.root.innerHTML;
            l.introHidden.value = html === QUILL_EMPTY ? '' : html;
        }

        // Validate per supported language. Switch to the first bad tab and
        // focus the offending field.
        const supported = langs.filter(l => isSupported(l.code));
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
            const emptyInput = sectionsContainer.querySelector<HTMLInputElement>('input[type="text"][value=""]') ??
                [...sectionsContainer.querySelectorAll<HTMLInputElement>('input[type="text"]')].find(i => !i.value.trim());
            if (emptyInput) {
                emptyInput.focus();
                emptyInput.classList.add('input-error');
                emptyInput.addEventListener('input', () => emptyInput.classList.remove('input-error'), { once: true });
                return;
            }
            const row = document.createElement('div');
            row.className = 'flex gap-1 items-center section-row';
            row.innerHTML = `<input type="text" name="Sections[${idx}]" maxlength="20" class="input input-bordered input-sm flex-1" placeholder="Sectienaam" /><button type="button" class="btn btn-ghost btn-sm text-error remove-section">\u00d7</button>`;
            sectionsContainer.appendChild(row);
            row.querySelector('input')?.focus();
            idx++;
        });
        sectionsContainer.addEventListener('click', (e) => {
            if ((e.target as HTMLElement).classList.contains('remove-section'))
                (e.target as HTMLElement).closest('.section-row')?.remove();
        });
    }

    // Strip empty section rows before submit to prevent server exceptions
    mainForm?.addEventListener('submit', () => {
        sectionsContainer?.querySelectorAll<HTMLElement>('.section-row').forEach(row => {
            const input = row.querySelector<HTMLInputElement>('input[type="text"]');
            if (input && !input.value.trim()) row.remove();
        });
    });
});
