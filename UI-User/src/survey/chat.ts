import { startSession, saveResponse, getNextQuestion, getResumeQuestion, submitSurvey } from './api';

const enum Fmt {
    Open = 1,
    SingleChoice = 2,
    MultipleChoice = 3,
    Likert = 4,
}

interface QuestionData {
    questionId: number;
    text: string;
    format: number;
    answerChoices: string[] | null;
    questionNumber: number;
    totalQuestions: number;
    isMandatory: boolean;
    position: number;
}

interface Ctx {
    projectId: number;
    questions: QuestionData[];
    questionIndexById: Map<number, number>;
    existingResponses: Record<string, string>;
    hasSession: boolean;
    messagesEl: HTMLElement;
    chipsRow: HTMLElement;
    inputRow: HTMLElement;
    inputEl: HTMLTextAreaElement;
    sendBtn: HTMLButtonElement;
    rootEl: HTMLElement;
}

const SYSTEM_BUBBLE_CLASS =
    'self-start max-w-[80%] rounded-2xl rounded-bl-sm bg-base-200 text-base-content px-4 py-3 text-sm leading-relaxed shadow-sm';
const USER_BUBBLE_CLASS =
    'self-end max-w-[80%] rounded-2xl rounded-br-sm bg-primary text-primary-content px-4 py-3 text-sm leading-relaxed shadow-sm';
const CHIP_BASE_CLASS =
    'rounded-full border border-base-300 bg-base-100 px-4 py-2 text-sm text-base-content cursor-pointer transition-all hover:border-primary hover:bg-primary/10 active:scale-95 focus:outline-none focus:border-primary';
const CHIP_SELECTED_CLASS = 'border-primary bg-primary/15';

function scrollToBottom(ctx: Ctx): void {
    requestAnimationFrame(() => {
        ctx.messagesEl.scrollTop = ctx.messagesEl.scrollHeight;
    });
}

function appendSystemBubble(ctx: Ctx, text: string): HTMLElement {
    const bubble = document.createElement('div');
    bubble.className = SYSTEM_BUBBLE_CLASS;
    bubble.textContent = text;
    bubble.setAttribute('tabindex', '-1');
    ctx.messagesEl.appendChild(bubble);
    scrollToBottom(ctx);
    requestAnimationFrame(() => bubble.focus({ preventScroll: true }));
    return bubble;
}

function appendUserBubble(ctx: Ctx, text: string): HTMLElement {
    const bubble = document.createElement('div');
    bubble.className = USER_BUBBLE_CLASS;
    bubble.textContent = text;
    bubble.setAttribute('aria-label', `Jouw antwoord: ${text}`);
    ctx.messagesEl.appendChild(bubble);
    scrollToBottom(ctx);
    return bubble;
}

function showInput(ctx: Ctx): void {
    ctx.inputRow.classList.remove('hidden');
    ctx.inputEl.value = '';
    ctx.inputEl.disabled = false;
    ctx.sendBtn.disabled = false;
    requestAnimationFrame(() => ctx.inputEl.focus());
}

function hideInput(ctx: Ctx): void {
    ctx.inputRow.classList.add('hidden');
}

function clearChips(ctx: Ctx): void {
    ctx.chipsRow.innerHTML = '';
    ctx.chipsRow.classList.add('hidden');
    ctx.chipsRow.classList.remove('flex');
}

interface ChipsOptions {
    multi: boolean;
    onSelect?: (value: string) => void;
    onConfirm?: (values: string[]) => void;
}

