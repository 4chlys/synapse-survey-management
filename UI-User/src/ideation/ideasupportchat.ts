/**
 * ideasupportchat.ts
 *
 * Drives the AI Idea Support chat overlay.
 *
 * Flow:
 *   1. User clicks "add idea" → openIdeaSupportChat(categoryId, categoryName, onSubmit)
 *   2. Overlay appears; assistant fires opening question automatically
 *   3. User types replies; each turn goes through IdeaSupportSession.send()
 *   4. When the agent produces a [DRAFT], the draft banner appears
 *   5. "Dit indienen" → onSubmit(draftText) → caller posts to API
 *   6. "Verfijnen" → refine via session.refineDraft(), update banner
 *
 * The module is fully self-contained: it renders its own messages and manages
 * the overlay lifecycle. The caller only needs to call openIdeaSupportChat().
 */

import { IdeaSupportSession } from './ideasupport';

// DOM element references (resolved once on first call)

let overlay: HTMLElement | null = null;
let messagesContainer: HTMLElement | null = null;
let loadingBubble: HTMLElement | null = null;
let draftBanner: HTMLElement | null = null;
let draftTextEl: HTMLElement | null = null;
let inputEl: HTMLTextAreaElement | null = null;
let sendBtn: HTMLButtonElement | null = null;
let closeBtn: HTMLButtonElement | null = null;
let useDraftBtn: HTMLButtonElement | null = null;
let refineDraftBtn: HTMLButtonElement | null = null;

// State

let activeSession: IdeaSupportSession | null = null;
let currentDraft: string | null = null;
let isSending = false;
let onSubmitCallback: ((text: string) => void) | null = null;
let isInitialised = false;

// Initialise DOM refs

function ensureRefs(): boolean {
    if (isInitialised) return true;

    overlay          = document.getElementById('idea-support-overlay');
    messagesContainer = document.getElementById('idea-support-messages');
    loadingBubble    = document.getElementById('idea-support-loading');
    draftBanner      = document.getElementById('idea-support-draft-banner');
    draftTextEl      = document.getElementById('idea-support-draft-text');
    inputEl          = document.getElementById('idea-support-input') as HTMLTextAreaElement | null;
    sendBtn          = document.getElementById('idea-support-btn-send') as HTMLButtonElement | null;
    closeBtn         = document.getElementById('idea-support-btn-close') as HTMLButtonElement | null;
    useDraftBtn      = document.getElementById('idea-support-btn-use-draft') as HTMLButtonElement | null;
    refineDraftBtn   = document.getElementById('idea-support-btn-refine-draft') as HTMLButtonElement | null;

    if (!overlay || !messagesContainer || !inputEl || !sendBtn || !closeBtn) return false;

    // Wire events once
    closeBtn.addEventListener('click', closeOverlay);
    sendBtn.addEventListener('click', handleSend);
    useDraftBtn?.addEventListener('click', handleUseDraft);
    refineDraftBtn?.addEventListener('click', handleRefineDraft);

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

// Public API

/**
 * Opens the chat overlay for a specific category.
 *
 * @param projectId    — current project
 * @param categoryId   — the category the idea belongs to
 * @param categoryName — human-readable name (shown in header, sent to agent)
 * @param onSubmit     — called with the final idea text when the user confirms
 */
export function openIdeaSupportChat(
    projectId: number,
    categoryId: number,
    categoryName: string,
    onSubmit: (text: string) => void,
): void {
    if (!ensureRefs()) return;

    // Reset state
    activeSession      = new IdeaSupportSession(projectId, categoryId);
    currentDraft       = null;
    onSubmitCallback   = onSubmit;
    isSending          = false;

    // Clear previous conversation
    clearMessages();
    hideDraftBanner();
    resetInput();

    // Show overlay
    overlay!.classList.remove('hidden');
    overlay!.classList.add('flex');

    // Focus input after animation frame so the keyboard appears on mobile
    requestAnimationFrame(() => inputEl?.focus());

    // Fire the first assistant turn automatically (no user message needed)
    void fireOpeningTurn(categoryName);
}

// Opening turn

async function fireOpeningTurn(categoryName: string): Promise<void> {
    showLoading();
    setSendingState(true);

    try {
        // Send an empty opening message — the service uses IsFirstTurn + category context
        const reply = await activeSession!.send(
            `Ik wil een idee indienen in de categorie "${categoryName}".`
        );

        hideLoading();
        setSendingState(false);

        if (reply.assistantMessage) {
            appendAssistantBubble(reply.assistantMessage);
        }
        if (reply.suggestedDraft) {
            showDraft(reply.suggestedDraft);
        }
    } catch {
        hideLoading();
        setSendingState(false);
        appendAssistantBubble(
            'Sorry, de assistent is even niet beschikbaar. Je kan je idee direct intypen hieronder.'
        );
    }
}

// Send handler

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
            showDraft(reply.suggestedDraft);
        }
    } catch {
        hideLoading();
        setSendingState(false);
        appendAssistantBubble('Er is iets misgegaan. Probeer het opnieuw.');
    }
}

// Draft handlers

function handleUseDraft(): void {
    if (!currentDraft || !onSubmitCallback) return;
    closeOverlay();
    onSubmitCallback(currentDraft);
}

async function handleRefineDraft(): Promise<void> {
    if (!currentDraft || !activeSession) return;

    setSendingState(true);
    refineDraftBtn!.textContent = 'Bezig...';

    try {
        const result = await activeSession.refineDraft(currentDraft);
        if (result.refinedText) {
            showDraft(result.refinedText);
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

// Close

function closeOverlay(): void {
    if (!overlay) return;
    overlay.classList.add('hidden');
    overlay.classList.remove('flex');
    activeSession?.reset();
    activeSession       = null;
    onSubmitCallback    = null;
    currentDraft        = null;
}

// Message rendering

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
            <p class="text-sm text-gray-800 leading-relaxed">${escapeHtml(text)}</p>
        </div>`;
    messagesContainer!.appendChild(bubble);
    scrollToBottom();
}

// Loading indicator

function showLoading(): void {
    loadingBubble?.classList.remove('hidden');
    scrollToBottom();
}

function hideLoading(): void {
    loadingBubble?.classList.add('hidden');
}

// Draft banner

function showDraft(draft: string): void {
    currentDraft = draft;
    if (!draftBanner || !draftTextEl) return;
    draftTextEl.textContent = draft;
    draftBanner.classList.remove('hidden');
    scrollToBottom();
}

function hideDraftBanner(): void {
    draftBanner?.classList.add('hidden');
    if (draftTextEl) draftTextEl.textContent = '';
    currentDraft = null;
}

// UI helpers

function setSendingState(sending: boolean): void {
    isSending = sending;
    if (sendBtn) sendBtn.disabled = sending;
    if (inputEl) inputEl.disabled = sending;
}

function clearMessages(): void {
    if (!messagesContainer) return;
    // Remove all children except the loading bubble
    const children = Array.from(messagesContainer.children);
    for (const child of children) {
        if (child.id !== 'idea-support-loading') {
            child.remove();
        }
    }
}

function resetInput(): void {
    if (!inputEl) return;
    inputEl.value = '';
    inputEl.style.height = 'auto';
}

function scrollToBottom(): void {
    if (!messagesContainer) return;
    requestAnimationFrame(() => {
        messagesContainer!.scrollTop = messagesContainer!.scrollHeight;
    });
}

function escapeHtml(text: string): string {
    return text
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
        .replace(/\n/g, '<br>');
}