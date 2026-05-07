import { startSession, saveResponse, getNextQuestion, getResumeQuestion, submitSurvey, removeAllResponses } from './api';
import { t } from '../i18n';

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

interface QuestionEntry {
    qid: number;
    sysBubble: HTMLElement;
    ansBubble: HTMLElement | null;
}

interface Ctx {
    projectId: number;
    questions: QuestionData[];
    questionIndexById: Map<number, number>;
    existingResponses: Record<string, string>;
    hasSession: boolean;
    messagesEl: HTMLElement;
    inputRow: HTMLElement;
    inputEl: HTMLTextAreaElement;
    sendBtn: HTMLButtonElement;
    rootEl: HTMLElement;
    backBtn: HTMLButtonElement;
    restartBtn: HTMLButtonElement;
    questionStack: QuestionEntry[];
    activeChips: HTMLElement | null;
    confirmBubble: HTMLElement | null;
    openHandlers: { send: () => void; key: (e: KeyboardEvent) => void } | null;
}

const SYSTEM_BUBBLE_CLASS =
    'self-start max-w-[80%] rounded-2xl rounded-bl-sm bg-base-200 text-base-content px-4 py-3 text-sm leading-relaxed shadow-sm';
const USER_BUBBLE_CLASS =
    'self-end max-w-[80%] rounded-2xl rounded-br-sm bg-primary text-primary-content px-4 py-3 text-sm leading-relaxed shadow-sm';
const CHIP_BASE_CLASS =
    'rounded-full border border-base-300 bg-base-100 px-4 py-2 text-sm text-base-content cursor-pointer transition-all hover:border-primary hover:bg-primary/10 active:scale-95 focus:outline-none focus:border-primary';
const CHIP_SELECTED_CLASS = 'border-primary bg-primary/15';
const CHIPS_WRAPPER_CLASS =
    'flex flex-wrap gap-2 z-10 ' +
    'max-sm:fixed max-sm:bottom-[72px] max-sm:left-0 max-sm:right-0 max-sm:px-4 max-sm:py-2 max-sm:bg-base-100 max-sm:border-t max-sm:border-base-300 ' +
    'sm:self-start sm:max-w-[80%] sm:mt-1';
const MESSAGES_MOBILE_CHIP_PAD = 'max-sm:pb-28';

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
    bubble.setAttribute('aria-label', t('surveyYourAnswer').replace('{text}', text));
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
    if (ctx.openHandlers) {
        ctx.sendBtn.removeEventListener('click', ctx.openHandlers.send);
        ctx.inputEl.removeEventListener('keydown', ctx.openHandlers.key);
        ctx.openHandlers = null;
    }
}

function removeActiveChips(ctx: Ctx): void {
    if (ctx.activeChips) {
        ctx.activeChips.remove();
        ctx.activeChips = null;
    }
    ctx.messagesEl.classList.remove(MESSAGES_MOBILE_CHIP_PAD);
}

interface ChipsOptions {
    multi: boolean;
    onSelect?: (value: string) => void;
    onConfirm?: (values: string[]) => void;
}

function showChips(ctx: Ctx, items: string[], opts: ChipsOptions): void {
    removeActiveChips(ctx);

    const wrapper = document.createElement('div');
    wrapper.className = CHIPS_WRAPPER_CLASS;
    wrapper.setAttribute('role', 'group');
    wrapper.setAttribute('aria-label', t('surveyChoiceOptions'));

    ctx.activeChips = wrapper;
    ctx.messagesEl.classList.add(MESSAGES_MOBILE_CHIP_PAD);

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
                removeActiveChips(ctx);
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

        wrapper.appendChild(btn);
        return btn;
    });

    function updateConfirm(): void {
        if (selected.size > 0 && !confirmBtn) {
            confirmBtn = document.createElement('button');
            confirmBtn.type = 'button';
            confirmBtn.className = `${CHIP_BASE_CLASS} bg-primary text-primary-content border-primary hover:bg-primary/90`;
            confirmBtn.textContent = t('surveyConfirm');
            confirmBtn.addEventListener('click', () => {
                const values = Array.from(selected);
                removeActiveChips(ctx);
                opts.onConfirm?.(values);
            });
            wrapper.appendChild(confirmBtn);
        } else if (selected.size === 0 && confirmBtn) {
            confirmBtn.remove();
            confirmBtn = null;
        }
    }

    ctx.messagesEl.appendChild(wrapper);
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
    const ansBubble = appendUserBubble(ctx, displayLabel);
    const top = ctx.questionStack[ctx.questionStack.length - 1];
    if (top && top.qid === q.questionId) top.ansBubble = ansBubble;

    try {
        await ensureSession(ctx);
        await saveResponse(ctx.projectId, q.questionId, value);
    } catch {
        appendSystemBubble(ctx, t('surveySaveError'));
        return;
    }
    updateBackBtn(ctx);
    await advance(ctx, q.questionId);
}

