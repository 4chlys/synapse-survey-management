import { QuestionSupportSession } from '../lib/questionsupport';

// DOM references
let messagesContainer: HTMLElement        | null = null;
let inputEl:           HTMLTextAreaElement | null = null;
let sendBtn:           HTMLButtonElement  | null = null;
let chatPanel:         HTMLElement        | null = null;
let loadingBubbleEl:   HTMLElement        | null = null;

// State
let activeSession: QuestionSupportSession | null = null;
let isSending = false;
let projectId = 0;
let isInitialised = false;

// Bootstrap

export function initQuestionSupportChat(pid: number): void {
    projectId = pid;
    if (!ensureRefs()) return;

    const questionsListEl = document.getElementById('questions-list');
    const mode = questionsListEl?.dataset['aiAssistance'] ?? 'Never';

    if (mode === 'Never') return;

    applyMode(mode);
}

function ensureRefs(): boolean {
    if (isInitialised) return true;

    messagesContainer = document.getElementById('qs-messages');
    inputEl           = document.getElementById('qs-input') as HTMLTextAreaElement | null;
    sendBtn           = document.getElementById('qs-btn-send') as HTMLButtonElement | null;
    chatPanel         = document.getElementById('qs-chat-panel');

    if (!messagesContainer || !inputEl || !sendBtn || !chatPanel) return false;

    sendBtn.addEventListener('click', () => { void handleSend(); });

    inputEl.addEventListener('input', () => {
        inputEl!.style.height = 'auto';
        inputEl!.style.height = `${Math.min(inputEl!.scrollHeight, 128)}px`;
    });

    inputEl.addEventListener('keydown', (e: KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            void handleSend();
        }
    });

    isInitialised = true;
    return true;
}

function applyMode(mode: string): void {
    activeSession = new QuestionSupportSession(projectId);

    if (mode === 'ProActive') {
        void autoOpenAndGreet();
    }
    // AskFirst: session ready, toggle button visible — user clicks to open
}

// ProActive: auto-open popup and send opening turn

async function autoOpenAndGreet(): Promise<void> {
    const popup = document.getElementById('qs-chat-popup');
    const iconOpen = document.getElementById('qs-chat-icon-open');
    const iconClose = document.getElementById('qs-chat-icon-close');
    if (popup) {
        popup.classList.remove('hidden');
        popup.classList.add('flex');
        iconOpen?.classList.add('hidden');
        iconClose?.classList.remove('hidden');
    }

    if (!activeSession) return;
    showLoading();
    setSendingState(true);

    try {
        const reply = await activeSession.send('', true);
        hideLoading();
        setSendingState(false);
        appendAssistantBubble(reply.assistantMessage);
    } catch {
        hideLoading();
        setSendingState(false);
        appendAssistantBubble(
            'Sorry, de assistent is even niet beschikbaar. Je kunt de vragen ook zonder hulp aanmaken.',
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
        appendAssistantBubble(reply.assistantMessage);
    } catch {
        hideLoading();
        setSendingState(false);
        appendAssistantBubble('Er is iets misgegaan. Probeer het opnieuw.');
    }
}

// Message rendering

function appendUserBubble(text: string): void {
    const bubble = document.createElement('div');
    bubble.className = 'flex justify-end';
    bubble.innerHTML = `
        <div class="bg-primary text-primary-content rounded-2xl rounded-br-sm px-4 py-2.5 max-w-[80%]">
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
        <div class="bg-base-200 rounded-2xl rounded-bl-sm px-4 py-2.5 max-w-[80%]">
            <p class="text-sm text-base-content leading-relaxed">${renderMarkdown(text)}</p>
        </div>`;
    messagesContainer!.appendChild(bubble);
    scrollToBottom();
}

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
        <div class="bg-base-200 rounded-2xl rounded-bl-sm px-4 py-3">
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
    loadingBubbleEl?.remove();
    loadingBubbleEl = null;
}

// Utilities

function setSendingState(sending: boolean): void {
    isSending = sending;
    if (sendBtn)  sendBtn.disabled  = sending;
    if (inputEl)  inputEl.disabled  = sending;
}

function resetInput(): void {
    if (!inputEl) return;
    inputEl.value        = '';
    inputEl.style.height = 'auto';
}

function scrollToBottom(): void {
    requestAnimationFrame(() => {
        messagesContainer!.scrollTop = messagesContainer!.scrollHeight;
    });
}

function escapeHtml(text: string): string {
    return text
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
}

function renderMarkdown(text: string): string {
    let s = escapeHtml(text);
    s = s.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
    s = s.replace(/(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)/g, '<em>$1</em>');
    s = s.replace(/`([^`]+)`/g, '<code class="bg-base-300 rounded px-1 text-xs font-mono">$1</code>');
    s = s.replace(/\n/g, '<br>');
    return s;
}

function wireFloatingToggle(): void {
    const toggle = document.getElementById('qs-chat-toggle');
    const popup = document.getElementById('qs-chat-popup');
    const closeBtn = document.getElementById('qs-chat-close');
    const iconOpen = document.getElementById('qs-chat-icon-open');
    const iconClose = document.getElementById('qs-chat-icon-close');
    if (!toggle || !popup) return;

    function setOpen(open: boolean): void {
        popup!.classList.toggle('hidden', !open);
        popup!.classList.toggle('flex', open);
        iconOpen?.classList.toggle('hidden', open);
        iconClose?.classList.toggle('hidden', !open);
    }

    toggle.addEventListener('click', () => {
        const isOpen = !popup.classList.contains('hidden');
        setOpen(!isOpen);
    });
    closeBtn?.addEventListener('click', () => setOpen(false));
}

// Auto-initialize when loaded as a Vite module
document.addEventListener('DOMContentLoaded', () => {
    wireFloatingToggle();
    const questionsListEl = document.getElementById('questions-list');
    if (!questionsListEl) return;
    const pid = parseInt(questionsListEl.dataset['projectId'] ?? '0', 10);
    if (pid) initQuestionSupportChat(pid);
});
