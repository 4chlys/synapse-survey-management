let bound = false;

export function initThankYouPopup(): void {
    const popup = document.getElementById('thank-you-popup');
    if (!popup || bound) return;
    bound = true;

    const open = () => {
        popup.classList.remove('hidden');
        popup.classList.add('flex');
        document.body.style.overflow = 'hidden';
    };

    const close = () => {
        popup.classList.add('hidden');
        popup.classList.remove('flex');
        document.body.style.overflow = '';
    };

    document.addEventListener('click', (e) => {
        const target = e.target;
        if (!(target instanceof Element)) return;

        const opener = target.closest('[data-action="show-thank-you"]');
        if (opener) {
            e.preventDefault();
            open();
            return;
        }

        if (target.closest('[data-action="close-thank-you"]')) {
            e.preventDefault();
            close();
            return;
        }

        if (target === popup) {
            close();
        }
    });

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && !popup.classList.contains('hidden')) {
            close();
        }
    });
}
