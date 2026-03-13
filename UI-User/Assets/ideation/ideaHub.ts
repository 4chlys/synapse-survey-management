import { wireModerationForm } from './moderation';

export function initIdeation(): void {
    window.showCreateForm = showCreateForm;
    window.hideCreateForm = hideCreateForm;
    window.showCommentForm = showCommentForm;
    window.hideCommentForm = hideCommentForm;
    window.showEmojiPicker = showEmojiPicker;
    
    const createForm = document.getElementById('createForm') as HTMLFormElement | null;
    if (createForm) {
        wireModerationForm(createForm);
    }

    const commentForm = document.getElementById('commentForm') as HTMLFormElement | null;
    if (commentForm) {
        wireModerationForm(commentForm);
    }
}

/**
 * Before submitting a moderated form, JS calls this to stamp the hidden
 * moderationStatus input with the correct value so the server knows whether
 * the content was accepted clean, or flagged-but-submitted-anyway.
 *
 * Called internally by wireModerationForm via the data-flagged attribute
 * pattern: if textarea has data-flagged="true" → "Flagged", else "Unflagged".
 *
 * This helper is exported so it can be tested in isolation.
 */
export function resolveModerationStatus(form: HTMLFormElement): void {
    const textareas = Array.from(form.querySelectorAll<HTMLTextAreaElement>('textarea'));
    const statusInput = form.querySelector<HTMLInputElement>('input[name="moderationStatus"]');
    if (!statusInput) return;

    const anyFlagged = textareas.some(ta => ta.dataset.flagged === 'true');
    statusInput.value = anyFlagged ? 'Flagged' : 'Unflagged';
}

function showCreateForm(): void {
    const form = document.getElementById('createForm');
    if (form) {
        form.classList.remove('hidden');
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }
}

function hideCreateForm(): void {
    const form = document.getElementById('createForm');
    if (form) form.classList.add('hidden');
}

function showCommentForm(): void {
    const form = document.getElementById('commentForm');
    if (form) form.classList.remove('hidden');
}

function hideCommentForm(): void {
    const form = document.getElementById('commentForm');
    if (form) form.classList.add('hidden');
}

function showEmojiPicker(targetId: string): void {
    document.querySelectorAll('[id^="picker-"]').forEach(el => {
        if (el.id !== `picker-${targetId}`) {
            el.classList.add('hidden');
        }
    });
    const picker = document.getElementById(`picker-${targetId}`);
    if (picker) picker.classList.toggle('hidden');
}

declare global {
    interface Window {
        showCreateForm: () => void;
        hideCreateForm: () => void;
        showCommentForm: () => void;
        hideCommentForm: () => void;
        showEmojiPicker: (targetId: string) => void;
    }
}