async function advance(ctx: Ctx, currentQuestionId: number): Promise<void> {
    let nextId: number | null;
    try {
        nextId = await getNextQuestion(ctx.projectId, currentQuestionId);
    } catch {
        appendSystemBubble(ctx, t('surveyLoadError'));
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

    const sysBubble = appendSystemBubble(ctx, q.text);
    ctx.questionStack.push({ qid: q.questionId, sysBubble, ansBubble: null });
    updateBackBtn(ctx);

    const prior = ctx.existingResponses[String(q.questionId)];
    if (prior !== undefined && prior !== null && prior !== '') {
        const ansBubble = appendUserBubble(ctx, prior);
        const top = ctx.questionStack[ctx.questionStack.length - 1];
        top.ansBubble = ansBubble;
        renderQuestion(ctx, index + 1);
        return;
    }

    switch (q.format) {
        case Fmt.Open:
            removeActiveChips(ctx);
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
            appendSystemBubble(ctx, t('surveyUnknownType'));
    }
}

function bindOpenSend(ctx: Ctx, q: QuestionData): void {
    const send = (): void => {
        const value = ctx.inputEl.value.trim();
        if (!value) return;
        ctx.inputEl.disabled = true;
        ctx.sendBtn.disabled = true;
        if (ctx.openHandlers) {
            ctx.sendBtn.removeEventListener('click', ctx.openHandlers.send);
            ctx.inputEl.removeEventListener('keydown', ctx.openHandlers.key);
            ctx.openHandlers = null;
        }
        ctx.inputRow.classList.add('hidden');
        void recordAnswer(ctx, q, value, value);
    };
    const key = (e: KeyboardEvent): void => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            send();
        }
    };
    ctx.openHandlers = { send, key };
    ctx.sendBtn.addEventListener('click', send);
    ctx.inputEl.addEventListener('keydown', key);
}

function renderConfirm(ctx: Ctx): void {
    hideInput(ctx);
    ctx.confirmBubble = appendSystemBubble(ctx, t('surveyReadyToSubmit'));
    updateBackBtn(ctx);
    showChips(ctx, [t('submit')], {
        multi: false,
        onSelect: async () => {
            try {
                await submitSurvey(ctx.projectId);
            } catch {
                appendSystemBubble(ctx, t('surveySubmitError'));
                renderConfirm(ctx);
                return;
            }
            renderDone(ctx);
        },
    });
}

function renderDone(ctx: Ctx): void {
    hideInput(ctx);
    removeActiveChips(ctx);
    ctx.backBtn.disabled = true;
    appendSystemBubble(ctx, t('surveyDone'));
    setTimeout(() => {
        window.location.href = `/${ctx.projectId}/Survey/ThankYou`;
    }, 800);
}

function updateBackBtn(ctx: Ctx): void {
    const hasAnswered = ctx.questionStack.some((e) => e.ansBubble !== null);
    ctx.backBtn.disabled = !hasAnswered && ctx.confirmBubble === null;
}

function goBack(ctx: Ctx): void {
    // Confirm step: remove confirm bubble + its chips, then revisit last answered question
    if (ctx.confirmBubble) {
        ctx.confirmBubble.remove();
        ctx.confirmBubble = null;
        removeActiveChips(ctx);
    } else {
        // Pop the currently-pending (unanswered) question
        const pending = ctx.questionStack.pop();
        if (!pending) return;
        pending.sysBubble.remove();
        if (pending.ansBubble) pending.ansBubble.remove();
        removeActiveChips(ctx);
        hideInput(ctx);
    }

    // Pop the last answered entry to re-render fresh for editing
    const last = ctx.questionStack.pop();
    if (!last) {
        updateBackBtn(ctx);
        renderQuestion(ctx, 0);
        return;
    }
    last.sysBubble.remove();
    if (last.ansBubble) last.ansBubble.remove();

    delete ctx.existingResponses[String(last.qid)];
    const idx = ctx.questionIndexById.get(last.qid);
    if (idx !== undefined) {
        renderQuestion(ctx, idx);
    }
    updateBackBtn(ctx);
}

async function doRestart(ctx: Ctx): Promise<void> {
    const ok = window.confirm(t('surveyRestartConfirm'));
    if (!ok) return;
    ctx.restartBtn.disabled = true;
    try {
        if (ctx.hasSession) await removeAllResponses(ctx.projectId);
    } catch {
        ctx.restartBtn.disabled = false;
        appendSystemBubble(ctx, t('surveyRestartError'));
        return;
    }
    window.location.reload();
}

function wireToolbar(ctx: Ctx): void {
    ctx.backBtn.addEventListener('click', () => goBack(ctx));
    ctx.restartBtn.addEventListener('click', () => void doRestart(ctx));
}

async function startFlow(ctx: Ctx): Promise<void> {
    if (ctx.hasSession) {
        try {
            const resumeId = await getResumeQuestion(ctx.projectId);
            if (resumeId !== null && resumeId !== undefined) {
                const idx = ctx.questionIndexById.get(resumeId);
                if (idx !== undefined) {
                    renderQuestion(ctx, 0);
                    return;
                }
            }
        } catch {
            /* fall through */
        }
    }
    renderQuestion(ctx, 0);
}

export function initChat(): void {
    const root = document.getElementById('chat-root');
    if (!root) return;

    const messagesEl = document.getElementById('chat-messages');
    const inputRow = document.getElementById('chat-input-row');
    const inputEl = document.getElementById('chat-input') as HTMLTextAreaElement | null;
    const sendBtn = document.getElementById('chat-send') as HTMLButtonElement | null;
    const backBtn = document.getElementById('chat-back') as HTMLButtonElement | null;
    const restartBtn = document.getElementById('chat-restart') as HTMLButtonElement | null;
    if (!messagesEl || !inputRow || !inputEl || !sendBtn || !backBtn || !restartBtn) return;

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
        inputRow,
        inputEl,
        sendBtn,
        rootEl: root,
        backBtn,
        restartBtn,
        questionStack: [],
        activeChips: null,
        confirmBubble: null,
        openHandlers: null,
    };

    inputEl.addEventListener('input', () => {
        inputEl.style.height = 'auto';
        inputEl.style.height = `${Math.min(inputEl.scrollHeight, 128)}px`;
    });

    wireToolbar(ctx);
    void startFlow(ctx);
}
