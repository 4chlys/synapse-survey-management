/**
 * ideasupportchat.ts
 *
 * Drives the AI Idea Support chat overlay.
 *
 * Fixes applied:
 *   1. History is maintained correctly across turns (loading bubble removed
 *      from the static DOM so it doesn't pollute the messages container;
 *      it is appended/removed dynamically instead).
 *   2. When the agent produces a suggestedDraft the chat shows an inline
 *      confirmation message with YES and NO buttons instead of (only) an
 *      external banner.
 *   3. The loading bubble is always inserted at the end of the messages
 *      list, not kept as a static top-level child.
 */

import { IdeaSupportSession } from './ideasupport';

//  DOM references (resolved once on first open)

let overlay: HTMLElement | null = null;
let messagesContainer: HTMLElement | null = null;
let draftBanner: HTMLElement | null = null;
let draftTextEl: HTMLElement | null = null;
let inputEl: HTMLTextAreaElement | null = null;
let sendBtn: HTMLButtonElement | null = null;
let closeBtn: HTMLButtonElement | null = null;
let useDraftBtn: HTMLButtonElement | null = null;
let refineDraftBtn: HTMLButtonElement | null = null;

//  State

let activeSession: IdeaSupportSession | null = null;
let currentDraft: string | null = null;
let isSending = false;
let onSubmitCallback: ((text: string) => void) | null = null;
let isInitialised = false;

// The loading bubble is created fresh each time it is needed and removed when
// hidden. We keep a reference so we can remove it cleanly.
let loadingBubbleEl: HTMLElement | null = null;

//  Initialisation

function ensureRefs(): boolean {
    if (isInitialised) return true;

    overlay           = document.getElementById('idea-support-overlay');
    messagesContainer = document.getElementById('idea-support-messages');
    // The static loading element from the HTML is no longer used – we create
    // the loading bubble dynamically. Hide the static one permanently.
    const staticLoading = document.getElementById('idea-support-loading');
    if (staticLoading) staticLoading.remove();

    draftBanner    = document.getElementById('idea-support-draft-banner');
    draftTextEl    = document.getElementById('idea-support-draft-text');
    inputEl        = document.getElementById('idea-support-input') as HTMLTextAreaElement | null;
    sendBtn        = document.getElementById('idea-support-btn-send') as HTMLButtonElement | null;
    closeBtn       = document.getElementById('idea-support-btn-close') as HTMLButtonElement | null;
    useDraftBtn    = document.getElementById('idea-support-btn-use-draft') as HTMLButtonElement | null;
    refineDraftBtn = document.getElementById('idea-support-btn-refine-draft') as HTMLButtonElement | null;

    if (!overlay || !messagesContainer || !inputEl || !sendBtn || !closeBtn) return false;

    closeBtn.addEventListener('click', closeOverlay);
    sendBtn.addEventListener('click', () => { void handleSend(); });
    useDraftBtn?.addEventListener('click', handleUseDraft);
    refineDraftBtn?.addEventListener('click', () => { void handleRefineDraft(); });

    // Auto-grow textarea
    inputEl.addEventListener('input', () => {
        inputEl!.style.height = 'auto';
        inputEl!.style.height = `${Math.min(inputEl!.scrollHeight, 128)}px`;
    });

    // Send on Enter (Shift+Enter = new line)
    inputEl.addEventListener('keydown', (e: KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            void handleSend();
        }
    });

    // Dismiss on backdrop click
    overlay.addEventListener('click', (e: MouseEvent) => {
        if (e.target === overlay) closeOverlay();
    });

    isInitialised = true;
    return true;
}

//  Public API 

/**
 * Opens the chat overlay for a specific category.
 *
 * @param projectId    – current project
 * @param categoryId   – the category the idea belongs to
 * @param categoryName – human-readable name sent to the agent for context
 * @param onSubmit     – called with the final idea text when the user confirms
 */
export function openIdeaSupportChat(
    projectId: number,
    categoryId: number,
    categoryName: string,
    onSubmit: (text: string) => void,
): void {
    if (!ensureRefs()) return;

    // Reset state for a fresh conversation
    activeSession    = new IdeaSupportSession(projectId, categoryId);
    currentDraft     = null;
    onSubmitCallback = onSubmit;
    isSending        = false;

    clearMessages();
    hideDraftBanner();
    resetInput();

    // Show overlay
    overlay!.classList.remove('hidden');
    overlay!.classList.add('flex');

    requestAnimationFrame(() => inputEl?.focus());

    void fireOpeningTurn(categoryName);
}

