// Types

export interface CommentSupportMessage {
    role: 'user' | 'assistant';
    content: string;
}

export interface CommentSupportChatResponse {
    assistantMessage: string;
    /** Optional polished version of the user's comment draft. */
    suggestion: string | null;
    /** True on the second AI turn — no further input expected. */
    isFinal: boolean;
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
 * Sends a single comment-support chat turn to the MVC proxy endpoint.
 * Empty `userMessage` = opening turn (AI generates the idea summary/greeting).
 */
export async function sendCommentSupportMessage(
    projectId: number,
    history: CommentSupportMessage[],
    userMessage: string,
    ideaText: string,
    categoryName: string,
    signal?: AbortSignal,
): Promise<CommentSupportChatResponse> {
    const res = await fetchWithTimeout(`/${projectId}/api/CommentSupportApi/chat`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getCsrfToken(),
        },
        body: JSON.stringify({ history, userMessage, ideaText, categoryName }),
        signal,
    });

    if (!res.ok) {
        throw new Error(`CommentSupport chat returned HTTP ${res.status}`);
    }

    return res.json() as Promise<CommentSupportChatResponse>;
}

// CommentSupportSession

export class CommentSupportSession {
    private readonly _projectId:    number;
    private readonly _ideaText:     string;
    private readonly _categoryName: string;
    private readonly _history:      CommentSupportMessage[] = [];
    private _abortController: AbortController | null = null;

    constructor(projectId: number, ideaText: string, categoryName: string) {
        this._projectId    = projectId;
        this._ideaText     = ideaText;
        this._categoryName = categoryName;
    }

    get history(): readonly CommentSupportMessage[] {
        return this._history;
    }

    /**
     * Sends a turn and returns the assistant reply.
     * Empty `userMessage` = opening turn.
     */
    async send(userMessage: string): Promise<CommentSupportChatResponse> {
        this._abortController?.abort();
        this._abortController = new AbortController();

        const reply = await sendCommentSupportMessage(
            this._projectId,
            [...this._history],
            userMessage,
            this._ideaText,
            this._categoryName,
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

    reset(): void {
        this._history.length = 0;
        this._abortController?.abort();
        this._abortController = null;
    }
}
