/**
 * commentsupportchat.ts
 *
 * Drives the AI Comment Support chat overlay.
 *
 * Two-turn flow:
 *   Turn 1 (AI) — AI summarises the idea and invites the user to write a reaction.
 *   Turn 2 (user → AI) — User submits a draft comment; AI gives direct feedback
 *                         with an optional polished suggestion.
 *   After turn 2 — submit UI is shown; the chat input is locked.
 */

import { CommentSupportSession } from './commentsupport';
import { t } from '../lib/i18n';
import { renderMarkdown, setupAutoGrow } from './ai-support-base';
import { escapeHtml } from '../lib/utils';
import { confirmDiscard } from '../lib/discard-confirm';

// DOM references

let overlay:           HTMLElement        | null = null;
let messagesContainer: HTMLElement        | null = null;
let inputEl:           HTMLTextAreaElement | null = null;
let sendBtn:           HTMLButtonElement  | null = null;
let closeBtn:          HTMLButtonElement  | null = null;
let skipBtn:           HTMLButtonElement  | null = null;
let loadingBubbleEl:   HTMLElement        | null = null;

// State

let activeSession:       CommentSupportSession | null = null;
let userDraftComment:    string | null = null;
let isSending                          = false;
// Guards against re-opening the discard dialog while it is already awaiting.
let confirmPending                     = false;
let onSubmitCallback:    ((text: string) => void) | null = null;
let onSkipCallback:      (() => void)             | null = null;
let isInitialised                      = false;

// Initialisation

function ensureRefs(): boolean {
    if (isInitialised) return true;

    overlay           = document.getElementById('comment-support-overlay');
    messagesContainer = document.getElementById('comment-support-messages');
    inputEl           = document.getElementById('comment-support-input') as HTMLTextAreaElement | null;
    sendBtn           = document.getElementById('comment-support-btn-send') as HTMLButtonElement | null;
    closeBtn          = document.getElementById('comment-support-btn-close') as HTMLButtonElement | null;
    skipBtn           = document.getElementById('comment-support-btn-skip') as HTMLButtonElement | null;

    if (!overlay || !messagesContainer || !inputEl || !sendBtn || !closeBtn) return false;

    closeBtn.addEventListener('click', () => { void attemptClose(); });
    skipBtn?.addEventListener('click', handleSkip);
    sendBtn.addEventListener('click', () => { void handleSend(); });

    setupAutoGrow(inputEl);

    inputEl.addEventListener('keydown', (e: KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            void handleSend();
        }
    });

    // Backdrop click and Escape attempt to close (guarded by a discard confirm).
    overlay.addEventListener('click', (e: MouseEvent) => {
        if (e.target === overlay) void attemptClose();
    });

    document.addEventListener('keydown', (e: KeyboardEvent) => {
        if (e.key !== 'Escape') return;
        if (!overlay || overlay.classList.contains('hidden')) return;
        if (confirmPending) return; // the discard dialog owns Escape while open
        void attemptClose();
    });

    isInitialised = true;
    return true;
}

// Public API

/**
 * Opens the comment support overlay.
 *
 * @param projectId   – current project
 * @param ideaText    – full text of the idea being commented on
 * @param categoryName – human-readable category label
 * @param onSubmit    – called with the final comment text when the user confirms
 * @param onSkip      – called when the user skips the AI and writes directly
 */
export function openCommentSupportChat(
    projectId: number,
    ideaText: string,
    categoryName: string,
    onSubmit: (text: string) => void,
    onSkip?: () => void,
): void {
    if (!ensureRefs()) return;

    activeSession     = new CommentSupportSession(projectId, ideaText, categoryName);
    userDraftComment  = null;
    onSubmitCallback  = onSubmit;
    onSkipCallback    = onSkip ?? null;
    isSending         = false;
    confirmPending    = false;

    clearMessages();
    resetInput();
    showInput(true);

    overlay!.classList.remove('hidden');
    overlay!.classList.add('flex');

    // Focus is dropped into the field once the opening turn re-enables it
    // (see setSendingState) — the field is disabled while the greeting loads,
    // so focusing here would silently no-op.
    void fireOpeningTurn();
}

// Opening turn

