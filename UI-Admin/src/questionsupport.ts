export interface QuestionSupportMessage {
    role: 'user' | 'assistant';
    content: string;
}

export interface QuestionSupportChatResponse {
    assistantMessage: string;
}

function getCsrfToken(): string {
    return document.querySelector<HTMLInputElement>(
        'input[name="__RequestVerificationToken"]'
    )?.value ?? '';
}

async function fetchWithTimeout(url: string, options: RequestInit): Promise<Response> {
    const controller = new AbortController();
    const id = setTimeout(() => controller.abort(), 30_000);
    try {
        return await fetch(url, { ...options, signal: controller.signal });
    } finally {
        clearTimeout(id);
    }
}

export async function sendQuestionSupportMessage(
    projectId: number,
    history: QuestionSupportMessage[],
    userMessage: string,
    isFirstTurn: boolean,
    signal?: AbortSignal,
): Promise<QuestionSupportChatResponse> {
    const res = await fetchWithTimeout(
        `/api/admin/projects/${projectId}/questionsupport/chat`,
        {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getCsrfToken(),
            },
            body: JSON.stringify({ history, userMessage, isFirstTurn }),
            signal,
        },
    );
    if (!res.ok) throw new Error(`QuestionSupport chat returned HTTP ${res.status}`);
    return res.json() as Promise<QuestionSupportChatResponse>;
}

export async function updateAiAssistanceMode(projectId: number, mode: string): Promise<void> {
    const res = await fetchWithTimeout(
        `/api/admin/projects/${projectId}/questionsupport/mode`,
        {
            method: 'PATCH',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getCsrfToken(),
            },
            body: JSON.stringify(mode),
        },
    );
    if (!res.ok) throw new Error(`UpdateMode returned HTTP ${res.status}`);
}

export class QuestionSupportSession {
    private readonly _projectId: number;
    private readonly _history:   QuestionSupportMessage[] = [];
    private _abortController: AbortController | null = null;

    constructor(projectId: number) {
        this._projectId = projectId;
    }

    get history(): readonly QuestionSupportMessage[] { return this._history; }

    async send(userMessage: string, isFirstTurn = false): Promise<QuestionSupportChatResponse> {
        this._abortController?.abort();
        this._abortController = new AbortController();

        const reply = await sendQuestionSupportMessage(
            this._projectId,
            [...this._history],
            userMessage,
            isFirstTurn,
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
