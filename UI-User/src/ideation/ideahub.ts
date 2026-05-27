import { wireIdeationForm, submitIdeaDirectly } from './ideationform';
import { wireModerationField } from './moderation';
import { wireIdeaImageField } from './ideaimage';
import { wireImageDescribeButton } from './imagedescribe';
import { openIdeaSupportChat } from './ideasupportchat';
import { openCommentSupportChat } from './commentsupportchat';
import { t, currentCulture } from '../lib/i18n';
import { EmojiReaction, IdeaResponse, LoadIdeasResponse } from './types';
import { escapeHtml, escapeAttr } from '../lib/utils';

export function initIdeation(): void {
    const projectId = getProjectId();
    const showAllToggle = document.getElementById('show-all-languages-toggle') as HTMLInputElement | null;

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
        wireIdeaImageField(createForm);
        wireImageDescribeButton(createForm, projectId);
        if (descriptionEl) {
            wireModerationField(descriptionEl, projectId);
        }
        createForm.addEventListener('idea:needs-edit', () => {
            createFormEl?.classList.remove('hidden');
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    }

    const modeOverlay = document.getElementById('mode-select-overlay');
    const modePanel   = document.getElementById('mode-select-panel');

    function openModeSelect(): void {
        if (!modeOverlay) return;
        modeOverlay.classList.remove('hidden');
        modeOverlay.classList.add('flex');
        document.body.style.overflow = 'hidden';
        modePanel?.querySelector<HTMLElement>('[data-action]')?.focus();
    }

    function closeModeSelect(): void {
        if (!modeOverlay) return;
        modeOverlay.classList.add('hidden');
        modeOverlay.classList.remove('flex');
        document.body.style.overflow = '';
    }

    modeOverlay?.addEventListener('click', (e: MouseEvent) => {
        if (modePanel && !modePanel.contains(e.target as Node)) closeModeSelect();
    });

    const contactPopup = document.getElementById('contactInfoPopup');
    const contactPanel = contactPopup?.querySelector<HTMLElement>('.rounded-t-3xl') ?? null;

    contactPopup?.addEventListener('click', (e: MouseEvent) => {
        if (contactPanel && !contactPanel.contains(e.target as Node)) {
            contactPopup.classList.add('hidden');
            contactPopup.classList.remove('flex');
        }
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

    const commentModeOverlay = document.getElementById('comment-mode-overlay');
    const commentModePanel   = document.getElementById('comment-mode-panel');

    function openCommentModeSelect(): void {
        if (!commentModeOverlay) return;
        commentModeOverlay.classList.remove('hidden');
        commentModeOverlay.classList.add('flex');
        document.body.style.overflow = 'hidden';
        commentModePanel?.querySelector<HTMLElement>('[data-action]')?.focus();
    }

    function closeCommentModeSelect(): void {
        if (!commentModeOverlay) return;
        commentModeOverlay.classList.add('hidden');
        commentModeOverlay.classList.remove('flex');
        document.body.style.overflow = '';
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
        if (e.key !== 'Escape') return;
        if (lightbox && !lightbox.classList.contains('hidden')) { closeLightbox(); return; }
        if (modeOverlay && !modeOverlay.classList.contains('hidden')) { closeModeSelect(); return; }
        if (commentModeOverlay && !commentModeOverlay.classList.contains('hidden')) { closeCommentModeSelect(); return; }
        if (contactPopup && !contactPopup.classList.contains('hidden')) {
            contactPopup.classList.add('hidden');
            contactPopup.classList.remove('flex');
        }
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
                            if (!createForm || !descriptionEl) return;
                            void submitIdeaDirectly(
                                createForm,
                                descriptionEl,
                                moderationEl,
                                draftText,
                                projectId,
                                () => {
                                    createFormEl?.classList.remove('hidden');
                                    window.scrollTo({ top: 0, behavior: 'smooth' });
                                },
                            );
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
            case 'refresh-ideas':
                e.preventDefault();
                handleRefreshIdeas(showAllToggle);
                break;

            case 'copy-idea': {
                e.preventDefault();
                e.stopPropagation();
                const description = target.dataset.ideaDescription ?? '';
                createFormEl?.classList.remove('hidden');
                if (descriptionEl) descriptionEl.value = description;
                window.scrollTo({ top: 0, behavior: 'smooth' });
                descriptionEl?.focus();
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

    // Restore toggle state from URL so the checkbox reflects the current filter on page load.
    const currentParams = new URLSearchParams(window.location.search);
    if (showAllToggle && currentParams.get('showAllLanguages') === 'true') {
        showAllToggle.checked = true;
    }

    showAllToggle?.addEventListener('change', () => {
        const url = new URL(window.location.href);
        if (showAllToggle.checked) {
            url.searchParams.set('showAllLanguages', 'true');
        } else {
            url.searchParams.delete('showAllLanguages');
        }
        window.location.href = url.toString();
    });
}

async function handleRefreshIdeas(toggle?: HTMLInputElement | null): Promise<void> {
    const list = document.getElementById('ideas-list');
    const btn = document.getElementById('refresh-ideas-btn');
    const counter = document.getElementById('refresh-ideas-counter');
    if (!list || !btn) return;

    if (btn.hasAttribute('disabled')) return;

    const projectId = list.dataset.projectId;
    const categoryId = list.dataset.categoryId;
    const sort = list.dataset.sort ?? 'diverse';
    const replace = list.dataset.replace === 'true';
    const maxExtra = parseInt(list.dataset.maxExtra ?? '3', 10);
    let currentPage = parseInt(list.dataset.currentPage ?? '1', 10);

    currentPage++;
    list.dataset.currentPage = currentPage.toString();

    const showAll = toggle?.checked ?? false;

    const spinner = document.getElementById('refresh-ideas-spinner');
    const icon    = document.getElementById('refresh-ideas-icon');
    spinner?.classList.remove('hidden');
    icon?.classList.add('hidden');

    try {
        const showAllParam = showAll ? '&showAllLanguages=true' : '';
        const url = `/${projectId}/Ideation/LoadIdeas?categoryId=${categoryId}&page=${currentPage}&sort=${encodeURIComponent(sort)}${showAllParam}`;
        const response = await fetch(url);
        if (!response.ok) return;

        const data: LoadIdeasResponse = await response.json();

        if (counter) counter.textContent = `${data.remaining}/${maxExtra}`;

        if (data.remaining <= 0) {
            btn.classList.add('btn-disabled');
            btn.setAttribute('disabled', 'true');
        }

        if (replace) {
            list.querySelectorAll(':scope > li').forEach(el => el.remove());
            if (data.ideas.length === 0) {
                list.insertAdjacentHTML('beforeend', renderEmptyState(list.dataset.emptyLabel ?? ''));
                return;
            }
        } else if (data.ideas.length === 0) {
            return;
        }

        const html = data.ideas.map(idea => renderIdeaCard(idea, projectId!, categoryId!)).join('');
        list.insertAdjacentHTML('beforeend', html);
    } catch {
        // Silently fail
    } finally {
        spinner?.classList.add('hidden');
        icon?.classList.remove('hidden');
    }
}

function renderEmptyState(label: string): string {
    return `<li class="text-center py-16 flex flex-col items-center gap-3">
        <div class="w-14 h-14 rounded-field bg-primary/10 flex items-center justify-center">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-7 w-7 text-primary/70" fill="none"
                 viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
                <path stroke-linecap="round" stroke-linejoin="round"
                      d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m1.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"/>
            </svg>
        </div>
        <p class="text-base-content/70 text-sm font-medium">${escapeHtml(label)}</p>
    </li>`;
}

function renderIdeaCard(idea: IdeaResponse, projectId: string, categoryId: string): string {
    const borderClass = idea.isFlagged
        ? 'bg-warning/5 border-warning/30 hover:border-warning/50'
        : idea.isOwnedByCurrentUser
            ? 'bg-primary/5 border-primary/20 hover:border-primary/40'
            : 'bg-base-100 border-base-300 hover:border-primary/30 hover:shadow-sm';

    let statusLabel = '';
    if (idea.isFlagged) {
        statusLabel = `<span class="inline-flex items-center gap-1 text-xs text-warning font-medium mb-2">
            <span class="w-1.5 h-1.5 rounded-full bg-warning inline-block"></span>
            ${t('ideaCardStatusFlagged')}
        </span>`;
    } else if (idea.isOwnedByCurrentUser) {
        statusLabel = `<span class="inline-flex items-center gap-1 text-xs text-primary font-medium mb-2">
            <span class="w-1.5 h-1.5 rounded-full bg-primary inline-block"></span>
            ${t('ideaCardStatusOwnIdea')}
        </span>`;
    }

    const emojiHtml = idea.emojis.map(emoji => {
        const btnClass = emoji.postedByCurrentUser
            ? 'bg-primary/10 border border-primary/30 text-primary hover:bg-primary/20'
            : 'bg-base-200 border border-base-300 text-base-content/70 hover:bg-base-300';
        return `<span class="inline-flex items-center gap-1.5 px-3 py-2 rounded-full text-xs font-semibold ${btnClass} min-h-8">
            <span class="text-base leading-none">${emoji.symbol}</span>
            <span class="leading-none font-semibold">${emoji.count}</span>
        </span>`;
    }).join('');

    // Mark the description with `lang` when the idea was written in another language (WCAG 3.1.2).
    const pageLang = currentCulture().split('-')[0];
    const langAttr = idea.languageCode && idea.languageCode !== pageLang
        ? ` lang="${idea.languageCode}"`
        : '';

    return `<li class="group relative rounded-field border p-5 transition-all duration-150 active:scale-[0.99] cursor-pointer ${borderClass}">
        <div class="absolute -top-3 -right-3 bg-base-100 border border-base-300 rounded-full px-2.5 py-1 flex items-center gap-1.5 shadow-sm">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5 text-neutral/70" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"/>
            </svg>
            <span class="text-xs font-bold text-base-content tabular-nums">${idea.commentsCount}</span>
        </div>
        ${statusLabel}
        <p class="text-base-content text-sm leading-relaxed pr-8"${langAttr}>${escapeHtml(idea.description)}</p>
        <div class="mt-3 relative z-[2]">
            <button type="button" data-action="copy-idea"
                    data-idea-description="${escapeAttr(idea.description)}"
                    class="inline-flex items-center gap-1.5 text-xs text-base-content/50 hover:text-primary transition-colors cursor-pointer">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"/>
                </svg>
                ${t('ideaCardUseAsStartingPoint')}
            </button>
        </div>
        ${emojiHtml ? `<div class="flex gap-2 mt-4 items-center flex-wrap relative z-[2]">${emojiHtml}</div>` : ''}
        <a href="/${projectId}/Ideation/Detail?categoryId=${categoryId}&ideaId=${idea.ideaId}"
           class="absolute inset-0 rounded-field z-[1] focus-visible:ring-2 focus-visible:ring-primary/50 focus-visible:outline-none"
           aria-label="${escapeAttr(idea.description)}"></a>
    </li>`;
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