export function initSettingsPrompts(): void {
    document.addEventListener('click', (e) => {
        const toggle = (e.target as Element | null)?.closest<HTMLElement>('[data-toggle-prompts]');
        if (!toggle) return;

        const bodyId = toggle.dataset.togglePrompts;
        const chevronId = toggle.dataset.toggleChevron;
        if (!bodyId) return;

        document.getElementById(bodyId)?.classList.toggle('is-open');
        if (chevronId) document.getElementById(chevronId)?.classList.toggle('is-open');
    });
}
