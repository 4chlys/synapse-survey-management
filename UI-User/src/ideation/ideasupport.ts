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

// Low-level API calls

/**
 * Send a chat turn.  The caller owns the history array and appends to it
 * after each successful round trip so the conversation is maintained
 * entirely on the client (stateless backend).
 */
export async function sendIdeaSupportMessage(
    projectId: number,
    categoryId: number,
    history: IdeaSupportMessage[],
    userMessage: string,
    signal?: AbortSignal,
): Promise<IdeaSupportChatResponse> {
    try {
        const res = await fetch(`/${projectId}/api/IdeaSupportApi/chat`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getCsrfToken(),
            },
            body: JSON.stringify({ history, userMessage, categoryId }),
            signal,
        });

        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res.json();
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
        const res = await fetch(`/${projectId}/api/IdeaSupportApi/refine`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getCsrfToken(),
            },
            body: JSON.stringify({ draftText }),
        });

        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res.json();
    } catch (err) {
        console.warn('[IdeaSupport] refine unreachable:', err);
        return { refinedText: null };
    }
}

// IdeaSupportSession

/**
 * Manages a single idea-support conversation session.
 *
 * Usage:
 *   const session = new IdeaSupportSession(projectId, categoryId);
 *   const reply   = await session.send('Ik wil meer groen op de speelplaats');
 *   console.log(reply.assistantMessage);
 *   if (reply.suggestedDraft) { ... }
 *
 * The session holds the full conversation history so callers don't have to.
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
     * Automatically appends both turns to the internal history.
     * Cancels any in-flight request before sending a new one.
     */
    async send(userMessage: string): Promise<IdeaSupportChatResponse> {
        this._abortController?.abort();
        this._abortController = new AbortController();

        const reply = await sendIdeaSupportMessage(
            this._projectId,
            this._categoryId,
            [...this._history],
            userMessage,
            this._abortController.signal,
        );

        // Append both turns to local history for the next call.
        this._history.push({ role: 'user',      content: userMessage });
        this._history.push({ role: 'assistant', content: reply.assistantMessage });

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