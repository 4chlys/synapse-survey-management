// Preset emoji name groups. Names are stable identifiers; IDs are dynamic
// and read from data attributes rendered by the server.
const PRESETS: Record<string, string[] | null> = {
    all:          null,
    standard:     ['heart', 'thumbsup', 'fire', 'check', 'trophy'],
    innovation:   ['lightbulb', 'rocket', 'thinking', 'star', 'target'],
    enthusiasm:   ['fire', 'rocket', 'party', 'raisinghands', 'muscle'],
};

document.addEventListener('DOMContentLoaded', () => {
    const saveBtn = document.getElementById('save-btn') as HTMLButtonElement | null;
    const nextBtn = document.getElementById('next-btn') as HTMLButtonElement | null;

    if (saveBtn && nextBtn) {
        const categoriesError = document.getElementById('categories-error');

        function validateCount(btn: HTMLButtonElement): boolean {
            const count = parseInt(btn.dataset.projectCount ?? '0', 10);
            if (count < 1) {
                categoriesError?.classList.remove('hidden');
                return false;
            }
            categoriesError?.classList.add('hidden');
            return true;
        }

        // Save: validate then submit the enclosing POST form (not a GET redirect).
        saveBtn.addEventListener('click', () => {
            if (!validateCount(saveBtn)) return;
            (saveBtn.closest('form') as HTMLFormElement | null)?.submit();
        });

        // Next: validate then navigate via href stored in data attribute.
        nextBtn.addEventListener('click', () => {
            if (!validateCount(nextBtn)) return;
            window.location.href = nextBtn.dataset.href ?? '/';
        });
    }

    // Emoji preset tabs + select-all 
    const emojiGrid    = document.getElementById('emoji-grid');
    const tabs         = document.querySelectorAll<HTMLButtonElement>('.emoji-tab');
    const selectAllBtn = document.getElementById('emoji-select-all-btn') as HTMLButtonElement | null;
    const presetForm   = document.getElementById('emoji-preset-form') as HTMLFormElement | null;

    if (!emojiGrid || !tabs.length) return;

    // Build name → id lookup from the rendered emoji forms.
    const nameToId: Record<string, number> = {};
    emojiGrid.querySelectorAll<HTMLElement>('[data-emoji-name]').forEach(form => {
        nameToId[form.dataset.emojiName!] = parseInt(form.dataset.emojiId!, 10);
    });

    let activeTab = 'all';

    function switchTab(tab: string): void {
        activeTab = tab;
        const allowed = PRESETS[tab] ?? null;

        // Show/hide emoji pills based on the active preset.
        emojiGrid!.querySelectorAll<HTMLElement>('[data-emoji-name]').forEach(form => {
            form.style.display =
                (!allowed || allowed.includes(form.dataset.emojiName!)) ? '' : 'none';
        });

        // Update tab button active styles.
        tabs.forEach(btn => {
            const active = btn.dataset.tab === tab;
            btn.classList.toggle('border-primary',       active);
            btn.classList.toggle('text-primary',          active);
            btn.classList.toggle('border-transparent',   !active);
            btn.classList.toggle('text-base-content/50', !active);
        });
    }

    // Activate the first tab on load.
    switchTab('all');

    tabs.forEach(btn =>
        btn.addEventListener('click', () => switchTab(btn.dataset.tab!))
    );

    // "Select all" — injects hidden inputs for the active preset's emoji IDs
    // into the shared form and submits it to EnableEmojiPreset on the server.
    if (selectAllBtn && presetForm) {
        selectAllBtn.addEventListener('click', () => {
            const names = PRESETS[activeTab] ?? null;
            const ids   = names
                ? names.map(n => nameToId[n]).filter((id): id is number => id !== undefined)
                : Object.values(nameToId);

            // Clear previous inputs before adding new ones (idempotent click).
            presetForm.querySelectorAll('input[name="emojiIds"]').forEach(i => i.remove());
            ids.forEach(id => {
                const input = Object.assign(document.createElement('input'), {
                    type: 'hidden', name: 'emojiIds', value: String(id),
                });
                presetForm!.appendChild(input);
            });
            presetForm.submit();
        });
    }
});
