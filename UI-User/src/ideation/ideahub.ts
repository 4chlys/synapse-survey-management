import { wireIdeationForm } from './ideationform';
import { wireModerationField } from './moderation';
import { wireIdeaImageField } from './ideaimage';
import { openIdeaSupportChat } from './ideasupportchat';
import { openCommentSupportChat } from './commentsupportchat';

interface EmojiReaction {
    emojiId: number;
    symbol: string;
    count: number;
    postedByCurrentUser: boolean;
}

export function initIdeation(): void {
    const projectId = getProjectId();

    const createFormEl    = document.getElementById('createForm');
    const createForm      = createFormEl?.querySelector<HTMLFormElement>('form') ?? null;
    
    const categoryIdInput = createForm?.querySelector<HTMLInputElement>('input[name="categoryId"]') ?? null;
    const descriptionEl   = createForm?.querySelector<HTMLTextAreaElement>('textarea[name="description"]') ?? null;
    const moderationEl    = createForm?.querySelector<HTMLInputElement>('input[name="moderationToken"]') ?? null;

    const pageContainer = document.getElementById('ideation-page') ??
        document.querySelector<HTMLElement>('[data-category-id]');
    const categoryId    = parseInt(pageContainer?.dataset.categoryId ?? categoryIdInput?.value ?? '0', 10);
    const categoryName  = pageContainer?.dataset.categoryName ?? '';

    if (createForm && projectId != null && categoryId) {
        wireIdeationForm(createForm, 'description', projectId, 'idea');
        wireIdeaImageField(createForm, projectId);
        if (descriptionEl) {
            wireModerationField(descriptionEl, projectId);
        }
    }

    // Mode-selection sheet
    const modeOverlay = document.getElementById('mode-select-overlay');
    const modePanel   = document.getElementById('mode-select-panel');

    function openModeSelect(): void {
        if (!modeOverlay) return;
        modeOverlay.classList.remove('hidden');
        modeOverlay.classList.add('flex');
    }

    function closeModeSelect(): void {
        if (!modeOverlay) return;
        modeOverlay.classList.add('hidden');
        modeOverlay.classList.remove('flex');
    }

    // Dismiss mode sheet on backdrop click (outside the panel)
    modeOverlay?.addEventListener('click', (e: MouseEvent) => {
        if (modePanel && !modePanel.contains(e.target as Node)) closeModeSelect();
    });

    // Add-comment form
    const commentFormEl = document.getElementById('commentForm');
    const commentForm = commentFormEl?.querySelector<HTMLFormElement>('form') ?? null;
    const commentTextEl = commentForm?.querySelector<HTMLTextAreaElement>('textarea[name="text"]') ?? null;
    const commentModerationEl = commentForm?.querySelector<HTMLInputElement>('input[name="moderationToken"]') ?? null;
    if (commentForm && projectId != null) {
        wireIdeationForm(commentForm, 'text', projectId, 'comment');
        if (commentTextEl) {
            wireModerationField(commentTextEl, projectId);
        }
    }

    // Comment mode selector
    const commentModeOverlay = document.getElementById('comment-mode-overlay');
    const commentModePanel   = document.getElementById('comment-mode-panel');

    function openCommentModeSelect(): void {
        if (!commentModeOverlay) return;
        commentModeOverlay.classList.remove('hidden');
        commentModeOverlay.classList.add('flex');
    }

    function closeCommentModeSelect(): void {
        if (!commentModeOverlay) return;
        commentModeOverlay.classList.add('hidden');
        commentModeOverlay.classList.remove('flex');
    }

    commentModeOverlay?.addEventListener('click', (e: MouseEvent) => {
        if (commentModePanel && !commentModePanel.contains(e.target as Node))
            closeCommentModeSelect();
    });

    // Emoji reaction forms
    document.addEventListener('submit', async (e: Event) => {
        const form = e.target as HTMLFormElement;
        if (!form.hasAttribute('data-emoji-form')) return;

        e.preventDefault();

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

    // Image lightbox
    const lightbox    = document.getElementById('image-lightbox');
    const lightboxImg = document.getElementById('lightbox-img') as HTMLImageElement | null;

    function openLightbox(src: string): void {
        if (!lightbox || !lightboxImg) return;
        lightboxImg.src = src;
        lightbox.classList.remove('hidden');
        lightbox.classList.add('flex');
    }

    function closeLightbox(): void {
        if (!lightbox) return;
        lightbox.classList.add('hidden');
        lightbox.classList.remove('flex');
    }

    lightbox?.addEventListener('click', () => closeLightbox());

    document.addEventListener('keydown', (e: KeyboardEvent) => {
        if (e.key === 'Escape') closeLightbox();
    });

    // Button delegation
    document.addEventListener('click', (e: MouseEvent) => {
        const target = (e.target as Element).closest<HTMLElement>('[data-action]');
        if (!target) return;

        const action = target.dataset.action;

        switch (action) {
            case 'show-create-form':
                e.preventDefault();
                // Open the mode-selection sheet so the user can choose AI chat or classic form.
                openModeSelect();
                break;

            case 'show-ai-form':
                e.preventDefault();
                closeModeSelect();
                if (projectId != null && categoryId) {
                    openIdeaSupportChat(
                        projectId,
                        categoryId,
                        categoryName,
                        (draftText) => {
                            // Reveal the form before dispatching submit so that, if moderation
                            // flags the text and the user picks "Bewerken", the editor is visible.
                            createFormEl?.classList.remove('hidden');
                            submitIdeaFromChat(createForm, descriptionEl, moderationEl, draftText);
                        },
                        (draft) => {
                            createFormEl?.classList.remove('hidden');
                            if (draft && descriptionEl) descriptionEl.value = draft;
                            window.scrollTo({ top: 0, behavior: 'smooth' });
                        },
                    );
                }
                break;

            case 'show-manual-form':
                e.preventDefault();
                closeModeSelect();
                createFormEl?.classList.remove('hidden');
                window.scrollTo({top: 0, behavior: 'smooth'});
                break;

            case 'hide-create-form':
                e.preventDefault();
                createFormEl?.classList.add('hidden');
                break;

            case 'show-comment-mode':
                e.preventDefault();
                openCommentModeSelect();
                break;

            case 'show-ai-comment': {
                e.preventDefault();
                closeCommentModeSelect();
                if (projectId == null) break;

                const ideaText     = target.dataset.ideaText ?? '';
                const categoryName = target.dataset.categoryName ?? '';

                openCommentSupportChat(
                    projectId,
                    ideaText,
                    categoryName,
                    (commentText) => {
                        // Reveal the form before dispatching submit so that, if moderation
                        // flags the text and the user picks "Bewerken", the editor is visible.
                        commentFormEl?.classList.remove('hidden');
                        submitCommentFromChat(commentForm, commentTextEl, commentModerationEl, commentText);
                    },
                    () => {
                        commentFormEl?.classList.remove('hidden');
                    },
                );
                break;
            }

            case 'show-manual-comment':
                e.preventDefault();
                closeCommentModeSelect();
                commentFormEl?.classList.remove('hidden');
                break;

            case 'show-comment-form':
                e.preventDefault();
                commentFormEl?.classList.remove('hidden');
                break;

            case 'hide-comment-form':
                e.preventDefault();
                commentFormEl?.classList.add('hidden');
                break;

            case 'open-image': {
                e.preventDefault();
                openLightbox((target as HTMLImageElement).src);
                break;
            }

            case 'close-image': {
                e.preventDefault();
                closeLightbox();
                break;
            }

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

// Submit a comment that came from the AI chat
async function submitCommentFromChat(
    form: HTMLFormElement | null,
    textEl: HTMLTextAreaElement | null,
    moderationEl: HTMLInputElement | null,
    commentText: string,
): Promise<void> {
    if (!form || !textEl) return;

    textEl.value = commentText;
    if (moderationEl) moderationEl.value = '';

    form.dispatchEvent(new SubmitEvent('submit', { bubbles: true, cancelable: true }));
}

// Submit an idea that came from the AI chat
async function submitIdeaFromChat(
    form: HTMLFormElement | null,
    descriptionEl: HTMLTextAreaElement | null,
    moderationEl: HTMLInputElement | null,
    draftText: string,
): Promise<void> {
    if (!form || !descriptionEl) return;

    descriptionEl.value = draftText;
    if (moderationEl) moderationEl.value = '';

    form.dispatchEvent(new SubmitEvent('submit', { bubbles: true, cancelable: true }));
}

// Helpers

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