// Shared CSRF utility
export function getCsrfToken(): string {
    return document.querySelector<HTMLMetaElement>(
        'meta[name="__RequestVerificationToken"]'
    )?.content ?? '';
}

// Shared fetch with 30s timeout
const FETCH_TIMEOUT_MS = 30_000;

export async function fetchWithTimeout(
    url: string,
    options: RequestInit,
): Promise<Response> {
    const controller = new AbortController();
    const id = setTimeout(() => controller.abort(), FETCH_TIMEOUT_MS);
    try {
        return await fetch(url, { ...options, signal: controller.signal });
    } finally {
        clearTimeout(id);
    }
}

// Shared text utilities
export function escapeHtml(text: string): string {
    return text
        .replace(/&/g,  '&amp;')
        .replace(/</g,  '&lt;')
        .replace(/>/g,  '&gt;')
        .replace(/"/g,  '&quot;')
        .replace(/'/g,  '&#39;');
}

export function renderMarkdown(text: string): string {
    let safe = escapeHtml(text);
    safe = safe.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
    safe = safe.replace(/(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)/g, '<em>$1</em>');
    safe = safe.replace(/`([^`]+)`/g, '<code class="bg-base-300 rounded px-1 text-xs font-mono">$1</code>');
    safe = safe.replace(/\n/g, '<br>');
    return safe;
}

// Shared auto-grow textarea setup
export function setupAutoGrow(textarea: HTMLTextAreaElement): void {
    textarea.addEventListener('input', () => {
        textarea.style.height = 'auto';
        textarea.style.height = `${Math.min(textarea.scrollHeight, 128)}px`;
    });
}

// Shared message type
export interface AiSupportMessage {
    role: 'user' | 'assistant';
    content: string;
}

// Abstract-style base session class
export abstract class AiSupportSessionBase {
    protected readonly _history: AiSupportMessage[] = [];
    protected _abortController: AbortController | null = null;

    get history(): readonly AiSupportMessage[] {
        return this._history;
    }

    abstract send(userMessage: string): Promise<unknown>;

    reset(): void {
        this._history.length = 0;
        this._abortController?.abort();
        this._abortController = null;
    }

    protected appendTurn(userMessage: string, assistantMessage: string): void {
        if (userMessage.trim()) {
            this._history.push({ role: 'user',      content: userMessage });
            this._history.push({ role: 'assistant', content: assistantMessage });
        } else if (assistantMessage) {
            this._history.push({ role: 'assistant', content: assistantMessage });
        }
    }
}