async function fireOpeningTurn(): Promise<void> {
    showLoading();
    setSendingState(true);

    try {
        const reply = await activeSession!.send('');
        hideLoading();
        setSendingState(false);
        appendAssistantBubble(reply.assistantMessage);
    } catch {
        hideLoading();
        setSendingState(false);
        appendAssistantBubble(t('aiChatAssistantUnavailableComment'));
    }
}

// Send handler

async function handleSend(): Promise<void> {
    if (!activeSession || isSending) return;

    const text = inputEl!.value.trim();
    if (!text) return;

    userDraftComment = text;
    appendUserBubble(text);
    resetInput();
    showInput(false); // lock input while AI responds
    showLoading();
    setSendingState(true);

    try {
        const reply = await activeSession!.send(text);
        hideLoading();
        appendAssistantBubble(reply.assistantMessage);

        if (reply.isFinal) {
            showSubmitUI(reply.suggestion);
        } else {
            setSendingState(false);
            showInput(true);
        }
    } catch {
        hideLoading();
        setSendingState(false);
        showInput(true);
        appendAssistantBubble(t('aiChatError'));
    }
}

// Submit UI — shown after the AI gives its final feedback

function showSubmitUI(suggestion: string | null): void {
    const wrapper = document.createElement('div');
    wrapper.className = 'flex flex-col gap-2 mt-1';

    if (suggestion && suggestion !== userDraftComment) {
        // Show two option cards
        const label = document.createElement('p');
        label.className = 'text-xs font-semibold text-primary uppercase tracking-wide px-1';
        label.textContent = t('aiChatChooseVersion');
        wrapper.appendChild(label);

        wrapper.appendChild(buildCommentCard(t('aiChatYourVersion'), userDraftComment ?? '', false));
        wrapper.appendChild(buildCommentCard(t('aiChatAiSuggestion'), suggestion, true));
    } else {
        // No meaningful suggestion — just show submit button for user's own text
        const card = buildCommentCard(t('aiChatYourComment'), userDraftComment ?? '', true);
        wrapper.appendChild(card);
    }

    messagesContainer!.appendChild(wrapper);
    scrollToBottom();
}

function buildCommentCard(label: string, text: string, isPrimary: boolean): HTMLElement {
    const card = document.createElement('div');
    card.className = isPrimary
        ? 'rounded-box border-2 border-primary/30 bg-primary/10 p-3 flex flex-col gap-2'
        : 'rounded-box border-2 border-base-300 bg-base-100 p-3 flex flex-col gap-2';

    const labelEl = document.createElement('p');
    labelEl.className = isPrimary
        ? 'text-xs font-semibold text-primary uppercase tracking-wide'
        : 'text-xs font-semibold text-base-content/50 uppercase tracking-wide';
    labelEl.textContent = label;

    const textEl = document.createElement('p');
    textEl.className = 'text-sm text-base-content leading-relaxed';
    textEl.textContent = text;

    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = isPrimary
        ? 'w-full py-2 rounded-field bg-primary text-primary-content text-sm font-semibold ' +
          'hover:bg-primary/90 active:scale-95 transition-all cursor-pointer'
        : 'w-full py-2 rounded-field border-2 border-base-300 text-base-content/70 text-sm font-medium ' +
          'hover:bg-base-200 active:scale-95 transition-all cursor-pointer';
    btn.textContent = t('submitFallback');

    btn.addEventListener('click', () => {
        btn.disabled = true;
        const capturedText    = text;
        const capturedCallback = onSubmitCallback;
        closeOverlay();
        capturedCallback?.(capturedText);
    });

    card.appendChild(labelEl);
    card.appendChild(textEl);
    card.appendChild(btn);
    return card;
}

// Skip handler

function handleSkip(): void {
    const callback = onSkipCallback;
    closeOverlay();
    callback?.();
}

// Close

/**
 * Routes every "leave" affordance (backdrop, X, Escape) through a discard
 * confirmation once the user has drafted a comment. With nothing to lose it
 * closes instantly. "Skip" bypasses this — it intentionally hands off to the
 * manual form.
 */
async function attemptClose(): Promise<void> {
    if (confirmPending) return;

    if (userDraftComment === null) {
        closeOverlay();
        return;
    }

    confirmPending = true;
    const discard = await confirmDiscard();
    confirmPending = false;

    if (discard) {
        closeOverlay();
    } else {
        inputEl?.focus();
    }
}

