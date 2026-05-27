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

async function checkUniqueness(shortForm: string, projectId: string | null, msgEl: HTMLElement): Promise<void> {
    if (!shortForm) {
        msgEl.textContent = '';
        shortFormValid = true;
        return;
    }

    const params = new URLSearchParams({ shortForm });
    if (projectId) params.set('projectId', projectId);

    try {
        const res = await fetch(`/Project/CheckShortForm?${params.toString()}`);
        const data = await res.json() as { available: boolean };
        if (data.available) {
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

document.addEventListener('DOMContentLoaded', () => {
    const nameInputs = Array.from(
        document.querySelectorAll<HTMLInputElement>('[data-translation-field="name"]')
    );
    const defaultRadios = Array.from(
        document.querySelectorAll<HTMLInputElement>('[data-language-default]')
    );
    const shortFormInput = document.getElementById('project-shortform-input') as HTMLInputElement | null;
    const projectIdInput = document.getElementById('ProjectId') as HTMLInputElement | null;
    const msgEl = document.getElementById('shortform-availability-msg');
    const form = shortFormInput?.closest('form');

    if (nameInputs.length === 0 || !shortFormInput) return;
    if (shortFormInput.hasAttribute('readonly')) return;

    const projectId = projectIdInput?.value || null;
    let dirty = shortFormInput.value.length > 0;

    function getDefaultNameInput(): HTMLInputElement | null {
        const selected = defaultRadios.find(r => r.checked && !r.disabled);
        if (selected) {
            return nameInputs.find(i => i.dataset.language === selected.value) ?? null;
        }
        return nameInputs[0] ?? null;
    }

    function scheduleCheck(value: string): void {
        if (checkTimeout !== null) clearTimeout(checkTimeout);
        if (value === lastChecked || !msgEl) return;
        lastChecked = value;
        checkTimeout = setTimeout(() => { void checkUniqueness(value, projectId, msgEl); }, 400);
    }

    function regenerateFromDefault(): void {
        if (dirty) return;
        const inp = getDefaultNameInput();
        if (!inp || !shortFormInput) return;
        const normalized = normalizeToShortForm(inp.value);
        shortFormInput.value = normalized;
        scheduleCheck(normalized);
    }

    shortFormInput.addEventListener('input', () => {
        dirty = true;
        scheduleCheck(shortFormInput.value);
    });

    shortFormInput.addEventListener('blur', () => {
        if (checkTimeout !== null) clearTimeout(checkTimeout);
        const val = shortFormInput.value;
        if (msgEl && val !== lastChecked) {
            lastChecked = val;
            void checkUniqueness(val, projectId, msgEl);
        }
    });

    // Only the default-language's Name drives the shortform; other languages'
    // Name inputs fire too but only act when they ARE the current default.
    nameInputs.forEach(inp => {
        inp.addEventListener('input', () => {
            const def = getDefaultNameInput();
            if (inp === def) regenerateFromDefault();
        });
    });

    // When the admin switches the default language while shortform is still
    // pristine, regenerate from the newly-default Name.
    defaultRadios.forEach(r => r.addEventListener('change', regenerateFromDefault));

    form?.addEventListener('submit', (e) => {
        if (!shortFormValid) {
            e.preventDefault();
            if (msgEl) msgEl.textContent = 'Kies een unieke shortform voordat je opslaat.';
        }
    });
});