function showChips(ctx: Ctx, items: string[], opts: ChipsOptions): void {
    ctx.chipsRow.innerHTML = '';
    ctx.chipsRow.classList.remove('hidden');
    ctx.chipsRow.classList.add('flex');

    const selected = new Set<string>();
    let confirmBtn: HTMLButtonElement | null = null;

    const buttons: HTMLButtonElement[] = items.map((item) => {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = CHIP_BASE_CLASS;
        btn.textContent = item;
        btn.dataset.value = item;
        if (opts.multi) btn.setAttribute('aria-pressed', 'false');

        btn.addEventListener('click', () => {
            if (!opts.multi) {
                clearChips(ctx);
                opts.onSelect?.(item);
                return;
            }
            if (selected.has(item)) {
                selected.delete(item);
                btn.classList.remove(...CHIP_SELECTED_CLASS.split(' '));
                btn.setAttribute('aria-pressed', 'false');
            } else {
                selected.add(item);
                btn.classList.add(...CHIP_SELECTED_CLASS.split(' '));
                btn.setAttribute('aria-pressed', 'true');
            }
            updateConfirm();
        });

        ctx.chipsRow.appendChild(btn);
        return btn;
    });

    function updateConfirm(): void {
        if (selected.size > 0 && !confirmBtn) {
            confirmBtn = document.createElement('button');
            confirmBtn.type = 'button';
            confirmBtn.className = `${CHIP_BASE_CLASS} bg-primary text-primary-content border-primary hover:bg-primary/90`;
            confirmBtn.textContent = 'Bevestig';
            confirmBtn.addEventListener('click', () => {
                const values = Array.from(selected);
                clearChips(ctx);
                opts.onConfirm?.(values);
            });
            ctx.chipsRow.appendChild(confirmBtn);
        } else if (selected.size === 0 && confirmBtn) {
            confirmBtn.remove();
            confirmBtn = null;
        }
    }

    requestAnimationFrame(() => buttons[0]?.focus());
    scrollToBottom(ctx);
}

function getLikertChoices(q: QuestionData): string[] {
    if (q.answerChoices && q.answerChoices.length > 0) return q.answerChoices;
    return ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10'];
}

async function ensureSession(ctx: Ctx): Promise<void> {
    if (ctx.hasSession) return;
    await startSession(ctx.projectId);
    ctx.hasSession = true;
    ctx.rootEl.dataset.hasSession = 'true';
}

async function recordAnswer(ctx: Ctx, q: QuestionData, value: string, displayLabel: string): Promise<void> {
    appendUserBubble(ctx, displayLabel);
    try {
        await ensureSession(ctx);
        await saveResponse(ctx.projectId, q.questionId, value);
    } catch {
        appendSystemBubble(ctx, 'Er ging iets mis bij het opslaan. Probeer opnieuw.');
        return;
    }
    await advance(ctx, q.questionId);
}

async function advance(ctx: Ctx, currentQuestionId: number): Promise<void> {
    let nextId: number | null;
    try {
        nextId = await getNextQuestion(ctx.projectId, currentQuestionId);
    } catch {
        appendSystemBubble(ctx, 'Kon volgende vraag niet laden.');
        return;
    }
    if (nextId === null || nextId === undefined) {
        renderConfirm(ctx);
        return;
    }
    const idx = ctx.questionIndexById.get(nextId);
    if (idx === undefined) {
        renderConfirm(ctx);
        return;
    }
    renderQuestion(ctx, idx);
}

function renderQuestion(ctx: Ctx, index: number): void {
    const q = ctx.questions[index];
    if (!q) {
        renderConfirm(ctx);
        return;
    }

    appendSystemBubble(ctx, q.text);

    const prior = ctx.existingResponses[String(q.questionId)];
    if (prior !== undefined && prior !== null && prior !== '') {
        appendUserBubble(ctx, prior);
        // Auto-advance via local sequencing on resume — no API call needed,
        // server-side resume already pointed us at the right question.
        const nextIndex = index + 1;
        renderQuestion(ctx, nextIndex);
        return;
    }

    switch (q.format) {
        case Fmt.Open:
            hideChipsForOpen(ctx);
            showInput(ctx);
            bindOpenSend(ctx, q);
            break;
        case Fmt.SingleChoice:
            hideInput(ctx);
            showChips(ctx, q.answerChoices ?? [], {
                multi: false,
                onSelect: (value) => void recordAnswer(ctx, q, value, value),
            });
            break;
        case Fmt.MultipleChoice:
            hideInput(ctx);
            showChips(ctx, q.answerChoices ?? [], {
                multi: true,
                onConfirm: (values) => {
                    const joined = values.join(', ');
                    void recordAnswer(ctx, q, joined, joined);
                },
            });
            break;
        case Fmt.Likert: {
            hideInput(ctx);
            const choices = getLikertChoices(q);
            showChips(ctx, choices, {
                multi: false,
                onSelect: (value) => void recordAnswer(ctx, q, value, value),
            });
            break;
        }
        default:
            appendSystemBubble(ctx, 'Onbekend vraagtype.');
    }
}

