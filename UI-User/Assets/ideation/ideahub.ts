import { wireIdeationForm } from './ideationForm';
import { wireModerationField } from './moderation';

export function initIdeation(): void {
    const projectId = getProjectId();

    // Create-idea form
    const createFormEl = document.getElementById('createForm');
    const createForm   = createFormEl?.querySelector<HTMLFormElement>('form') ?? null;
    if (createForm && projectId != null) {
        wireIdeationForm(createForm, 'description', projectId);
        const descriptionTextarea = createForm.querySelector<HTMLTextAreaElement>('textarea[name="description"]');
        if (descriptionTextarea) {
            wireModerationField(descriptionTextarea, projectId);
        }
    }

    // Add-comment form
    const commentFormEl = document.getElementById('commentForm');
    const commentForm   = commentFormEl?.querySelector<HTMLFormElement>('form') ?? null;
    if (commentForm && projectId != null) {
        wireIdeationForm(commentForm, 'text', projectId);
        const commentTextarea = commentForm.querySelector<HTMLTextAreaElement>('textarea[name="text"]');
        if (commentTextarea) {
            wireModerationField(commentTextarea, projectId);
        }
    }

    // Delegate all data-action button clicks
    document.addEventListener('click', (e: MouseEvent) => {
        const target = (e.target as Element).closest<HTMLElement>('[data-action]');
        if (!target) return;

        const action = target.dataset.action;

        switch (action) {
            case 'show-create-form':
                e.preventDefault();
                createFormEl?.classList.remove('hidden');
                window.scrollTo({ top: 0, behavior: 'smooth' });
                break;

            case 'hide-create-form':
                e.preventDefault();
                createFormEl?.classList.add('hidden');
                break;

            case 'show-comment-form':
                e.preventDefault();
                commentFormEl?.classList.remove('hidden');
                break;

            case 'hide-comment-form':
                e.preventDefault();
                commentFormEl?.classList.add('hidden');
                break;

            case 'toggle-emoji-picker': {
                e.preventDefault();
                e.stopPropagation();
                const targetId = target.dataset.target;
                if (!targetId) break;
                document.querySelectorAll<HTMLElement>('[id^="picker-"]').forEach(el => {
                    if (el.id !== targetId) el.classList.add('hidden');
                });
                document.getElementById(targetId)?.classList.toggle('hidden');
                break;
            }
        }
    });
}

function getProjectId(): number | null {
    const meta = document.querySelector<HTMLMetaElement>('meta[name="project-id"]');
    if (meta?.content) return parseInt(meta.content, 10);
    const match = window.location.pathname.match(/^\/(\d+)\//);
    return match ? parseInt(match[1], 10) : null;
}