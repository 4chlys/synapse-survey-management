import { wireIdeationForm } from './ideationform';
import { wireModerationField } from './moderation';
import { openIdeaSupportChat } from './ideasupportchat';

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
    const commentForm   = commentFormEl?.querySelector<HTMLFormElement>('form') ?? null;
    if (commentForm && projectId != null) {
        wireIdeationForm(commentForm, 'text', projectId, 'comment');
        const commentTextarea = commentForm.querySelector<HTMLTextAreaElement>('textarea[name="text"]');
        if (commentTextarea) {
            wireModerationField(commentTextarea, projectId);
        }
    }

    // Emoji reaction forms
    document.addEventListener('submit', async (e: Event) => {
        const form = e.target as HTMLFormElement;
        if (!form.hasAttribute('data-emoji-form')) return;

        e.preventDefault();

        const submitter = (e as SubmitEvent).submitter as HTMLButtonElement | null;
        const formData  = new FormData(form);
        if (submitter?.name) {
            formData.set(submitter.name, submitter.value);
        }

        const ideaId = form.dataset.ideaId
            ?? form.closest<HTMLElement>('[data-emoji-container]')?.dataset.ideaId;
        const container = ideaId
            ? document.querySelector<HTMLElement>(`[data-emoji-container][data-idea-id="${ideaId}"]`)
            : null;
        if (!container || !ideaId) return;

        const addUrl     = container.dataset.addUrl!;
        const removeUrl  = container.dataset.removeUrl!;
        const returnPage = container.dataset.returnPage!;
        const catId      = container.dataset.categoryId!;
        const projId     = container.dataset.projectId!;

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: formData,
            });
            if (!response.ok) return;

            const emojis: EmojiReaction[] = await response.json();
            const reactionsEl = document.getElementById(`emoji-reactions-${ideaId}`);
            if (reactionsEl) {
                reactionsEl.replaceChildren(
                    ...buildReactionButtons(emojis, addUrl, removeUrl, projId, catId, ideaId, returnPage)
                );
            }

            const pickerId = container.dataset.pickerId;
            if (pickerId) document.getElementById(pickerId)?.classList.add('hidden');
        } catch {
            form.submit();
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
                        (draftText) => submitIdeaFromChat(createForm, descriptionEl, moderationEl, draftText),
                    );
                }
                break;

            case 'show-manual-form':
                e.preventDefault();
                closeModeSelect();
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

function hidden(name: string, value: string): HTMLInputElement {
    const input  = document.createElement('input');
    input.type   = 'hidden';
    input.name   = name;
    input.value  = value;
    return input;
}

function buildReactionButtons(
    emojis: EmojiReaction[],
    addUrl: string,
    removeUrl: string,
    projectId: string,
    categoryId: string,
    ideaId: string,
    returnPage: string,
): HTMLFormElement[] {
    const token = document.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]')?.value ?? '';

    return emojis.map(emoji => {
        const action    = emoji.postedByCurrentUser ? removeUrl : addUrl;
        const btnClass  = emoji.postedByCurrentUser
            ? 'bg-emerald-100 border-2 border-emerald-400 hover:bg-emerald-200'
            : 'bg-gray-100 border border-gray-200 hover:bg-gray-200';
        const textClass = emoji.postedByCurrentUser ? 'text-emerald-700' : 'text-gray-700';

        const form  = document.createElement('form');
        form.action = action;
        form.method = 'post';
        form.setAttribute('data-emoji-form', '');

        form.appendChild(hidden('__RequestVerificationToken', token));
        form.appendChild(hidden('projectId',  projectId));
        form.appendChild(hidden('categoryId', categoryId));
        form.appendChild(hidden('ideaId',     ideaId));
        form.appendChild(hidden('emojiId',    String(emoji.emojiId)));
        form.appendChild(hidden('returnPage', returnPage));

        const btn       = document.createElement('button');
        btn.type        = 'submit';
        btn.className   = `inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs min-w-[3rem] leading-none transition-colors ${btnClass}`;

        const symbolSpan       = document.createElement('span');
        symbolSpan.className   = 'text-lg leading-none';
        symbolSpan.textContent = emoji.symbol;

        const countSpan       = document.createElement('span');
        countSpan.className   = `font-semibold ${textClass} text-sm leading-none`;
        countSpan.textContent = String(emoji.count);

        btn.appendChild(symbolSpan);
        btn.appendChild(countSpan);
        form.appendChild(btn);

        return form;
    });
}

function getProjectId(): number | null {
    const meta  = document.querySelector<HTMLMetaElement>('meta[name="project-id"]');
    if (meta?.content) return parseInt(meta.content, 10);
    const match = window.location.pathname.match(/^\/(\d+)\//);
    return match ? parseInt(match[1], 10) : null;
}