//  Opening turn

async function fireOpeningTurn(categoryName: string): Promise<void> {
    showLoading();
    setSendingState(true);

    try {
        const reply = await activeSession!.send('', categoryName);

        hideLoading();
        setSendingState(false);

        if (reply.assistantMessage) {
            appendAssistantBubble(reply.assistantMessage);
        }
        if (reply.suggestedDraft) {
            showDraftConfirmation(reply.suggestedDraft);
        }
    } catch {
        hideLoading();
        setSendingState(false);
        appendAssistantBubble(
            'Sorry, de assistent is even niet beschikbaar. Je kunt je idee direct hieronder intypen en indienen.',
        );
    }
}

//  Send handler

async function handleSend(): Promise<void> {
    if (!activeSession || isSending) return;

    const text = inputEl!.value.trim();
    if (!text) return;

    appendUserBubble(text);
    resetInput();
    showLoading();
    setSendingState(true);

    try {
        const reply = await activeSession.send(text);

        hideLoading();
        setSendingState(false);

        if (reply.assistantMessage) {
            appendAssistantBubble(reply.assistantMessage);
        }
        if (reply.suggestedDraft) {
            showDraftConfirmation(reply.suggestedDraft);
        }
    } catch {
        hideLoading();
        setSendingState(false);
        appendAssistantBubble('Er is iets misgegaan. Probeer het opnieuw.');
    }
}

//  Draft confirmation (inline YES / NO buttons)

/**
 * Appends an inline confirmation bubble with the proposed draft and two
 * buttons: "Ja, indienen" (YES) and "Nee, aanpassen" (NO).
 *
 * The external banner is also updated so the user can still see the draft
 * text while they decide.
 */
function showDraftConfirmation(draft: string): void {
    currentDraft = draft;

    // Update the external banner text (hidden until the user decides)
    if (draftTextEl) draftTextEl.textContent = draft;

    // Build inline confirmation bubble
    const bubble = document.createElement('div');
    bubble.className = 'flex gap-2 items-end';
    bubble.setAttribute('data-draft-confirmation', 'true');

    const avatar = document.createElement('div');
    avatar.className = 'w-7 h-7 rounded-full bg-emerald-100 flex items-center justify-center flex-shrink-0 self-end';
    avatar.innerHTML = `
        <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5 text-emerald-600" fill="none"
             viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round"
                  d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"/>
        </svg>`;

    const card = document.createElement('div');
    card.className = 'bg-emerald-50 border-2 border-emerald-200 rounded-2xl rounded-bl-sm px-4 py-3 max-w-[85%] flex flex-col gap-3';

    const label = document.createElement('p');
    label.className = 'text-xs font-semibold text-emerald-700 uppercase tracking-wide';
    label.textContent = 'Jouw idee — wil je dit indienen?';

    const draftPara = document.createElement('p');
    draftPara.className = 'text-sm text-gray-800 leading-relaxed';
    draftPara.textContent = draft;

    const btnRow = document.createElement('div');
    btnRow.className = 'flex gap-2';

    const yesBtn = document.createElement('button');
    yesBtn.type = 'button';
    yesBtn.className =
        'flex-1 py-2 rounded-xl bg-emerald-500 text-white text-sm font-semibold ' +
        'hover:bg-emerald-600 active:scale-95 transition-all cursor-pointer';
    yesBtn.textContent = 'Ja, indienen';

    const noBtn = document.createElement('button');
    noBtn.type = 'button';
    noBtn.className =
        'flex-1 py-2 rounded-xl border-2 border-gray-200 text-gray-600 text-sm font-medium ' +
        'hover:border-gray-300 hover:bg-gray-50 active:scale-95 transition-all cursor-pointer';
    noBtn.textContent = 'Nee, aanpassen';

    yesBtn.addEventListener('click', () => {
        // Disable both buttons to prevent double-tap
        yesBtn.disabled = true;
        noBtn.disabled  = true;
        handleUseDraft();
    });

    noBtn.addEventListener('click', () => {
        yesBtn.disabled = true;
        noBtn.disabled  = true;
        appendAssistantBubble('Geen probleem! Vertel me wat je wil aanpassen.');
        // Re-enable input so the user can continue the conversation
        setSendingState(false);
    });

    btnRow.appendChild(yesBtn);
    btnRow.appendChild(noBtn);

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
    if (!currentDraft || !onSubmitCallback) return;
    closeOverlay();
    onSubmitCallback(currentDraft);
}

