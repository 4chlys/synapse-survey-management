import {t} from '../lib/i18n';
import { getCsrfToken } from '../lib/utils';

export interface ModerationCallbacks {
    onBlock: (reason: string, flaggedBy: 'Regex' | 'AI') => void;
    onClear: () => void;
}

export interface ModerationResponse {
    isAcceptable: boolean;
    reason: string | null;
    flaggedBy: 'Regex' | 'AI' | null;
    moderationToken: string | null;
}

interface SuggestResponse {
    suggestion: string | null;
    moderationToken: string | null;
}

export interface RewritePopupOutcome {
    action: 'accepted' | 'rewrite' | 'submitFlagged';
    text: string;
}

// Config

const REGEX_DEBOUNCE_MS = 400;
const AI_DEBOUNCE_MS = 1500;
const MIN_LENGTH_FOR_CHECK = 3;

// Low-level API calls (routed through UI-User ModerationApiController)

async function callModeration(
    endpoint: 'regex' | 'ai',
    text: string,
    projectId: number,
    contentType: 'idea' | 'comment' = 'idea',
    signal?: AbortSignal,
): Promise<ModerationResponse> {
    try {
        const res = await fetch(`/${projectId}/api/ModerationApi/${endpoint}`, {
            method: 'POST',
            headers: {'Content-Type': 'application/json', 'RequestVerificationToken': getCsrfToken()},
            body: JSON.stringify({text: text, contentType: contentType}),
            signal,
        });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res.json();
    } catch (err) {
        if ((err as Error).name === 'AbortError') throw err;
        console.warn(`Moderation (${endpoint}) unreachable:`, err);
        return {moderationToken: '', isAcceptable: true, reason: null, flaggedBy: null};
    }
}

/** Called by ideationForm.ts on submit */
export async function callModerationAi(
    text: string,
    projectId: number,
    contentType: 'idea' | 'comment' = 'idea',
): Promise<ModerationResponse> {
    return callModeration('ai', text, projectId, contentType);
}

/** Called by ideationForm.ts on submit. Returns suggestion text + its pre-approved token. */
export async function callSuggest(
    text: string,
    projectId: number,
    language?: string,
): Promise<{ suggestion: string | null; moderationToken: string | null }> {
    try {
        const res = await fetch(`/${projectId}/api/ModerationApi/suggest`, {
            method: 'POST',
            headers: {'Content-Type': 'application/json', 'RequestVerificationToken': getCsrfToken()},
            body: JSON.stringify({text, language}),
        });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data: SuggestResponse = await res.json();
        return {suggestion: data.suggestion ?? null, moderationToken: data.moderationToken ?? null};
    } catch (err) {
        console.warn('Moderation suggest unreachable:', err);
        return {suggestion: null, moderationToken: null};
    }
}

// Popup

function getPopupEls() {
    return {
        overlay: document.getElementById('moderation-overlay')!,
        reason: document.getElementById('moderation-reason')!,
        suggestionBlock: document.getElementById('moderation-suggestion-block')!,
        suggestionText: document.getElementById('moderation-suggestion-text')!,
        blockMessage: document.getElementById('moderation-block-message')!,
        btnAccept: document.getElementById('moderation-btn-accept')!,
        btnRewrite: document.getElementById('moderation-btn-rewrite')!,
        btnClose: document.getElementById('moderation-btn-close')!,
        btnSubmitFlagged: document.getElementById('moderation-btn-submit-flagged')!,
        btnX: document.getElementById('moderation-btn-x'),
    };
}

/**
 * Shows the moderation popup.
 *
 * When a suggestion is available, three outcomes are possible:
 *   { action: 'accepted',      text: rewrittenText } — user accepted the AI rewrite
 *   { action: 'rewrite',       text: originalText  } — user wants to edit themselves
 *   { action: 'submitFlagged', text: originalText  } — user insists on original (stored as Flagged)
 *
 * When suggestion === null the block-only variant is shown with a single close button.
 */
