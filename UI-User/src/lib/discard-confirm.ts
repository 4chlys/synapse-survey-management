/**
 * discard-confirm.ts
 *
 * Shared "Discard your conversation?" guard for the AI idea/comment chats.
 * Resolves `true` when the user confirms discarding, `false` when they choose
 * to keep editing (cancel button, backdrop click, or Escape).
 *
 * Markup lives in Views/Shared/_DiscardConfirmPopup.cshtml.
 */
export function confirmDiscard(): Promise<boolean> {
    return new Promise(resolve => {
        const overlay = document.getElementById('discard-confirm-overlay');
        const confirmBtn = document.getElementById('discard-confirm-btn-confirm');
        const cancelBtn = document.getElementById('discard-confirm-btn-cancel');

        // Fail open: if the dialog is missing, behave as the old instant-close.
        if (!overlay || !confirmBtn || !cancelBtn) {
            resolve(true);
            return;
        }

        overlay.classList.remove('hidden');
        overlay.classList.add('flex');

        function close(): void {
            overlay!.classList.add('hidden');
            overlay!.classList.remove('flex');
            confirmBtn!.removeEventListener('click', onConfirm);
            cancelBtn!.removeEventListener('click', onCancel);
            overlay!.removeEventListener('click', onBackdrop);
            document.removeEventListener('keydown', onKey);
        }

        function onConfirm(): void { close(); resolve(true); }
        function onCancel(): void { close(); resolve(false); }
        function onBackdrop(e: MouseEvent): void { if (e.target === overlay) onCancel(); }
        function onKey(e: KeyboardEvent): void {
            if (e.key === 'Escape') { e.stopPropagation(); onCancel(); }
        }

        confirmBtn.addEventListener('click', onConfirm);
        cancelBtn.addEventListener('click', onCancel);
        overlay.addEventListener('click', onBackdrop);
        document.addEventListener('keydown', onKey);

        // Land focus on the safe option so Enter keeps editing.
        requestAnimationFrame(() => (cancelBtn as HTMLButtonElement).focus());
    });
}
