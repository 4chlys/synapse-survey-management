// Types

export interface IdeaSupportMessage {
    role: 'user' | 'assistant';
    content: string;
}

export interface IdeaSupportChatResponse {
    assistantMessage: string;
    /** Present when the agent has generated a ready-to-submit draft idea. */
    suggestedDraft: string | null;
    /** Phase returned by the server state machine. */
    phase: 'Intro' | 'Ideation' | 'Draft' | 'Confirm' | 'Error';
}

export interface IdeaSupportRefineResponse {
    /** Null when the text could not be meaningfully improved. */
    refinedText: string | null;
}

// CSRF

function getCsrfToken(): string {
    return document.querySelector<HTMLMetaElement>(
        'meta[name="__RequestVerificationToken"]'
    )?.content ?? '';
}

// Low-level fetch with timeout

const FETCH_TIMEOUT_MS = 30_000;

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
 * Send a single chat turn to the MVC proxy endpoint.
 *
 * An empty `userMessage` is the opening turn — the server generates the
 * greeting from the system + context prompt alone.
 *
 * Throws on non-OK HTTP responses so callers can distinguish network/auth
 * failures from empty-reply situations.
 */
export async function sendIdeaSupportMessage(
    projectId: number,
    categoryId: number,
    history: IdeaSupportMessage[],
    userMessage: string,
    signal?: AbortSignal,
): Promise<IdeaSupportChatResponse> {
    const res = await fetchWithTimeout(`/${projectId}/api/IdeaSupportApi/chat`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getCsrfToken(),
        },
        body: JSON.stringify({ history, userMessage, categoryId }),
        signal,
    });

    if (!res.ok) {
        throw new Error(`IdeaSupport chat returned HTTP ${res.status}`);
    }

    return res.json() as Promise<IdeaSupportChatResponse>;
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

export class IdeaSupportSession {
    private readonly _projectId: number;
    private readonly _categoryId: number;
    private readonly _history: IdeaSupportMessage[] = [];
    private _abortController: AbortController | null = null;

    constructor(projectId: number, categoryId: number) {
        this._projectId  = projectId;
        this._categoryId = categoryId;
    }

    get history(): readonly IdeaSupportMessage[] {
        return this._history;
    }

    /**
     * Sends a turn and returns the assistant reply.
     *
     * - Empty `userMessage` → opening turn.  The server generates the
     *   greeting; we store it in history so the first real reply sees it.
     * - Non-empty `userMessage` → normal turn.  Both sides are appended.
     *
     * Throws on HTTP errors so `ideasupportchat.ts` can show the right UI.
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

        if (userMessage.trim()) {
            this._history.push({ role: 'user',      content: userMessage });
            this._history.push({ role: 'assistant', content: reply.assistantMessage });
        } else if (reply.assistantMessage) {
            this._history.push({ role: 'assistant', content: reply.assistantMessage });
        }

        return reply;
    }
    
    async refineDraft(draftText: string): Promise<IdeaSupportRefineResponse> {
        return refineIdeaDraft(this._projectId, draftText);
    }

    reset(): void {
        this._history.length = 0;
        this._abortController?.abort();
        this._abortController = null;
    }
}