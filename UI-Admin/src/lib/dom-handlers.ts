// Generic data-attribute event handlers. Wired once on init so that .cshtml
// files can express common UI behaviour without inline JS.

export function initDomHandlers(): void {
    document.addEventListener('click', (e) => {
        const target = e.target as Element | null;
        if (!target) return;

        const opener = target.closest<HTMLElement>('[data-open-modal]');
        if (opener) {
            const id = opener.dataset.openModal;
            if (id) (document.getElementById(id) as HTMLDialogElement | null)?.showModal();
            return;
        }

        const closer = target.closest<HTMLElement>('[data-close-modal]');
        if (closer) {
            const id = closer.dataset.closeModal;
            if (id) (document.getElementById(id) as HTMLDialogElement | null)?.close();
            return;
        }

        const copier = target.closest<HTMLElement>('[data-copy-from]');
        if (copier) {
            const sel = copier.dataset.copyFrom;
            if (!sel) return;
            const src = document.querySelector<HTMLInputElement>(sel);
            if (src) navigator.clipboard.writeText(src.value);
            return;
        }

        const confirmer = target.closest<HTMLElement>('[data-confirm]');
        if (confirmer && !confirm(confirmer.dataset.confirm ?? '')) {
            e.preventDefault();
            e.stopImmediatePropagation();
        }
    });

    document.addEventListener('change', (e) => {
        const trigger = e.target as (HTMLSelectElement | HTMLInputElement) | null;
        if (trigger?.matches?.('[data-submit-on-change]') && trigger.form) {
            trigger.form.submit();
        }
    });

    document.addEventListener('submit', (e) => {
        const form = e.target as HTMLFormElement;
        const msg = form.dataset.confirmSubmit;
        if (msg && !confirm(msg)) e.preventDefault();
    });
}
