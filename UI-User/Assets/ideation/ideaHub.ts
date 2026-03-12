export function initIdeation(): void {
    window.showCreateForm = showCreateForm;
    window.hideCreateForm = hideCreateForm;
    window.showCommentForm = showCommentForm;
    window.hideCommentForm = hideCommentForm;
    window.showEmojiPicker = showEmojiPicker;
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

function showReplyForm(commentId: number): void {
    document.querySelectorAll('[id^="replyForm-"]').forEach(el => {
        el.classList.add('hidden');
    });
    const form = document.getElementById(`replyForm-${commentId}`);
    if (form) form.classList.remove('hidden');
}

function hideReplyForm(commentId: number): void {
    const form = document.getElementById(`replyForm-${commentId}`);
    if (form) form.classList.add('hidden');
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