export function showRewritePopup(
    originalText: string,
    suggestion: string | null,
    reason: string,
): Promise<RewritePopupOutcome> {
    return new Promise(resolve => {
        const els = getPopupEls();

        els.reason.textContent = reason;

        if (suggestion) {
            els.suggestionText.textContent = suggestion;
            els.suggestionBlock.classList.remove('hidden');
            els.blockMessage.classList.add('hidden');
            els.btnAccept.classList.remove('hidden');
            els.btnRewrite.classList.remove('hidden');
            els.btnClose.classList.add('hidden');
            els.btnSubmitFlagged.classList.remove('hidden');
        } else {
            els.suggestionBlock.classList.add('hidden');
            els.blockMessage.classList.remove('hidden');
            els.btnAccept.classList.add('hidden');
            els.btnRewrite.classList.add('hidden');
            els.btnClose.classList.remove('hidden');
            els.btnSubmitFlagged.classList.remove('hidden');
        }

        els.overlay.classList.remove('hidden');
        els.overlay.classList.add('flex');

        function close() {
            els.overlay.classList.add('hidden');
            els.overlay.classList.remove('flex');
            els.btnAccept.removeEventListener('click', onAccept);
            els.btnRewrite.removeEventListener('click', onRewrite);
            els.btnClose.removeEventListener('click', onClose);
            els.btnSubmitFlagged.removeEventListener('click', onSubmitFlagged);
            els.btnX?.removeEventListener('click', onClose);
        }

        function onAccept() {
            close();
            resolve({action: 'accepted', text: suggestion!});
        }

        function onRewrite() {
            close();
            resolve({action: 'rewrite', text: originalText});
        }

        function onClose() {
            close();
            resolve({action: 'rewrite', text: originalText});
        }

        function onSubmitFlagged() {
            close();
            resolve({action: 'submitFlagged', text: originalText});
        }

        els.btnAccept.addEventListener('click', onAccept, {once: true});
        els.btnRewrite.addEventListener('click', onRewrite, {once: true});
        els.btnClose.addEventListener('click', onClose, {once: true});
        els.btnSubmitFlagged.addEventListener('click', onSubmitFlagged, {once: true});
        els.btnX?.addEventListener('click', onClose, {once: true});
    });
}

// Live inline field moderation (typing feedback)

export function attachModeration(
    element: HTMLTextAreaElement | HTMLInputElement,
    callbacks: ModerationCallbacks,
    projectId: number,
    contentType: 'idea' | 'comment' = 'idea',
): () => void {
    let regexTimer: ReturnType<typeof setTimeout> | null = null;
    let aiTimer: ReturnType<typeof setTimeout> | null = null;
    let aiAbort: AbortController | null = null;

    function clearTimers() {
        if (regexTimer !== null) {
            clearTimeout(regexTimer);
            regexTimer = null;
        }
        if (aiTimer !== null) {
            clearTimeout(aiTimer);
            aiTimer = null;
        }
        aiAbort?.abort();
        aiAbort = null;
    }

    async function runRegex(snapshot: string) {
        try {
            const result = await callModeration('regex', snapshot, projectId, contentType);
            if (element.value !== snapshot) return;
            if (!result.isAcceptable && result.reason)
                callbacks.onBlock(result.reason, 'Regex');
            else
                callbacks.onClear();
        } catch { /* ignore */
        }
    }

    async function runAi(snapshot: string) {
        aiAbort?.abort();
        aiAbort = new AbortController();
        try {
            const result = await callModeration('ai', snapshot, projectId, contentType, aiAbort.signal);
            if (element.value !== snapshot) return;
            if (!result.isAcceptable && result.reason)
                callbacks.onBlock(result.reason, result.flaggedBy ?? 'AI');
            else
                callbacks.onClear();
        } catch { /* ignore */
        }
    }

    function onInput() {
        const text = element.value;
        clearTimers();
        if (text.length < MIN_LENGTH_FOR_CHECK) {
            callbacks.onClear();
            return;
        }
        const snapshot = text;
        regexTimer = setTimeout(() => runRegex(snapshot), REGEX_DEBOUNCE_MS);
        aiTimer = setTimeout(() => runAi(snapshot), AI_DEBOUNCE_MS);
    }

    element.addEventListener('input', onInput);
    return () => {
        element.removeEventListener('input', onInput);
        clearTimers();
    };
}