function hideChipsForOpen(ctx: Ctx): void {
    clearChips(ctx);
}

function bindOpenSend(ctx: Ctx, q: QuestionData): void {
    const handler = async (): Promise<void> => {
        const value = ctx.inputEl.value.trim();
        if (!value) return;
        ctx.inputEl.disabled = true;
        ctx.sendBtn.disabled = true;
        ctx.sendBtn.removeEventListener('click', handler);
        ctx.inputEl.removeEventListener('keydown', keyHandler);
        hideInput(ctx);
        await recordAnswer(ctx, q, value, value);
    };
    const keyHandler = (e: KeyboardEvent): void => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            void handler();
        }
    };
    ctx.sendBtn.addEventListener('click', handler);
    ctx.inputEl.addEventListener('keydown', keyHandler);
}

function renderConfirm(ctx: Ctx): void {
    hideInput(ctx);
    appendSystemBubble(ctx, 'Klaar om in te sturen?');
    showChips(ctx, ['Verstuur'], {
        multi: false,
        onSelect: async () => {
            try {
                await submitSurvey(ctx.projectId);
            } catch {
                appendSystemBubble(ctx, 'Verzenden mislukt. Probeer opnieuw.');
                renderConfirm(ctx);
                return;
            }
            renderDone(ctx);
        },
    });
}

function renderDone(ctx: Ctx): void {
    hideInput(ctx);
    clearChips(ctx);
    appendSystemBubble(ctx, 'Bedankt voor je deelname!');
    setTimeout(() => {
        window.location.href = `/${ctx.projectId}/Category`;
    }, 800);
}

async function startFlow(ctx: Ctx): Promise<void> {
    if (ctx.hasSession) {
        try {
            const resumeId = await getResumeQuestion(ctx.projectId);
            if (resumeId !== null && resumeId !== undefined) {
                const idx = ctx.questionIndexById.get(resumeId);
                if (idx !== undefined) {
                    // Replay prior answers up to the resume index so user sees history.
                    for (let i = 0; i < idx; i++) {
                        const q = ctx.questions[i];
                        appendSystemBubble(ctx, q.text);
                        const prior = ctx.existingResponses[String(q.questionId)];
                        if (prior) appendUserBubble(ctx, prior);
                    }
                    renderQuestion(ctx, idx);
                    return;
                }
            }
        } catch {
            /* fall through to fresh start */
        }
    }

    renderQuestion(ctx, 0);
}

export function initChat(): void {
    const root = document.getElementById('chat-root');
    if (!root) return;

    const messagesEl = document.getElementById('chat-messages');
    const chipsRow = document.getElementById('chat-chips-row');
    const inputRow = document.getElementById('chat-input-row');
    const inputEl = document.getElementById('chat-input') as HTMLTextAreaElement | null;
    const sendBtn = document.getElementById('chat-send') as HTMLButtonElement | null;
    if (!messagesEl || !chipsRow || !inputRow || !inputEl || !sendBtn) return;

    const projectId = parseInt(root.dataset.projectId ?? '0', 10);
    const hasSession = root.dataset.hasSession === 'true';

    const questions = JSON.parse(root.dataset.questions ?? '[]') as QuestionData[];
    const existingResponses = JSON.parse(root.dataset.existingResponses ?? '{}') as Record<string, string>;

    const questionIndexById = new Map<number, number>();
    questions.forEach((q, i) => questionIndexById.set(q.questionId, i));

    const ctx: Ctx = {
        projectId,
        questions,
        questionIndexById,
        existingResponses,
        hasSession,
        messagesEl,
        chipsRow,
        inputRow,
        inputEl,
        sendBtn,
        rootEl: root,
    };

    inputEl.addEventListener('input', () => {
        inputEl.style.height = 'auto';
        inputEl.style.height = `${Math.min(inputEl.scrollHeight, 128)}px`;
    });

    void startFlow(ctx);
}
