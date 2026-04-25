import {wireIdeationForm} from './ideationform';
import {wireModerationField} from './moderation';

interface EmojiReaction {
    emojiId: number;
    symbol: string;
    count: number;
    postedByCurrentUser: boolean;
}

export function initIdeation(): void {
    const projectId = getProjectId();

    // Create-idea form
    const createFormEl = document.getElementById('createForm');
    const createForm = createFormEl?.querySelector<HTMLFormElement>('form') ?? null;
    if (createForm && projectId != null) {
        wireIdeationForm(createForm, 'description', projectId, 'idea');
        const descriptionTextarea = createForm.querySelector<HTMLTextAreaElement>('textarea[name="description"]');
        if (descriptionTextarea) {
            wireModerationField(descriptionTextarea, projectId);
        }
    }

    // Add-comment form
    const commentFormEl = document.getElementById('commentForm');
    const commentForm = commentFormEl?.querySelector<HTMLFormElement>('form') ?? null;
    if (commentForm && projectId != null) {
        wireIdeationForm(commentForm, 'text', projectId, 'comment');
        const commentTextarea = commentForm.querySelector<HTMLTextAreaElement>('textarea[name="text"]');
        if (commentTextarea) {
            wireModerationField(commentTextarea, projectId);
        }
    }

    // Emoji reaction forms — intercept and update in place without page reload
    document.addEventListener('submit', async (e: Event) => {
        const form = e.target as HTMLFormElement;
        if (!form.hasAttribute('data-emoji-form')) return;

        e.preventDefault();

        // For picker forms, the submitter button carries the emojiId as name/value
        const submitter = (e as SubmitEvent).submitter as HTMLButtonElement | null;
        const formData = new FormData(form);
        if (submitter?.name) {
            formData.set(submitter.name, submitter.value);
        }

        // Find context from the nearest data-emoji-container
        const emojiContainer = form.closest<HTMLElement>('[data-emoji-container]');
        const ideaId = form.dataset.ideaId ?? emojiContainer?.dataset.ideaId;
        const commentId = form.dataset.commentId ?? emojiContainer?.dataset.commentId;

        // The target element to update (could be idea reactions or comment reactions)
        const targetId = commentId ? `emoji-reactions-comment-${commentId}` : `emoji-reactions-${ideaId}`;
        const targetEl = document.getElementById(targetId);

        if (!targetEl || !ideaId) return;

        const projectIdVal = emojiContainer?.dataset.projectId ?? getProjectId()?.toString();
        const categoryId = emojiContainer?.dataset.categoryId ?? (formData.get('categoryId') as string | null) ?? undefined;
        const returnPage = emojiContainer?.dataset.returnPage ?? (formData.get('returnPage') as string | null) ?? 'Detail';

        if (!projectIdVal || !categoryId) return;

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                headers: {'X-Requested-With': 'XMLHttpRequest'},
                body: formData,
            });
            if (!response.ok) return;

            const emojis: EmojiReaction[] = await response.json();
            targetEl.innerHTML = renderReactionButtons(
                emojis, projectIdVal, categoryId, ideaId, returnPage, commentId
            );

            // Hide the picker if it was open
            const pickerId = emojiContainer?.dataset.pickerId ?? form.closest('[id^="picker-"]')?.id;
            if (pickerId) document.getElementById(pickerId)?.classList.add('hidden');
        } catch {
            // Fallback: let the browser submit normally
            form.submit();
        }
    });

    // Delegate all data-action button clicks
    document.addEventListener('click', (e: MouseEvent) => {
        const target = (e.target as Element).closest<HTMLElement>('[data-action]');
        if (!target) return;

        const action = target.dataset.action;

        switch (action) {
            case 'show-create-form':
                e.preventDefault();
                createFormEl?.classList.remove('hidden');
                window.scrollTo({top: 0, behavior: 'smooth'});
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

function renderReactionButtons(
    emojis: EmojiReaction[],
    projectId: string,
    categoryId: string,
    ideaId: string,
    returnPage: string,
    commentId?: string
): string {
    const token = document.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]')?.value ?? '';

    return emojis.map(emoji => {
        const actionBase = commentId ? 'CommentEmoji' : 'IconComment';
        const action = emoji.postedByCurrentUser ? `Remove${actionBase}` : `Add${actionBase}`;
        const url = `/${projectId}/Ideation/${action}`;

        const btnClass = emoji.postedByCurrentUser
            ? 'bg-primary/10 border border-primary/30 text-primary hover:bg-primary/20'
            : 'bg-base-200 border border-base-300 text-base-content/70 hover:bg-base-300';

        return `<form action="${url}" method="post" data-emoji-form data-idea-id="${ideaId}" ${commentId ? `data-comment-id="${commentId}"` : ''}>
            <input type="hidden" name="__RequestVerificationToken" value="${token}" />
            <input type="hidden" name="projectId" value="${projectId}" />
            <input type="hidden" name="categoryId" value="${categoryId}" />
            <input type="hidden" name="ideaId" value="${ideaId}" />
            ${commentId ? `<input type="hidden" name="commentId" value="${commentId}" />` : ''}
            <input type="hidden" name="emojiId" value="${emoji.emojiId}" />
            <input type="hidden" name="returnPage" value="${returnPage}" />
            <button type="submit"
                    class="inline-flex items-center gap-1.5 px-3 py-2 rounded-full text-xs font-semibold transition-colors cursor-pointer min-h-[32px] ${btnClass}">
                <span class="text-base leading-none">${emoji.symbol}</span>
                <span class="leading-none font-semibold">${emoji.count}</span>
            </button>
        </form>`;
    }).join('');
}

function getProjectId(): number | null {
    const meta = document.querySelector<HTMLMetaElement>('meta[name="project-id"]');
    if (meta?.content) return parseInt(meta.content, 10);
    const match = window.location.pathname.match(/^\/(\d+)\//);
    return match ? parseInt(match[1], 10) : null;
}