export function wireModerationField(
    input: HTMLTextAreaElement | HTMLInputElement,
    projectId: number,
    contentType: 'idea' | 'comment' = 'idea',
): () => void {
    const wrapper = document.createElement('div');
    wrapper.className = 'flex flex-col flex-1 min-w-0';
    input.parentNode!.insertBefore(wrapper, input);
    wrapper.appendChild(input);

    const errorEl = document.createElement('p');
    errorEl.className =
        'moderation-error hidden text-red-500 text-xs mt-1 font-medium ' +
        'animate-[fade-up_0.2s_ease-out_both]';
    wrapper.appendChild(errorEl);

    const showError = (reason: string) => {
        errorEl.textContent = reason;
        errorEl.classList.remove('hidden');
        input.setAttribute('data-moderation-blocked', 'true');
        input.classList.add('border-red-400');
    };

    const clearError = () => {
        errorEl.textContent = '';
        errorEl.classList.add('hidden');
        input.removeAttribute('data-moderation-blocked');
        input.classList.remove('border-red-400');
    };

    const detach = attachModeration(input, {onBlock: showError, onClear: clearError}, projectId, contentType);
    return () => {
        detach();
        // Unwrap: move input back to its original position and remove wrapper
        wrapper.parentNode?.insertBefore(input, wrapper);
        wrapper.remove();
    };
}

// Survey form wiring

export function wireModerationForm(
    form: HTMLFormElement,
    onSubmitBlocked?: () => void,
): void {
    const projectId = getProjectIdFromContext(form);
    const textareas = Array.from(form.querySelectorAll<HTMLTextAreaElement>('textarea'));
    textareas.forEach(ta => wireModerationField(ta, projectId));

    async function onSubmit(e: SubmitEvent) {
        e.preventDefault();

        const toCheck = textareas.filter(ta => ta.value.trim().length >= MIN_LENGTH_FOR_CHECK);
        let anyBlocked = false;

        for (const textarea of toCheck) {
            const result = await callModeration('ai', textarea.value, projectId);
            if (result.isAcceptable) continue;

            const suggestResult = await callSuggest(
                textarea.value,
                projectId,
                form.querySelector<HTMLSelectElement>('select[name="languageCode"]')?.value,
            );
            const outcome = await showRewritePopup(
                textarea.value,
                suggestResult.suggestion,
                result.reason ?? t('contentNotAllowed'),
            );

            if (outcome.action === 'accepted') {
                textarea.value = outcome.text;
                textarea.removeAttribute('data-moderation-blocked');
                textarea.removeAttribute('data-flagged');
            } else if (outcome.action === 'rewrite') {
                anyBlocked = true;
                onSubmitBlocked?.();
                break;
            }
            // submitFlagged: continue, submit as-is
        }

        if (anyBlocked) return;

        const tokenInput = form.querySelector<HTMLInputElement>('input[name="moderationToken"]');
        if (tokenInput) tokenInput.value = '';

        form.removeEventListener('submit', onSubmit);
        form.requestSubmit();
    }

    form.addEventListener('submit', onSubmit);
}

// Helpers

function getProjectIdFromContext(form: HTMLFormElement): number {
    const attr = form.dataset.projectId;
    if (attr) return parseInt(attr, 10);
    const meta = document.querySelector<HTMLMetaElement>('meta[name="project-id"]');
    if (meta?.content) return parseInt(meta.content, 10);
    const match = window.location.pathname.match(/^\/(\d+)\//);
    if (match) return parseInt(match[1], 10);
    return 1;
}