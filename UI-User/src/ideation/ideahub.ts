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
        wireIdeationForm(createForm, 'description', projectId);
        const descriptionTextarea = createForm.querySelector<HTMLTextAreaElement>('textarea[name="description"]');
        if (descriptionTextarea) {
            wireModerationField(descriptionTextarea, projectId);
        }
    }

    // Add-comment form
    const commentFormEl = document.getElementById('commentForm');
    const commentForm = commentFormEl?.querySelector<HTMLFormElement>('form') ?? null;
    if (commentForm && projectId != null) {
        wireIdeationForm(commentForm, 'text', projectId);
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

        // Find context from the nearest data-emoji-container (reaction forms are direct children;
        // picker forms use data-idea-id to locate the container)
        const ideaId = form.dataset.ideaId
            ?? form.closest<HTMLElement>('[data-emoji-container]')?.dataset.ideaId;
        const container = ideaId
            ? document.querySelector<HTMLElement>(`[data-emoji-container][data-idea-id="${ideaId}"]`)
            : null;
        if (!container || !ideaId) return;

        const addUrl = container.dataset.addUrl!;
        const removeUrl = container.dataset.removeUrl!;
        const returnPage = container.dataset.returnPage!;
        const categoryId = container.dataset.categoryId!;
        const projectIdVal = container.dataset.projectId!;

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                headers: {'X-Requested-With': 'XMLHttpRequest'},
                body: formData,
            });
            if (!response.ok) return;

            const emojis: EmojiReaction[] = await response.json();
            const reactionsEl = document.getElementById(`emoji-reactions-${ideaId}`);
            if (reactionsEl) {
                reactionsEl.innerHTML = renderReactionButtons(
                    emojis, addUrl, removeUrl, projectIdVal, categoryId, ideaId, returnPage
                );
            }

            // Hide the picker
            const pickerId = container.dataset.pickerId;
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
    addUrl: string,
    removeUrl: string,
    projectId: string,
    categoryId: string,
    ideaId: string,
    returnPage: string
): string {
    const token = document.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]')?.value ?? '';

    return emojis.map(emoji => {
        const action = emoji.postedByCurrentUser ? removeUrl : addUrl;
        const btnClass = emoji.postedByCurrentUser
            ? 'bg-emerald-100 border-2 border-emerald-400 hover:bg-emerald-200'
            : 'bg-gray-100 border border-gray-200 hover:bg-gray-200';
        const textClass = emoji.postedByCurrentUser ? 'text-emerald-700' : 'text-gray-700';

        return `<form action="${action}" method="post" data-emoji-form>
            <input type="hidden" name="__RequestVerificationToken" value="${token}" />
            <input type="hidden" name="projectId" value="${projectId}" />
            <input type="hidden" name="categoryId" value="${categoryId}" />
            <input type="hidden" name="ideaId" value="${ideaId}" />
            <input type="hidden" name="emojiId" value="${emoji.emojiId}" />
            <input type="hidden" name="returnPage" value="${returnPage}" />
            <button type="submit"
                    class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs min-w-[3rem] leading-none transition-colors ${btnClass}">
                <span class="text-lg leading-none">${emoji.symbol}</span>
                <span class="font-semibold ${textClass} text-sm leading-none">${emoji.count}</span>
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