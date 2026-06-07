function normalizeToShortForm(value: string): string {
    return value
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '')
        .slice(0, 30)
        .replace(/-+$/, '');
}

let checkTimeout: ReturnType<typeof setTimeout> | null = null;
let lastChecked = '';
let shortFormValid = true;

let _nameInputs: HTMLInputElement[] = [];
let _defaultRadios: HTMLInputElement[] = [];
let _shortFormInput: HTMLInputElement | null = null;
let _projectId: string | null = null;
let _msgEl: HTMLElement | null = null;
let _dirty = false;

async function checkUniqueness(shortForm: string, projectId: string | null, msgEl: HTMLElement, applySuggestion: boolean): Promise<void> {
    if (!shortForm) {
        msgEl.textContent = '';
        shortFormValid = true;
        return;
    }

    const params = new URLSearchParams({ shortForm });
    if (projectId) params.set('projectId', projectId);

    try {
        const res = await fetch(`/Project/CheckShortForm?${params.toString()}`);
        const data = await res.json() as { available: boolean; suggestion: string };
        if (data.available) {
            msgEl.textContent = '';
            shortFormValid = true;
        } else if (applySuggestion && !_dirty && data.suggestion && _shortFormInput) {
            // Autofill path: silently adopt the server's unique suggestion (e.g. "-2").
            _shortFormInput.value = data.suggestion;
            lastChecked = data.suggestion;
            msgEl.textContent = '';
            shortFormValid = true;
        } else {
            msgEl.textContent = 'Deze shortform is al in gebruik.';
            shortFormValid = false;
        }
    } catch {
        shortFormValid = true;
    }
}

function getDefaultNameInput(): HTMLInputElement | null {
    const selected = _defaultRadios.find(r => r.checked && !r.disabled);
    if (selected) {
        return _nameInputs.find(i => i.dataset.language === selected.value) ?? null;
    }
    return _nameInputs[0] ?? null;
}

function scheduleCheck(value: string, applySuggestion = false): void {
    if (checkTimeout !== null) clearTimeout(checkTimeout);
    if (value === lastChecked || !_msgEl) return;
    lastChecked = value;
    checkTimeout = setTimeout(() => { void checkUniqueness(value, _projectId, _msgEl!, applySuggestion); }, 400);
}

function regenerateFromDefault(): void {
    if (_dirty) return;
    const inp = getDefaultNameInput();
    if (!inp || !_shortFormInput) return;
    const normalized = normalizeToShortForm(inp.value);
    _shortFormInput.value = normalized;
    scheduleCheck(normalized, true);
}

export function initShortformAutofill(): void {
    _nameInputs = Array.from(document.querySelectorAll<HTMLInputElement>('[data-translation-field="name"]'));
    _defaultRadios = Array.from(document.querySelectorAll<HTMLInputElement>('[data-language-default]'));
    _shortFormInput = document.getElementById('project-shortform-input') as HTMLInputElement | null;
    const projectIdInput = document.getElementById('ProjectId') as HTMLInputElement | null;
    _msgEl = document.getElementById('shortform-availability-msg');
    const form = _shortFormInput?.closest('form');

    if (_nameInputs.length === 0 || !_shortFormInput) return;
    if (_shortFormInput.hasAttribute('readonly')) return;

    _projectId = projectIdInput?.value || null;
    _dirty = _shortFormInput.value.length > 0;

    _shortFormInput.addEventListener('input', () => {
        _dirty = true;
        scheduleCheck(_shortFormInput!.value);
    });

    _shortFormInput.addEventListener('blur', () => {
        if (checkTimeout !== null) clearTimeout(checkTimeout);
        const val = _shortFormInput!.value;
        if (_msgEl && val !== lastChecked) {
            lastChecked = val;
            void checkUniqueness(val, _projectId, _msgEl, false);
        }
    });

    // Only the default-language's Name drives the shortform; other languages'
    // Name inputs fire too but only act when they ARE the current default.
    _nameInputs.forEach(inp => {
        inp.addEventListener('input', () => {
            const def = getDefaultNameInput();
            if (inp === def) regenerateFromDefault();
        });
    });

    // When the admin switches the default language while shortform is still
    // pristine, regenerate from the newly-default Name.
    _defaultRadios.forEach(r => r.addEventListener('change', regenerateFromDefault));

    form?.addEventListener('submit', (e) => {
        if (!shortFormValid) {
            e.preventDefault();
            if (_msgEl) _msgEl.textContent = 'Kies een unieke shortform voordat je opslaat.';
        }
    });
}
