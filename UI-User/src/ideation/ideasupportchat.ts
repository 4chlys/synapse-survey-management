/**
 * ideasupportchat.ts
 *
 * Drives the AI Idea Support chat overlay.
 *
 * State machine phases (driven by server):
 *   Intro    → greeting rendered, input enabled
 *   Ideation → user sends idea → AI refines with a question
 *   Draft    → AI has enough context, proposes a draft
 *   Confirm  → AI asks confirmation; input locked, YES/NO shown inline
 *   Error    → fallback message shown, input re-enabled
 */

import { IdeaSupportSession, IdeaSupportChatResponse } from './ideasupport';
import { t } from '../lib/i18n';
import { renderMarkdown, setupAutoGrow } from './ai-support-base';
import { escapeHtml } from '../lib/utils';
import { confirmDiscard } from '../lib/discard-confirm';

//  DOM references (resolved once on first open)

let overlay: HTMLElement | null = null;
let messagesContainer: HTMLElement | null = null;
let draftBanner: HTMLElement | null = null;
let draftTextEl: HTMLElement | null = null;
let inputEl: HTMLTextAreaElement | null = null;
let sendBtn: HTMLButtonElement | null = null;
let closeBtn: HTMLButtonElement | null = null;
let switchBtn: HTMLButtonElement | null = null;
let useDraftBtn: HTMLButtonElement | null = null;
let refineDraftBtn: HTMLButtonElement | null = null;
let loadingBubbleEl: HTMLElement | null = null;

//  State

let activeSession: IdeaSupportSession | null = null;
let currentDraft: string | null = null;
let isSending = false;
// Tracks whether the user has invested anything worth guarding against an
// accidental close (sent a message, or the AI has proposed a draft).
let userHasSentMessage = false;
// Guards against re-opening the discard dialog while it is already awaiting.
let confirmPending = false;
let onSubmitCallback: ((text: string) => void) | null = null;
let onSwitchToManualCallback: ((draft: string | null) => void) | null = null;
let isInitialised = false;

type ChatPhase = 'INTRO' | 'IDEATION' | 'DRAFT' | 'CONFIRM' | 'ERROR';

