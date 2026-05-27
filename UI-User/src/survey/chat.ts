import { startSession, saveResponse, getNextQuestion, getResumeQuestion, submitSurvey, removeAllResponses } from './api';
import { t } from '../lib/i18n';
import type { QuestionData, BubbleMedia, ChipsOptions, Ctx } from './types';

const enum Fmt {
    Open = 1,
    SingleChoice = 2,
    MultipleChoice = 3,
    Likert = 4,
}

// ─── Visual class constants ──────────────────────────────
const SYSTEM_BUBBLE_CLASS =
    'self-start max-w-[80%] rounded-box rounded-tl-none ' +
    'bg-base-100 border border-base-300/60 ' +
    'text-base-content px-4 py-3 text-sm leading-relaxed shadow-sm chat-bubble-left';

const USER_BUBBLE_CLASS =
    'self-end max-w-[80%] rounded-box rounded-tr-none ' +
    'bg-primary text-primary-content px-4 py-3 text-sm leading-relaxed ' +
    'shadow-lg shadow-primary/20 chat-bubble-right';

const TYPING_BUBBLE_CLASS =
    'self-start rounded-box rounded-tl-none ' +
    'bg-base-100 border border-base-300/60 ' +
    'px-4 py-3.5 text-base-content/50 shadow-sm chat-bubble-left flex gap-1.5 items-center';

const USER_TYPING_BUBBLE_CLASS =
    'self-end rounded-box rounded-tr-none ' +
    'bg-primary text-primary-content/60 ' +
    'px-4 py-3.5 shadow-lg shadow-primary/20 chat-bubble-right flex gap-1.5 items-center';

const CHIP_BASE_CLASS =
    'group relative inline-flex items-center justify-center gap-2 rounded-field px-4 py-2.5 text-sm font-medium ' +
    'text-primary cursor-pointer transition-all duration-200 ' +
    'bg-gradient-to-br from-primary/5 to-primary/10 ' +
    'border-2 border-dashed border-primary/40 ' +
    'hover:border-solid hover:border-primary hover:from-primary hover:to-primary/90 hover:text-primary-content ' +
    'hover:shadow-lg hover:shadow-primary/25 hover:-translate-y-0.5 ' +
    'active:translate-y-0 active:scale-95 ' +
    'focus:outline-none focus-visible:ring-2 focus-visible:ring-primary/40 chat-chip-enter';

const CHIP_SELECTED_CLASS =
    'border-solid border-primary from-primary/20 to-primary/25';

// ─── Helpers ─────────────────────────────────────────────
function scrollToBottom(ctx: Ctx): void {
    requestAnimationFrame(() => {
        ctx.messagesEl.scrollTop = ctx.messagesEl.scrollHeight;
    });
}

function updateProgress(q: QuestionData): void {
    const fill = document.getElementById('chat-progress-fill');
    if (!fill || q.totalQuestions <= 0) return;
    const pct = Math.round(((q.questionNumber - 1) / q.totalQuestions) * 100);
    fill.style.width = `${pct}%`;
}

function setProgressTo(pct: number): void {
    const fill = document.getElementById('chat-progress-fill');
    if (fill) fill.style.width = `${pct}%`;
}

function showUserTyping(ctx: Ctx): void {
    if (ctx.userTypingBubble) return;
    const bubble = document.createElement('div');
    bubble.className = USER_TYPING_BUBBLE_CLASS;
    bubble.innerHTML =
        '<span class="typing-dot" aria-hidden="true"></span>' +
        '<span class="typing-dot" aria-hidden="true"></span>' +
        '<span class="typing-dot" aria-hidden="true"></span>';
    ctx.messagesEl.appendChild(bubble);
    ctx.userTypingBubble = bubble;
    scrollToBottom(ctx);
}

function removeUserTyping(ctx: Ctx): void {
    if (ctx.userTypingBubble) {
        ctx.userTypingBubble.remove();
        ctx.userTypingBubble = null;
    }
}

