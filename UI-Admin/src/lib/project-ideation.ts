// Preset emoji name groups. Names are stable identifiers; IDs are dynamic
// and read from data attributes rendered by the server.
const PRESETS: Record<string, string[] | null> = {
    all:          null,
    standard:     ['heart', 'thumbsup', 'fire', 'check', 'trophy'],
    innovation:   ['lightbulb', 'rocket', 'thinking', 'star', 'target'],
    enthusiasm:   ['fire', 'rocket', 'party', 'raisinghands', 'muscle'],
};

let _emojiGrid: HTMLElement | null = null;
let _nameToId: Record<string, number> = {};
let _activeTab = 'all';
let _tabs: NodeListOf<HTMLButtonElement>;

function validateCount(btn: HTMLButtonElement, categoriesError: HTMLElement | null): boolean {
    const count = parseInt(btn.dataset.projectCount ?? '0', 10);
    if (count < 1) {
        categoriesError?.classList.remove('hidden');
        return false;
    }
    categoriesError?.classList.add('hidden');
    return true;
}

function switchTab(tab: string, storageKey: string): void {
    _activeTab = tab;
    sessionStorage.setItem(storageKey, tab);
    const allowed = PRESETS[tab] ?? null;

    _emojiGrid!.querySelectorAll<HTMLElement>('[data-emoji-name]').forEach(form => {
        form.style.display =
            (!allowed || allowed.includes(form.dataset.emojiName!)) ? '' : 'none';
    });

    _tabs.forEach(btn => {
        const active = btn.dataset.tab === tab;
        btn.classList.toggle('border-primary',       active);
        btn.classList.toggle('text-primary',          active);
        btn.classList.toggle('border-transparent',   !active);
        btn.classList.toggle('text-base-content/50', !active);
    });
}

function setEmojiState(form: HTMLFormElement, enabled: boolean): void {
    form.querySelector<HTMLButtonElement>('button')!.dataset.enabled = String(enabled);
    form.action = enabled
        ? form.dataset.disableUrl!
        : form.dataset.enableUrl!;
}

function activeIds(): number[] {
    const names = PRESETS[_activeTab] ?? null;
    return names
        ? names.map(n => _nameToId[n]).filter((id): id is number => id !== undefined)
        : Object.values(_nameToId);
}

async function fetchPreset(form: HTMLFormElement, targetEnabled: boolean): Promise<void> {
    const ids  = activeIds();
    const data = new FormData(form);
    ids.forEach(id => data.append('emojiIds', String(id)));

    const res = await fetch(form.action, { method: 'POST', body: data });
    if (!res.ok) return;

    const idSet = new Set(ids);
    _emojiGrid!.querySelectorAll<HTMLFormElement>('[data-emoji-name]').forEach(emojiForm => {
        if (idSet.has(parseInt(emojiForm.dataset.emojiId!, 10)))
            setEmojiState(emojiForm, targetEnabled);
    });
}

export function initProjectIdeation(): void {
    const saveBtn = document.getElementById('save-btn') as HTMLButtonElement | null;
    const nextBtn = document.getElementById('next-btn') as HTMLButtonElement | null;

    if (saveBtn && nextBtn) {
        const categoriesError = document.getElementById('categories-error');

        // Save: validate then submit the enclosing POST form (not a GET redirect).
        saveBtn.addEventListener('click', () => {
            if (!validateCount(saveBtn, categoriesError)) return;
            (saveBtn.closest('form') as HTMLFormElement | null)?.submit();
        });

        // Next: validate then navigate via href stored in data attribute.
        nextBtn.addEventListener('click', () => {
            if (!validateCount(nextBtn, categoriesError)) return;
            window.location.href = nextBtn.dataset.href ?? '/';
        });
    }

    // Emoji preset tabs + select-all
    _emojiGrid   = document.getElementById('emoji-grid');
    _tabs        = document.querySelectorAll<HTMLButtonElement>('.emoji-tab');
    const selectAllBtn = document.getElementById('emoji-select-all-btn') as HTMLButtonElement | null;
    const clearAllBtn  = document.getElementById('emoji-clear-all-btn')  as HTMLButtonElement | null;
    const presetForm   = document.getElementById('emoji-preset-form')    as HTMLFormElement | null;
    const clearForm    = document.getElementById('emoji-clear-form')     as HTMLFormElement | null;

    if (!_emojiGrid || !_tabs.length) return;

    // Build name → id lookup from the rendered emoji forms.
    _nameToId = {};
    _emojiGrid.querySelectorAll<HTMLElement>('[data-emoji-name]').forEach(form => {
        _nameToId[form.dataset.emojiName!] = parseInt(form.dataset.emojiId!, 10);
    });

    // Persist the active tab across page reloads (e.g. after enabling/disabling an emoji).
    // Key includes the project ID so tabs don't bleed across different projects.
    const projectId  = new URLSearchParams(location.search).get('projectId') ?? '0';
    const storageKey = `emojiTab_${projectId}`;

    // Restore the last active tab, falling back to 'all' if the stored value is unknown.
    const savedTab = sessionStorage.getItem(storageKey) ?? 'all';
    switchTab(savedTab in PRESETS ? savedTab : 'all', storageKey);

    _tabs.forEach(btn =>
        btn.addEventListener('click', () => switchTab(btn.dataset.tab!, storageKey))
    );

    // AJAX toggle for individual emoji pills — no full-page reload.
    _emojiGrid.querySelectorAll<HTMLFormElement>('[data-emoji-name]').forEach(form => {
        form.addEventListener('submit', async e => {
            e.preventDefault();
            const btn = form.querySelector<HTMLButtonElement>('button')!;
            const enabling = btn.dataset.enabled !== 'true';
            btn.disabled = true;
            try {
                const res = await fetch(form.action, { method: 'POST' });
                if (res.ok) setEmojiState(form, enabling);
            } finally {
                btn.disabled = false;
            }
        });
    });

    if (selectAllBtn && presetForm) {
        selectAllBtn.addEventListener('click', async () => {
            selectAllBtn.disabled = true;
            try { await fetchPreset(presetForm, true); }
            finally { selectAllBtn.disabled = false; }
        });
    }

    if (clearAllBtn && clearForm) {
        clearAllBtn.addEventListener('click', async () => {
            clearAllBtn.disabled = true;
            try { await fetchPreset(clearForm, false); }
            finally { clearAllBtn.disabled = false; }
        });
    }
}
