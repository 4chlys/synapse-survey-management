export function initIdeation(): void {
    (window as any).showCreateForm = showCreateForm;
    (window as any).hideCreateForm = hideCreateForm;
    (window as any).showCommentForm = showCommentForm;
    (window as any).hideCommentForm = hideCommentForm;
    (window as any).showEmojiPicker = showEmojiPicker;
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
    // Close all other open pickers first
    document.querySelectorAll('[id^="picker-"]').forEach(el => {
        if (el.id !== `picker-${targetId}`) {
            el.classList.add('hidden');
        }
    });
    const picker = document.getElementById(`picker-${targetId}`);
    if (picker) picker.classList.toggle('hidden');
}