import { AiSupportSessionBase, AiSupportMessage } from './ai-support-base';
import { getCsrfToken, fetchWithTimeout } from '../lib/utils';

export type { AiSupportMessage as CommentSupportMessage };

export interface CommentSupportChatResponse {
    assistantMessage: string;
    /** Optional polished version of the user's comment draft. */
    suggestion: string | null;
    /** True on the second AI turn — no further input expected. */
    isFinal: boolean;
}

/**
 * Sends a single comment-support chat turn to the MVC proxy endpoint.
 * Empty `userMessage` = opening turn (AI generates the idea summary/greeting).
 */
export async function sendCommentSupportMessage(
    projectId: number,
    history: AiSupportMessage[],
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

    if (!res.ok) throw new Error(`CommentSupport chat returned HTTP ${res.status}`);
    return res.json() as Promise<CommentSupportChatResponse>;
}

// CommentSupportSession

export class CommentSupportSession extends AiSupportSessionBase {
    private readonly _projectId:    number;
    private readonly _ideaText:     string;
    private readonly _categoryName: string;

    constructor(projectId: number, ideaText: string, categoryName: string) {
        super();
        this._projectId    = projectId;
        this._ideaText     = ideaText;
        this._categoryName = categoryName;
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

        this.appendTurn(userMessage, reply.assistantMessage);
        return reply;
    }
}