// Async: shows typing indicator, then resolves with the real bubble element
async function appendSystemBubble(ctx: Ctx, text: string, withTyping = true, media?: BubbleMedia): Promise<HTMLElement> {
    if (withTyping) {
        const typing = document.createElement('div');
        typing.className = TYPING_BUBBLE_CLASS;
        typing.innerHTML =
            '<span class="typing-dot" aria-hidden="true"></span>' +
            '<span class="typing-dot" aria-hidden="true"></span>' +
            '<span class="typing-dot" aria-hidden="true"></span>';
        ctx.messagesEl.appendChild(typing);
        scrollToBottom(ctx);
        await new Promise<void>(r => setTimeout(r, 460));
        typing.remove();
    }

    const bubble = document.createElement('div');
    bubble.className = SYSTEM_BUBBLE_CLASS;
    bubble.setAttribute('tabindex', '-1');

    if (media?.imageUrl) {
        const img = document.createElement('img');
        img.src = media.imageUrl;
        img.alt = '';
        img.className = 'rounded-field max-h-56 w-full object-contain bg-base-200 mb-2';
        img.addEventListener('load', () => scrollToBottom(ctx));
        bubble.appendChild(img);
    }
    if (media?.videoUrl) {
        if (media.isEmbed && (media.videoUrl.includes('youtube.com/embed') || media.videoUrl.includes('player.vimeo.com'))) {
            const wrap = document.createElement('div');
            wrap.className = 'aspect-video rounded-field overflow-hidden bg-black mb-2';
            const iframe = document.createElement('iframe');
            iframe.src = media.videoUrl;
            iframe.className = 'w-full h-full';
            iframe.setAttribute('sandbox', 'allow-scripts allow-same-origin allow-presentation');
            iframe.setAttribute('allow', 'accelerometer; encrypted-media; picture-in-picture');
            iframe.loading = 'lazy';
            wrap.appendChild(iframe);
            bubble.appendChild(wrap);
        } else {
            const video = document.createElement('video');
            video.controls = true;
            video.preload = 'metadata';
            video.src = media.videoUrl;
            video.className = 'rounded-field max-h-56 w-full bg-black mb-2';
            bubble.appendChild(video);
        }
    }
    if (media?.audioUrl) {
        const audio = document.createElement('audio');
        audio.controls = true;
        audio.preload = 'metadata';
        audio.src = media.audioUrl;
        audio.className = 'w-full mb-2';
        bubble.appendChild(audio);
    }
    if (media?.extraText) {
        const sub = document.createElement('div');
        sub.className = 'text-xs text-base-content/70 mb-1';
        sub.textContent = media.extraText;
        bubble.appendChild(sub);
    }

    const textEl = document.createElement('div');
    // Use innerHTML when the caller signals HTML content (e.g. intro from Quill editor),
    // otherwise fall back to textContent to prevent XSS from untrusted strings.
    if (media?.isHtml)
        textEl.innerHTML = text;
    else
        textEl.textContent = text;
    bubble.appendChild(textEl);

    ctx.messagesEl.appendChild(bubble);
    scrollToBottom(ctx);
    requestAnimationFrame(() => bubble.focus({ preventScroll: true }));
    return bubble;
}

function appendUserBubble(ctx: Ctx, text: string, sourceEl?: HTMLElement | null): HTMLElement {
    const bubble = document.createElement('div');
    bubble.className = USER_BUBBLE_CLASS;
    bubble.textContent = text;
    bubble.setAttribute('aria-label', t('surveyYourAnswer').replace('{text}', text));
    ctx.messagesEl.appendChild(bubble);
    scrollToBottom(ctx);
    if (sourceEl) flyToBubble(bubble, sourceEl);
    return bubble;
}

function flyToBubble(bubble: HTMLElement, sourceEl: HTMLElement): void {
    if (typeof bubble.animate !== 'function') return;
    const src = sourceEl.getBoundingClientRect();
    const dst = bubble.getBoundingClientRect();
    if (src.width === 0 || dst.width === 0) return;
    const dx = src.left + src.width / 2 - (dst.left + dst.width / 2);
    const dy = src.top + src.height / 2 - (dst.top + dst.height / 2);
    const sx = Math.max(0.2, Math.min(2, src.width / dst.width));
    const sy = Math.max(0.2, Math.min(2, src.height / dst.height));
    bubble.animate(
        [
            { transform: `translate(${dx}px, ${dy}px) scale(${sx}, ${sy})`, opacity: 0.35, borderRadius: '9999px' },
            { transform: 'translate(0, 0) scale(1, 1)', opacity: 1 },
        ],
        { duration: 420, easing: 'cubic-bezier(0.22, 1, 0.36, 1)', fill: 'backwards' },
    );
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
    removeUserTyping(ctx);
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
}

