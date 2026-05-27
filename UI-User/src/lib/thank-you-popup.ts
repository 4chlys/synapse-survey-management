let popup: HTMLElement | null = null;

function openThankYouPopup(): void {
    if (!popup) return;
    popup.classList.remove('hidden');
    popup.classList.add('flex');
    document.body.style.overflow = 'hidden';
}

function closeThankYouPopup(): void {
    if (!popup) return;
    popup.classList.add('hidden');
    popup.classList.remove('flex');
    document.body.style.overflow = '';
}

export function initThankYouPopup(): void {
    popup = document.getElementById('thank-you-popup');
    if (!popup || popup.dataset.bound) return;
    popup.dataset.bound = 'true';

    document.addEventListener('click', (e) => {
        const target = e.target;
        if (!(target instanceof Element)) return;

        const opener = target.closest('[data-action="show-thank-you"]');
        if (opener) {
            e.preventDefault();
            openThankYouPopup();
            return;
        }

        if (target.closest('[data-action="close-thank-you"]')) {
            e.preventDefault();
            closeThankYouPopup();
            return;
        }

        if (target === popup) {
            closeThankYouPopup();
        }
    });

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && popup && !popup.classList.contains('hidden')) {
            closeThankYouPopup();
        }
    });
}