function closeOverlay(): void {
    if (!overlay) return;
    overlay.classList.add('hidden');
    overlay.classList.remove('flex');
    activeSession?.reset();
    activeSession      = null;
    onSubmitCallback   = null;
    onSkipCallback     = null;
    userDraftComment   = null;
    confirmPending     = false;
    hideLoading();
}

// Message rendering

function appendUserBubble(text: string): void {
    const bubble = document.createElement('div');
    bubble.className = 'flex justify-end';
    bubble.innerHTML = `
        <div class="bg-primary text-primary-content rounded-box rounded-br-sm px-4 py-2.5 max-w-[80%]">
            <p class="text-sm leading-relaxed">${escapeHtml(text)}</p>
        </div>`;
    messagesContainer!.appendChild(bubble);
    scrollToBottom();
}

function appendAssistantBubble(text: string): void {
    const bubble = document.createElement('div');
    bubble.className = 'flex gap-2 items-end';
    bubble.innerHTML = `
        <div class="w-7 h-7 rounded-full bg-primary/15 flex items-center justify-center flex-shrink-0 self-end">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5 text-primary" fill="none"
                 viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round"
                      d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"/>
            </svg>
        </div>
        <div class="bg-base-200 rounded-box rounded-bl-sm px-4 py-2.5 max-w-[80%]">
            <p class="text-sm text-base-content leading-relaxed">${renderMarkdown(text)}</p>
        </div>`;
    messagesContainer!.appendChild(bubble);
    scrollToBottom();
}

// Loading indicator

function showLoading(): void {
    hideLoading();
    loadingBubbleEl = document.createElement('div');
    loadingBubbleEl.className = 'flex gap-2 items-end';
    loadingBubbleEl.setAttribute('data-loading-bubble', 'true');
    loadingBubbleEl.innerHTML = `
        <div class="w-7 h-7 rounded-full bg-primary/15 flex items-center justify-center flex-shrink-0">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5 text-primary" fill="none"
                 viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round"
                      d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"/>
            </svg>
        </div>
        <div class="bg-base-200 rounded-box rounded-bl-sm px-4 py-3 max-w-[80%]">
            <div class="flex gap-1 items-center h-4">
                <span class="w-1.5 h-1.5 bg-base-content/40 rounded-full animate-bounce" style="animation-delay:0ms"></span>
                <span class="w-1.5 h-1.5 bg-base-content/40 rounded-full animate-bounce" style="animation-delay:150ms"></span>
                <span class="w-1.5 h-1.5 bg-base-content/40 rounded-full animate-bounce" style="animation-delay:300ms"></span>
            </div>
        </div>`;
    messagesContainer!.appendChild(loadingBubbleEl);
    scrollToBottom();
}

function hideLoading(): void {
    if (loadingBubbleEl) {
        loadingBubbleEl.remove();
        loadingBubbleEl = null;
    }
}

// UI helpers

function setSendingState(sending: boolean): void {
    isSending = sending;
    if (sendBtn) sendBtn.disabled = sending;
    if (inputEl) inputEl.disabled = sending;

    // When the field becomes usable again (opening greeting done, or an AI reply
    // arrived) drop focus back into it so the user can keep typing — unless the
    // discard dialog is open, which owns focus until the user answers it.
    if (!sending && !confirmPending && overlay && !overlay.classList.contains('hidden')) {
        requestAnimationFrame(() => inputEl?.focus());
    }
}

function showInput(visible: boolean): void {
    const inputArea = document.getElementById('comment-support-input-area');
    if (inputArea) {
        if (visible) {
            inputArea.classList.remove('hidden');
        } else {
            inputArea.classList.add('hidden');
        }
    }
    // Also reset send state when hiding
    if (!visible) setSendingState(false);
}

function clearMessages(): void {
    if (!messagesContainer) return;
    messagesContainer.innerHTML = '';
    loadingBubbleEl = null;
}

function resetInput(): void {
    if (!inputEl) return;
    inputEl.value        = '';
    inputEl.style.height = 'auto';
}

function scrollToBottom(): void {
    if (!messagesContainer) return;
    requestAnimationFrame(() => {
        messagesContainer!.scrollTop = messagesContainer!.scrollHeight;
    });
}