function showChips(ctx: Ctx, items: string[], opts: ChipsOptions): void {
    removeActiveChips(ctx);

    const wrapper = document.createElement('div');
    wrapper.className = 'flex flex-col gap-3 w-full';

    const chipsRow = document.createElement('div');
    chipsRow.className = 'flex flex-wrap gap-2 justify-center sm:justify-start';
    chipsRow.setAttribute('role', 'group');
    chipsRow.setAttribute('aria-label', t('surveyChoiceOptions'));

    const confirmRow = document.createElement('div');
    confirmRow.className = 'flex justify-center sm:justify-end';

    wrapper.append(chipsRow, confirmRow);

    ctx.activeChips = wrapper;

    const selected = new Set<string>();
    let confirmBtn: HTMLButtonElement | null = null;

    const buttons: HTMLButtonElement[] = items.map((item, i) => {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = CHIP_BASE_CLASS;
        btn.textContent = item;
        btn.dataset.value = item;
        // Stagger entrance animation
        btn.style.animationDelay = `${i * 40}ms`;
        if (opts.multi) btn.setAttribute('aria-pressed', 'false');

        btn.addEventListener('click', () => {
            if (!opts.multi) {
                buttons.forEach(b => { b.disabled = true; b.style.pointerEvents = 'none'; });
                btn.classList.add(...CHIP_SELECTED_CLASS.split(' '));
                btn.setAttribute('aria-pressed', 'true');
                const r = btn.getBoundingClientRect();
                const sourceRect = btn.cloneNode(true) as HTMLElement;
                sourceRect.classList.remove('chat-chip-enter');
                Object.assign(sourceRect.style, {
                    position: 'fixed', left: `${r.left}px`, top: `${r.top}px`,
                    width: `${r.width}px`, height: `${r.height}px`, margin: '0', pointerEvents: 'none', zIndex: '50', opacity: '0', visibility: 'hidden',
                });
                document.body.appendChild(sourceRect);
                removeActiveChips(ctx);
                opts.onSelect?.(item, sourceRect);
                setTimeout(() => sourceRect.remove(), 500);
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

        chipsRow.appendChild(btn);
        return btn;
    });

    function updateConfirm(): void {
        if (selected.size > 0 && !confirmBtn) {
            confirmBtn = document.createElement('button');
            confirmBtn.type = 'button';
            confirmBtn.className =
                'inline-flex items-center gap-2 rounded-full bg-primary text-primary-content ' +
                'px-6 py-2.5 text-sm font-semibold shadow-md shadow-primary/25 ' +
                'hover:bg-primary/90 hover:shadow-lg hover:shadow-primary/30 hover:scale-105 ' +
                'active:scale-95 transition-all cursor-pointer chat-chip-enter';
            confirmBtn.innerHTML =
                `<span>${t('surveyConfirm')}</span>` +
                '<svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="3"><path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7"/></svg>';
            confirmBtn.addEventListener('click', () => {
                const values = Array.from(selected);
                const cb = confirmBtn!;
                cb.disabled = true;
                cb.style.pointerEvents = 'none';
                cb.classList.add('opacity-90', 'scale-95');
                buttons.forEach(b => { b.disabled = true; b.style.pointerEvents = 'none'; });
                const r = cb.getBoundingClientRect();
                const ghost = cb.cloneNode(true) as HTMLElement;
                ghost.classList.remove('chat-chip-enter');
                Object.assign(ghost.style, {
                    position: 'fixed', left: `${r.left}px`, top: `${r.top}px`,
                    width: `${r.width}px`, height: `${r.height}px`, margin: '0', pointerEvents: 'none', zIndex: '50', opacity: '0', visibility: 'hidden',
                });
                document.body.appendChild(ghost);
                removeActiveChips(ctx);
                opts.onConfirm?.(values, ghost);
                setTimeout(() => ghost.remove(), 500);
            });
            confirmRow.appendChild(confirmBtn);
        } else if (selected.size === 0 && confirmBtn) {
            confirmBtn.remove();
            confirmBtn = null;
        }
    }

    ctx.optionsRow.appendChild(wrapper);
    requestAnimationFrame(() => {
        let max = 0;
        buttons.forEach(b => { if (b.offsetWidth > max) max = b.offsetWidth; });
        if (max > 0) buttons.forEach(b => { b.style.minWidth = `${max}px`; });
        buttons[0]?.focus();
    });
    scrollToBottom(ctx);
}

function showLikertSlider(ctx: Ctx, items: string[], onSelect: (value: string, sourceEl?: HTMLElement) => void): void {
    removeActiveChips(ctx);
    if (items.length === 0) return;

    const wrapper = document.createElement('div');
    wrapper.className = 'flex flex-col gap-3 w-full';
    wrapper.setAttribute('role', 'group');
    wrapper.setAttribute('aria-label', t('surveyChoiceOptions'));

    ctx.activeChips = wrapper;

    const max = items.length - 1;
    const initial = Math.floor(max / 2);

    const valueLabel = document.createElement('div');
    valueLabel.className = 'text-center text-sm font-semibold text-primary';
    valueLabel.textContent = items[initial];

    const slider = document.createElement('input');
    slider.type = 'range';
    slider.min = '0';
    slider.max = String(max);
    slider.step = '1';
    slider.value = String(initial);
    slider.className = 'w-full range range-primary range-sm cursor-pointer';
    slider.setAttribute('aria-label', t('surveyChoiceOptions'));

    const endpoints = document.createElement('div');
    endpoints.className = 'flex justify-between text-[11px] text-base-content/50 px-1';
    const minSpan = document.createElement('span');
    minSpan.textContent = items[0];
    const maxSpan = document.createElement('span');
    maxSpan.textContent = items[max];
    endpoints.append(minSpan, maxSpan);

    slider.addEventListener('input', () => {
        const idx = Number(slider.value);
        valueLabel.textContent = items[idx];
    });

    const confirmBtn = document.createElement('button');
    confirmBtn.type = 'button';
    confirmBtn.className =
        'self-center rounded-full bg-primary text-primary-content px-5 py-2 text-sm font-semibold ' +
        'hover:bg-primary/90 active:scale-95 transition-all cursor-pointer shadow-sm shadow-primary/20';
    confirmBtn.textContent = t('surveyConfirm');
    confirmBtn.addEventListener('click', () => {
        const idx = Number(slider.value);
        const value = items[idx];
        confirmBtn.disabled = true;
        confirmBtn.style.pointerEvents = 'none';
        confirmBtn.classList.add('opacity-90', 'scale-95');
        slider.disabled = true;
        const r = confirmBtn.getBoundingClientRect();
        const ghost = confirmBtn.cloneNode(true) as HTMLElement;
        ghost.classList.remove('chat-chip-enter');
        Object.assign(ghost.style, {
            position: 'fixed', left: `${r.left}px`, top: `${r.top}px`,
            width: `${r.width}px`, height: `${r.height}px`, margin: '0', pointerEvents: 'none', zIndex: '50', opacity: '0', visibility: 'hidden',
        });
        document.body.appendChild(ghost);
        removeActiveChips(ctx);
        onSelect(value, ghost);
        setTimeout(() => ghost.remove(), 500);
    });

    wrapper.append(valueLabel, slider, endpoints, confirmBtn);
    ctx.optionsRow.appendChild(wrapper);
    requestAnimationFrame(() => slider.focus());
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

async function recordAnswer(ctx: Ctx, q: QuestionData, value: string, displayLabel: string, sourceEl?: HTMLElement | null): Promise<void> {
    const ansBubble = appendUserBubble(ctx, displayLabel, sourceEl);
    const top = ctx.questionStack[ctx.questionStack.length - 1];
    if (top && top.qid === q.questionId) top.ansBubble = ansBubble;

    try {
        await ensureSession(ctx);
        await saveResponse(ctx.projectId, q.questionId, value);
    } catch {
        appendSystemBubble(ctx, t('surveySaveError'), false);
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
        appendSystemBubble(ctx, t('surveyLoadError'), false);
        return;
    }
    if (nextId === null || nextId === undefined) {
        await renderConfirm(ctx);
        return;
    }
    const idx = ctx.questionIndexById.get(nextId);
    if (idx === undefined) {
        await renderConfirm(ctx);
        return;
    }
    await renderQuestion(ctx, idx);
}

async function renderQuestion(ctx: Ctx, index: number, withTyping = true): Promise<void> {
    const q = ctx.questions[index];
    if (!q) {
        await renderConfirm(ctx);
        return;
    }

    updateProgress(q);

    const media: BubbleMedia | undefined = q.mediaType
        ? {
            imageUrl: q.mediaType === 1 ? (q.mediaUrl ?? undefined) : undefined,
            videoUrl: q.mediaType === 2 ? (q.mediaUrl ?? undefined) : undefined,
            audioUrl: q.mediaType === 3 ? (q.mediaUrl ?? undefined) : undefined,
            isEmbed: q.mediaIsEmbed,
            extraText: q.mediaText ?? undefined,
        }
        : (q.mediaText ? { extraText: q.mediaText } : undefined);
    const sysBubble = await appendSystemBubble(ctx, q.text, withTyping, media);
    ctx.questionStack.push({ qid: q.questionId, sysBubble, ansBubble: null });
    updateBackBtn(ctx);

    const prior = ctx.existingResponses[String(q.questionId)];
    if (prior !== undefined && prior !== null && prior !== '') {
        const ansBubble = appendUserBubble(ctx, prior);
        const top = ctx.questionStack[ctx.questionStack.length - 1];
        top.ansBubble = ansBubble;

        // Follow the backend's branching path during replay so questions that
        // were skipped (because of a different earlier answer) are not shown again.
        let nextId: number | null;
        try {
            nextId = await getNextQuestion(ctx.projectId, q.questionId);
        } catch {
            await renderConfirm(ctx);
            return;
        }
        if (nextId === null || nextId === undefined) {
            await renderConfirm(ctx);
            return;
        }
        const nextIdx = ctx.questionIndexById.get(nextId);
        if (nextIdx === undefined) {
            await renderConfirm(ctx);
            return;
        }
        await renderQuestion(ctx, nextIdx, false);
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
                onSelect: (value, src) => void recordAnswer(ctx, q, value, value, src),
            });
            break;
        case Fmt.MultipleChoice:
            hideInput(ctx);
            showChips(ctx, q.answerChoices ?? [], {
                multi: true,
                onConfirm: (values, src) => {
                    const joined = values.join(', ');
                    void recordAnswer(ctx, q, joined, joined, src);
                },
            });
            break;
        case Fmt.Likert: {
            hideInput(ctx);
            const choices = getLikertChoices(q);
            showLikertSlider(ctx, choices, (value, src) => void recordAnswer(ctx, q, value, value, src));
            break;
        }
        default:
            appendSystemBubble(ctx, t('surveyUnknownType'), false);
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
        const inputRect = ctx.inputEl.getBoundingClientRect();
        const ghost = document.createElement('div');
        ghost.textContent = value;
        Object.assign(ghost.style, {
            position: 'fixed', left: `${inputRect.left}px`, top: `${inputRect.top}px`,
            width: `${inputRect.width}px`, height: `${inputRect.height}px`, margin: '0',
            pointerEvents: 'none', zIndex: '50', opacity: '0',
        });
        document.body.appendChild(ghost);
        ctx.inputRow.classList.add('hidden');
        removeUserTyping(ctx);
        void recordAnswer(ctx, q, value, value, ghost);
        setTimeout(() => ghost.remove(), 500);
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

async function renderConfirm(ctx: Ctx): Promise<void> {
    hideInput(ctx);
    setProgressTo(95);
    ctx.confirmBubble = await appendSystemBubble(ctx, t('surveyReadyToSubmit'));
    updateBackBtn(ctx);
    showChips(ctx, [t('submit')], {
        multi: false,
        onSelect: async () => {
            try {
                await submitSurvey(ctx.projectId);
            } catch {
                await appendSystemBubble(ctx, t('surveySubmitError'), false);
                await renderConfirm(ctx);
                return;
            }
            renderDone(ctx);
        },
    });
}

function renderDone(ctx: Ctx): void {
    hideInput(ctx);
    removeActiveChips(ctx);
    setProgressTo(100);
    ctx.backBtns.forEach(b => (b.disabled = true));
    appendSystemBubble(ctx, t('surveyDone'), false);
    setTimeout(() => {
        const popup = document.getElementById('thank-you-popup');
        if (popup) {
            popup.classList.remove('hidden');
            popup.classList.add('flex');
        }
    }, 1500);
}

function updateBackBtn(ctx: Ctx): void {
    const hasAnswered = ctx.questionStack.some((e) => e.ansBubble !== null);
    const disabled = !hasAnswered && ctx.confirmBubble === null;
    ctx.backBtns.forEach(b => (b.disabled = disabled));
}

async function goBack(ctx: Ctx): Promise<void> {
    if (ctx.confirmBubble) {
        ctx.confirmBubble.remove();
        ctx.confirmBubble = null;
        removeActiveChips(ctx);
    } else {
        const pending = ctx.questionStack.pop();
        if (!pending) return;
        pending.sysBubble.remove();
        if (pending.ansBubble) pending.ansBubble.remove();
        removeActiveChips(ctx);
        hideInput(ctx);
    }

    const last = ctx.questionStack.pop();
    if (!last) {
        updateBackBtn(ctx);
        await renderQuestion(ctx, 0, false);
        return;
    }
    last.sysBubble.remove();
    if (last.ansBubble) last.ansBubble.remove();

    delete ctx.existingResponses[String(last.qid)];
    const idx = ctx.questionIndexById.get(last.qid);
    if (idx !== undefined) {
        // No typing indicator on back — feels snappier
        await renderQuestion(ctx, idx, false);
    }
    updateBackBtn(ctx);
}

function wireToolbar(ctx: Ctx): void {
    ctx.backBtns.forEach(b => b.addEventListener('click', () => void goBack(ctx)));
    ctx.restartBtns.forEach(b => b.addEventListener('click', () => void doRestart(ctx)));
}

async function doRestart(ctx: Ctx): Promise<void> {
    const ok = window.confirm(t('surveyRestartConfirm'));
    if (!ok) return;
    ctx.restartBtns.forEach(b => (b.disabled = true));
    try {
        if (ctx.hasSession) await removeAllResponses(ctx.projectId);
    } catch {
        ctx.restartBtns.forEach(b => (b.disabled = false));
        appendSystemBubble(ctx, t('surveyRestartError'), false);
        return;
    }
    window.location.reload();
}

async function showIntro(ctx: Ctx): Promise<void> {
    if (ctx.introShown) return;
    ctx.introShown = true;
    const hasText = ctx.introText.trim().length > 0;
    const hasImage = ctx.introImage.trim().length > 0;
    if (!hasText && !hasImage) return;
    const text = hasText ? ctx.introText : '';
    await appendSystemBubble(ctx, text, true, {
        imageUrl: hasImage ? ctx.introImage : undefined,
        isHtml: hasText,
    });
}

async function startFlow(ctx: Ctx): Promise<void> {
    await showIntro(ctx);
    if (ctx.hasSession) {
        try {
            const resumeId = await getResumeQuestion(ctx.projectId);
            if (resumeId !== null && resumeId !== undefined) {
                const idx = ctx.questionIndexById.get(resumeId);
                if (idx !== undefined) {
                    await renderQuestion(ctx, 0);
                    return;
                }
            }
        } catch {
            /* fall through */
        }
    }
    await renderQuestion(ctx, 0);
}

export function initChat(): void {
    const root = document.getElementById('chat-root');
    if (!root) return;

    const messagesEl = document.getElementById('chat-messages');
    const optionsRow = document.getElementById('chat-options-row');
    const inputRow = document.getElementById('chat-input-row');
    const inputEl = document.getElementById('chat-input') as HTMLTextAreaElement | null;
    const sendBtn = document.getElementById('chat-send') as HTMLButtonElement | null;
    const backBtns = [
        document.getElementById('chat-back'),
        document.getElementById('chat-back-mobile'),
    ].filter((b): b is HTMLButtonElement => b instanceof HTMLButtonElement);
    const restartBtns = [
        document.getElementById('chat-restart'),
        document.getElementById('chat-restart-mobile'),
    ].filter((b): b is HTMLButtonElement => b instanceof HTMLButtonElement);
    if (!messagesEl || !optionsRow || !inputRow || !inputEl || !sendBtn || backBtns.length === 0 || restartBtns.length === 0) return;

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
        optionsRow,
        inputRow,
        inputEl,
        sendBtn,
        rootEl: root,
        backBtns,
        restartBtns,
        questionStack: [],
        activeChips: null,
        confirmBubble: null,
        userTypingBubble: null,
        openHandlers: null,
        introText: root.dataset.introText ?? '',
        introImage: root.dataset.introImage ?? '',
        introShown: false,
    };

    inputEl.addEventListener('input', () => {
        inputEl.style.height = 'auto';
        inputEl.style.height = `${Math.min(inputEl.scrollHeight, 128)}px`;
        if (inputEl.value.trim()) {
            showUserTyping(ctx);
        } else {
            removeUserTyping(ctx);
        }
    });

    wireToolbar(ctx);
    void startFlow(ctx);
}
