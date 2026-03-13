import { wireIdeationForm } from './ideationForm';

export function initIdeation(): void {
    window.showCreateForm  = showCreateForm;
    window.hideCreateForm  = hideCreateForm;
    window.showCommentForm = showCommentForm;
    window.hideCommentForm = hideCommentForm;
    window.showCommentViewModelForm = showCommentForm;
    window.hideCommentViewModelForm = hideCommentForm;
    window.showEmojiPicker = showEmojiPicker;

    // Read the projectId that the Razor layout embeds as a meta tag
    const projectId = getProjectId();

    // ── Create-idea form ──────────────────────────────────────────────────────
    const createFormEl = document.getElementById('createForm');
    const createForm   = createFormEl?.querySelector<HTMLFormElement>('form') ?? null;
    if (createForm && projectId != null) {
        wireIdeationForm(createForm, 'description', projectId);
    }

    // ── Add-comment form ──────────────────────────────────────────────────────
    const commentFormEl = document.getElementById('commentForm');
    const commentForm   = commentFormEl?.querySelector<HTMLFormElement>('form') ?? null;
    if (commentForm && projectId != null) {
        wireIdeationForm(commentForm, 'text', projectId);
    }
}

function getProjectId(): number | null {
    // The layout already sets <meta name="api-base"> — we piggyback a
    // <meta name="project-id"> added to the ideation views.
    const meta = document.querySelector<HTMLMetaElement>('meta[name="project-id"]');
    if (meta?.content) return parseInt(meta.content, 10);
    // Fallback: parse from the URL path  /1/Ideation/...
    const match = window.location.pathname.match(/^\/(\d+)\//);
    return match ? parseInt(match[1], 10) : null;
}

function showCreateForm(): void {
    const form = document.getElementById('createForm');
    if (form) {
        form.classList.remove('hidden');
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }
}

function hideCreateForm(): void {
    document.getElementById('createForm')?.classList.add('hidden');
}

function showCommentForm(): void {
    document.getElementById('commentForm')?.classList.remove('hidden');
}

function hideCommentForm(): void {
    document.getElementById('commentForm')?.classList.add('hidden');
}

function showEmojiPicker(targetId: string): void {
    document.querySelectorAll('[id^="picker-"]').forEach(el => {
        if (el.id !== `picker-${targetId}`) el.classList.add('hidden');
    });
    document.getElementById(`picker-${targetId}`)?.classList.toggle('hidden');
}

declare global {
    interface Window {
        showCreateForm:           () => void;
        hideCreateForm:           () => void;
        showCommentForm:          () => void;
        hideCommentForm:          () => void;
        showCommentViewModelForm: () => void;
        hideCommentViewModelForm: () => void;
        showEmojiPicker:          (targetId: string) => void;
    }
}