async function handleRefineDraft(): Promise<void> {
    if (!currentDraft || !activeSession) return;

    setSendingState(true);
    if (refineDraftBtn) refineDraftBtn.textContent = 'Bezig...';

    try {
        const result = await activeSession.refineDraft(currentDraft);
        if (result.refinedText) {
            showDraftConfirmation(result.refinedText);
            appendAssistantBubble('Ik heb je idee wat aangescherpt. Kijk of dit beter is!');
        } else {
            appendAssistantBubble('Het idee is al goed verwoord. Wil je het zo indienen?');
        }
    } catch {
        appendAssistantBubble('Kon het idee niet verfijnen. Probeer het opnieuw.');
    } finally {
        setSendingState(false);
        if (refineDraftBtn) refineDraftBtn.textContent = 'Verfijnen';
    }
}

//  Close

function closeOverlay(): void {
    if (!overlay) return;
    overlay.classList.add('hidden');
    overlay.classList.remove('flex');
    activeSession?.reset();
    activeSession    = null;
    onSubmitCallback = null;
    currentDraft     = null;
    hideLoading();
}

//  Message rendering

function appendUserBubble(text: string): void {
    const bubble = document.createElement('div');
    bubble.className = 'flex justify-end';
    bubble.innerHTML = `
        <div class="bg-emerald-500 text-white rounded-2xl rounded-br-sm px-4 py-2.5 max-w-[80%]">
            <p class="text-sm leading-relaxed">${escapeHtml(text)}</p>
        </div>`;
    messagesContainer!.appendChild(bubble);
    scrollToBottom();
}

function appendAssistantBubble(text: string): void {
    const bubble = document.createElement('div');
    bubble.className = 'flex gap-2 items-end';
    bubble.innerHTML = `
        <div class="w-7 h-7 rounded-full bg-emerald-100 flex items-center justify-center flex-shrink-0 self-end">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5 text-emerald-600" fill="none"
                 viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round"
                      d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"/>
            </svg>
        </div>
        <div class="bg-gray-100 rounded-2xl rounded-bl-sm px-4 py-2.5 max-w-[80%]">
            <p class="text-sm text-gray-800 leading-relaxed">${renderMarkdown(text)}</p>
        </div>`;
    messagesContainer!.appendChild(bubble);
    scrollToBottom();
}

//  Loading indicator (dynamic — always at the bottom)

function showLoading(): void {
    // Remove any existing loading bubble first
    hideLoading();

    loadingBubbleEl = document.createElement('div');
    loadingBubbleEl.className = 'flex gap-2 items-end';
    loadingBubbleEl.setAttribute('data-loading-bubble', 'true');
    loadingBubbleEl.innerHTML = `
        <div class="w-7 h-7 rounded-full bg-emerald-100 flex items-center justify-center flex-shrink-0">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5 text-emerald-600" fill="none"
                 viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round"
                      d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"/>
            </svg>
        </div>
        <div class="bg-gray-100 rounded-2xl rounded-bl-sm px-4 py-3 max-w-[80%]">
            <div class="flex gap-1 items-center h-4">
                <span class="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style="animation-delay:0ms"></span>
                <span class="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style="animation-delay:150ms"></span>
                <span class="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style="animation-delay:300ms"></span>
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

//  Text helpers

function escapeHtml(text: string): string {
    return text
        .replace(/&/g,  '&amp;')
        .replace(/</g,  '&lt;')
        .replace(/>/g,  '&gt;')
        .replace(/"/g,  '&quot;')
        .replace(/'/g,  '&#39;');
}

function renderMarkdown(text: string): string {
    let safe = escapeHtml(text);
    safe = safe.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
    safe = safe.replace(/(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)/g, '<em>$1</em>');
    safe = safe.replace(/`([^`]+)`/g, '<code class="bg-gray-200 rounded px-1 text-xs font-mono">$1</code>');
    safe = safe.replace(/\n/g, '<br>');
    return safe;
}