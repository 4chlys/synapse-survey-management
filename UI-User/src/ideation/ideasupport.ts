// Types

export interface IdeaSupportMessage {
    role: 'user' | 'assistant';
    content: string;
}

export interface IdeaSupportChatResponse {
    assistantMessage: string;
    /** Present when the agent has generated a ready-to-submit draft idea. */
    suggestedDraft: string | null;
}

export interface IdeaSupportRefineResponse {
    /** Null when the text could not be meaningfully improved. */
    refinedText: string | null;
}

// CSRF

function getCsrfToken(): string {
    return document.querySelector<HTMLMetaElement>('meta[name="__RequestVerificationToken"]')?.content ?? '';
}

// Low-level API call with timeout

const FETCH_TIMEOUT_MS = 30_000; // 30 s — generous for cold-start AI

async function fetchWithTimeout(url: string, options: RequestInit): Promise<Response> {
    const controller = new AbortController();
    const id = setTimeout(() => controller.abort(), FETCH_TIMEOUT_MS);
    try {
        return await fetch(url, { ...options, signal: controller.signal });
    } finally {
        clearTimeout(id);
    }
}

/**
 * Send a chat turn.  The caller owns the history array and appends to it
 * after each successful round trip so the conversation is maintained
 * entirely on the client (stateless backend).
 *
 * When `userMessage` is an empty string this is treated as the opening
 * turn: the server uses only the system + context prompt and the assistant
 * fires its own greeting.  An empty message is NOT appended to history so
 * the user's first real reply starts a clean conversation from the
 * assistant's perspective.
 */
export async function sendIdeaSupportMessage(
    projectId: number,
    categoryId: number,
    history: IdeaSupportMessage[],
    userMessage: string,
    signal?: AbortSignal,
): Promise<IdeaSupportChatResponse> {
    try {
        const res = await fetchWithTimeout(`/${projectId}/api/IdeaSupportApi/chat`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getCsrfToken(),
            },
            body: JSON.stringify({ history, userMessage, categoryId }),
            signal,
        });

        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res.json() as Promise<IdeaSupportChatResponse>;
    } catch (err) {
        if ((err as Error).name === 'AbortError') throw err;
        console.warn('[IdeaSupport] chat unreachable:', err);
        return { assistantMessage: '', suggestedDraft: null };
    }
}

/**
 * Standalone draft refinement — no conversation context required.
 */
export async function refineIdeaDraft(
    projectId: number,
    draftText: string,
): Promise<IdeaSupportRefineResponse> {
    try {
        const res = await fetchWithTimeout(`/${projectId}/api/IdeaSupportApi/refine`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getCsrfToken(),
            },
            body: JSON.stringify({ draftText }),
        });

        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res.json() as Promise<IdeaSupportRefineResponse>;
    } catch (err) {
        console.warn('[IdeaSupport] refine unreachable:', err);
        return { refinedText: null };
    }
}

// IdeaSupportSession

/**
 * Manages a single idea-support conversation session.
 *
 * The session holds the full conversation history so callers don't have to.
 * An empty first userMessage triggers the assistant's opening greeting
 * without polluting the history with a synthetic user sentence.
 */
export class IdeaSupportSession {
    private readonly _projectId: number;
    private readonly _categoryId: number;
    private readonly _history: IdeaSupportMessage[] = [];
    private _abortController: AbortController | null = null;

    constructor(projectId: number, categoryId: number) {
        this._projectId  = projectId;
        this._categoryId = categoryId;
    }

    /** History of turns that have been sent/received so far. */
    get history(): readonly IdeaSupportMessage[] {
        return this._history;
    }

    /**
     * Sends a user message and returns the assistant reply.
     *
     * When `userMessage` is empty (opening turn) the message is NOT added to
     * history — only the assistant's reply is stored so the first real user
     * message starts the visible conversation.
     *
     * Cancels any in-flight request before sending a new one.
     *
     * @param userMessage  — the user's text, or '' for the opening greeting
     * @param _categoryName — unused here; kept for call-site readability
     */
    async send(userMessage: string, _categoryName?: string): Promise<IdeaSupportChatResponse> {
        this._abortController?.abort();
        this._abortController = new AbortController();

        const reply = await sendIdeaSupportMessage(
            this._projectId,
            this._categoryId,
            [...this._history],
            userMessage,
            this._abortController.signal,
        );

        // Only append real user turns to history.
        // An empty opening message should not appear in the conversation record.
        if (userMessage.trim()) {
            this._history.push({ role: 'user',      content: userMessage });
            this._history.push({ role: 'assistant', content: reply.assistantMessage });
        } else if (reply.assistantMessage) {
            // Store the opening assistant greeting so subsequent turns see it
            this._history.push({ role: 'assistant', content: reply.assistantMessage });
        }

        return reply;
    }

    /** Polishes the current draft without touching conversation history. */
    async refineDraft(draftText: string): Promise<IdeaSupportRefineResponse> {
        return refineIdeaDraft(this._projectId, draftText);
    }

    /** Resets the conversation (e.g. when the user closes and reopens the chat). */
    reset(): void {
        this._history.length = 0;
        this._abortController?.abort();
        this._abortController = null;
    }
}