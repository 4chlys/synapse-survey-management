export function initSettingsRadio(): void {
    document.querySelectorAll<HTMLElement>('.settings-model-option, .settings-mode-option').forEach((label) => {
        const radio = label.querySelector<HTMLInputElement>('input[type="radio"]');
        if (!radio) return;

        radio.addEventListener('change', () => {
            const activeClass = label.classList.contains('settings-model-option')
                ? 'settings-model-option--active'
                : 'settings-mode-option--active';

            label.closest('form')
                ?.querySelectorAll(`.${activeClass}`)
                .forEach((el) => el.classList.remove(activeClass));

            label.classList.add(activeClass);
        });
    });
}