function ensureRefs(): boolean {
    if (isInitialised) return true;

    overlay           = document.getElementById('idea-support-overlay');
    messagesContainer = document.getElementById('idea-support-messages');
    draftBanner    = document.getElementById('idea-support-draft-banner');
    draftTextEl    = document.getElementById('idea-support-draft-text');
    inputEl        = document.getElementById('idea-support-input') as HTMLTextAreaElement | null;
    sendBtn        = document.getElementById('idea-support-btn-send') as HTMLButtonElement | null;
    closeBtn       = document.getElementById('idea-support-btn-close') as HTMLButtonElement | null;
    switchBtn      = document.getElementById('idea-support-btn-switch') as HTMLButtonElement | null;
    useDraftBtn    = document.getElementById('idea-support-btn-use-draft') as HTMLButtonElement | null;
    refineDraftBtn = document.getElementById('idea-support-btn-refine-draft') as HTMLButtonElement | null;

    if (!overlay || !messagesContainer || !inputEl || !sendBtn || !closeBtn) return false;

    closeBtn.addEventListener('click', () => { void attemptClose(); });
    switchBtn?.addEventListener('click', handleSwitchToManual);
    sendBtn.addEventListener('click', () => { void handleSend(); });
    useDraftBtn?.addEventListener('click', handleUseDraft);
    refineDraftBtn?.addEventListener('click', () => { void handleRefineDraft(); });

    // Auto-grow textarea
    setupAutoGrow(inputEl);

    // Send on Enter (Shift+Enter = new line)
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

//  Public API

/**
 * Opens the chat overlay for a specific category.
 *
 * @param projectId          – current project
 * @param categoryId         – the category the idea belongs to
 * @param _categoryName      – human-readable label (informational; context is derived server-side)
 * @param onSubmit           – called with the final idea text when the user confirms
 * @param onSwitchToManual   – called when the user clicks "Zelf schrijven"; receives the current draft (or null)
 */
export function openIdeaSupportChat(
    projectId: number,
    categoryId: number,
    _categoryName: string,
    onSubmit: (text: string) => void,
    onSwitchToManual?: (draft: string | null) => void,
): void {
    if (!ensureRefs()) return;

    // Reset state for a fresh conversation
    activeSession             = new IdeaSupportSession(projectId, categoryId);
    currentDraft              = null;
    onSubmitCallback          = onSubmit;
    onSwitchToManualCallback  = onSwitchToManual ?? null;
    isSending                 = false;
    userHasSentMessage        = false;
    confirmPending            = false;

    clearMessages();
    hideDraftBanner();
    resetInput();

    // Show overlay
    overlay!.classList.remove('hidden');
    overlay!.classList.add('flex');

    // Focus is dropped into the field once the opening turn re-enables it
    // (see setSendingState) — the field is disabled while the greeting loads,
    // so focusing here would silently no-op.
    void fireOpeningTurn();
}

//  Opening turn

async function fireOpeningTurn(): Promise<void> {
    showLoading();
    setSendingState(true);

    try {
        // Empty userMessage signals the server to generate the greeting.
        const reply = await activeSession!.send('');
        await handleAiReply(reply);
    } catch {
        hideLoading();
        setSendingState(false);
        appendAssistantBubble(t('aiChatAssistantUnavailableIdea'));
    }
}

//  Reply handler

/**
 * Drives the UI state machine based on the phase returned by the server.
 *
 * INTRO / IDEATION : show assistant message, keep input enabled.
 * DRAFT  / CONFIRM : show message + inline YES/NO card, lock input until user decides.
 * ERROR            : show fallback message, re-enable input.
 */
async function handleAiReply(reply: IdeaSupportChatResponse): Promise<void> {
    hideLoading();

    // Guard: an empty message means the backend silently failed — show the fallback.
    if (!reply.assistantMessage?.trim()) {
        setSendingState(false);
        appendAssistantBubble(t('aiChatAssistantUnavailableIdea'));
        return;
    }

    const phase = (reply.phase?.toUpperCase() ?? 'IDEATION') as ChatPhase;

    switch (phase) {
        case 'INTRO':
        case 'IDEATION':
            setSendingState(false);
            appendAssistantBubble(reply.assistantMessage);
            break;

        case 'DRAFT':
        case 'CONFIRM':
            if (reply.suggestedDraft) {
                appendAssistantBubble(reply.assistantMessage);
                showDraftConfirmation(reply.suggestedDraft);
                // Lock input — the YES button closes the overlay, NO re-enables it.
                if (inputEl) inputEl.disabled = true;
                if (sendBtn) sendBtn.disabled = true;
                isSending = true;
            } else {
                setSendingState(false);
                appendAssistantBubble(reply.assistantMessage);
            }
            break;

        case 'ERROR':
        default:
            setSendingState(false);
            appendAssistantBubble(
                reply.assistantMessage || t('aiChatError'),
            );
            break;
    }
}

//  Send handler

async function handleSend(): Promise<void> {
    if (!activeSession || isSending) return;

    const text = inputEl!.value.trim();
    if (!text) return;

    userHasSentMessage = true;
    appendUserBubble(text);
    resetInput();
    showLoading();
    setSendingState(true);

    try {
        const reply = await activeSession!.send(text);
        await handleAiReply(reply);
    } catch {
        hideLoading();
        setSendingState(false);
        appendAssistantBubble(t('aiChatError'));
    }
}

//  Draft confirmation (inline YES / NO buttons)

/**
 * Appends an inline confirmation card showing the proposed draft with
 * "Ja, indienen" (YES) and "Nee, aanpassen" (NO) buttons.
 *
 * The external banner is also updated so the user can still see the draft
 * text while they decide.
 */
function showDraftConfirmation(draft: string): void {
    currentDraft = draft;

    if (draftTextEl) draftTextEl.textContent = draft;

    const bubble = document.createElement('div');
    bubble.className = 'flex gap-2 items-end';
    bubble.setAttribute('data-draft-confirmation', 'true');

    const avatar = document.createElement('div');
    avatar.className = 'w-7 h-7 rounded-full bg-primary/15 flex items-center justify-center flex-shrink-0 self-end';
    avatar.innerHTML = `
        <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5 text-primary" fill="none"
             viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round"
                  d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"/>
        </svg>`;

    const card = document.createElement('div');
    card.className = 'bg-primary/10 border-2 border-primary/30 rounded-box rounded-bl-sm px-4 py-3 max-w-[85%] flex flex-col gap-3';

    const label = document.createElement('p');
    label.className = 'text-xs font-semibold text-primary uppercase tracking-wide';
    label.textContent = t('aiChatDraftLabel');

    const draftPara = document.createElement('p');
    draftPara.className = 'text-sm text-base-content leading-relaxed';
    draftPara.textContent = draft;

    const btnRow = document.createElement('div');
    btnRow.className = 'flex flex-col gap-2';

    const submitBtn = document.createElement('button');
    submitBtn.type = 'button';
    submitBtn.className =
        'w-full py-2 rounded-field bg-primary text-primary-content text-sm font-semibold ' +
        'hover:bg-primary/90 active:scale-95 transition-all cursor-pointer';
    submitBtn.textContent = t('aiChatDraftYes');

    const addContentBtn = document.createElement('button');
    addContentBtn.type = 'button';
    addContentBtn.className =
        'w-full py-2 rounded-field border-2 border-primary/40 text-primary text-sm font-medium ' +
        'hover:bg-primary/10 active:scale-95 transition-all cursor-pointer';
    addContentBtn.textContent = t('aiChatDraftAddContent');

    const adjustBtn = document.createElement('button');
    adjustBtn.type = 'button';
    adjustBtn.className =
        'w-full py-2 rounded-field border-2 border-base-300 text-base-content/70 text-sm font-medium ' +
        'hover:bg-base-200 active:scale-95 transition-all cursor-pointer';
    adjustBtn.textContent = t('aiChatDraftNo');

    function disableAll(): void {
        submitBtn.disabled    = true;
        addContentBtn.disabled = true;
        adjustBtn.disabled    = true;
    }

    submitBtn.addEventListener('click', () => {
        disableAll();
        handleUseDraft();
    });

    addContentBtn.addEventListener('click', () => {
        disableAll();
        handleSwitchToManual();
    });

    adjustBtn.addEventListener('click', () => {
        disableAll();
        appendAssistantBubble(t('aiChatNoProblem'));
        setSendingState(false);
    });

    btnRow.appendChild(submitBtn);
    btnRow.appendChild(addContentBtn);
    btnRow.appendChild(adjustBtn);

    card.appendChild(label);
    card.appendChild(draftPara);
    card.appendChild(btnRow);

    bubble.appendChild(avatar);
    bubble.appendChild(card);

    messagesContainer!.appendChild(bubble);
    scrollToBottom();
}

//  Draft handlers

function handleUseDraft(): void {
    // Capture locals BEFORE closeOverlay() nullifies the module-level references.
    const draft    = currentDraft;
    const callback = onSubmitCallback;
    if (!draft || !callback) return;
    closeOverlay();
    callback(draft);
}

function handleSwitchToManual(): void {
    const draft    = currentDraft;
    const callback = onSwitchToManualCallback;
    closeOverlay();
    callback?.(draft);
}

async function handleRefineDraft(): Promise<void> {
    if (!currentDraft || !activeSession) return;

    setSendingState(true);
    if (refineDraftBtn) refineDraftBtn.textContent = t('aiChatRefining');

    try {
        const result = await activeSession.refineDraft(currentDraft);
        if (result.refinedText) {
            showDraftConfirmation(result.refinedText);
            appendAssistantBubble(t('aiChatRefinedText'));
        } else {
            appendAssistantBubble(t('aiChatAlreadyGood'));
        }
    } catch {
        appendAssistantBubble(t('aiChatRefineError'));
    } finally {
        setSendingState(false);
        if (refineDraftBtn) refineDraftBtn.textContent = t('aiChatRefine');
    }
}

//  Close

/** True once there is conversation progress worth guarding. */
function hasProgress(): boolean {
    return userHasSentMessage || currentDraft !== null;
}

/**
 * Routes every "leave" affordance (backdrop, X, Escape) through a discard
 * confirmation when the user has made progress. With nothing to lose, it
 * closes instantly. "Switch to form" bypasses this — it carries the draft over.
 */
async function attemptClose(): Promise<void> {
    if (confirmPending) return;

    if (!hasProgress()) {
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
    activeSession            = null;
    onSubmitCallback         = null;
    onSwitchToManualCallback = null;
    currentDraft             = null;
    userHasSentMessage       = false;
    confirmPending           = false;
    hideLoading();
}

//  Message rendering

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
                      d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"/>
            </svg>
        </div>
        <div class="bg-base-200 rounded-box rounded-bl-sm px-4 py-2.5 max-w-[80%]">
            <p class="text-sm text-base-content leading-relaxed">${renderMarkdown(text)}</p>
        </div>`;
    messagesContainer!.appendChild(bubble);
    scrollToBottom();
}

//  Loading indicator (dynamic — always at the bottom)

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
                      d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"/>
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

//  Draft banner (external, below the chat)

function hideDraftBanner(): void {
    draftBanner?.classList.add('hidden');
    if (draftTextEl) draftTextEl.textContent = '';
    currentDraft = null;
}

//  UI helpers

function setSendingState(sending: boolean): void {
    isSending = sending;
    if (sendBtn) sendBtn.disabled  = sending;
    if (inputEl) inputEl.disabled  = sending;

    // When the field becomes usable again (opening greeting done, or an AI reply
    // arrived) drop focus back into it so the user can keep typing.
    if (!sending && overlay && !overlay.classList.contains('hidden')) {
        requestAnimationFrame(() => inputEl?.focus());
    }
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

