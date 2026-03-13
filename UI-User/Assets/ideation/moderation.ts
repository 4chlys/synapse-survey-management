export interface ModerationCallbacks {
    onBlock: (reason: string, flaggedBy: 'Regex' | 'AI') => void;
    onClear: () => void;
}

interface ModerationResponse {
    isAcceptable: boolean;
    reason:       string | null;
    flaggedBy:    'Regex' | 'AI' | null;
}

interface SuggestResponse {
    suggestion: string | null;
}

let _apiBase = '';

export function setApiBase(url: string): void {
    _apiBase = url.replace(/\/$/, '');
}

const REGEX_DEBOUNCE_MS    = 400;
const AI_DEBOUNCE_MS       = 1500;
const MIN_LENGTH_FOR_CHECK = 3;

async function callModeration(
    endpoint: 'regex' | 'ai',
    text: string,
    signal?: AbortSignal,
): Promise<ModerationResponse> {
    try {
        const res = await fetch(`${_apiBase}/api/moderation/${endpoint}`, {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ text }),
            signal,
        });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res.json();
    } catch (err) {
        if ((err as Error).name === 'AbortError') throw err;
        console.warn(`Moderation API (${endpoint}) unreachable:`, err);
        return { isAcceptable: true, reason: null, flaggedBy: null };
    }
}

async function callSuggest(text: string): Promise<string | null> {
    try {
        const res = await fetch(`${_apiBase}/api/moderation/suggest`, {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ text }),
        });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data: SuggestResponse = await res.json();
        return data.suggestion ?? null;
    } catch (err) {
        console.warn('Moderation suggest API unreachable:', err);
        return null;
    }
}

function getPopupEls() {
    return {
        overlay:         document.getElementById('moderation-overlay')!,
        reason:          document.getElementById('moderation-reason')!,
        suggestionBlock: document.getElementById('moderation-suggestion-block')!,
        suggestionText:  document.getElementById('moderation-suggestion-text')!,
        blockMessage:    document.getElementById('moderation-block-message')!,
        btnDecline:      document.getElementById('moderation-btn-decline')!,
        btnAccept:       document.getElementById('moderation-btn-accept')!,
        btnClose:        document.getElementById('moderation-btn-close')!,
    };
}

/**
 * Shows the popup rendered by _ModerationPopup.cshtml.
 *
 * Resolves with:
 *   { accepted: true,  text: rewrittenText } — user accepted the rewrite
 *   { accepted: false, text: originalText  } — user declined or no rewrite possible
 */
function showRewritePopup(
    originalText: string,
    suggestion: string | null,
    reason: string,
): Promise<{ accepted: boolean; text: string }> {
    return new Promise(resolve => {
        const els = getPopupEls();

        els.reason.textContent = reason;

        if (suggestion) {
            els.suggestionText.textContent = suggestion;
            els.suggestionBlock.classList.remove('hidden');
            els.blockMessage.classList.add('hidden');

            els.btnDecline.classList.remove('hidden');
            els.btnAccept.classList.remove('hidden');
            els.btnClose.classList.add('hidden');
        } else {
            els.suggestionBlock.classList.add('hidden');
            els.blockMessage.classList.remove('hidden');

            els.btnDecline.classList.add('hidden');
            els.btnAccept.classList.add('hidden');
            els.btnClose.classList.remove('hidden');
        }

        els.overlay.classList.remove('hidden');
        els.overlay.classList.add('flex');

        function close() {
            els.overlay.classList.add('hidden');
            els.overlay.classList.remove('flex');
            els.btnAccept.removeEventListener('click', onAccept);
            els.btnDecline.removeEventListener('click', onDecline);
            els.btnClose.removeEventListener('click', onClose);
        }

        function onAccept() {
            close();
            resolve({ accepted: true, text: suggestion! });
        }

        function onDecline() {
            close();
            resolve({ accepted: false, text: originalText });
        }

        function onClose() {
            close();
            resolve({ accepted: false, text: originalText });
        }

        els.btnAccept.addEventListener('click', onAccept,  { once: true });
        els.btnDecline.addEventListener('click', onDecline, { once: true });
        els.btnClose.addEventListener('click', onClose,   { once: true });
    });
}

export function attachModeration(
    element: HTMLTextAreaElement | HTMLInputElement,
    callbacks: ModerationCallbacks,
): () => void {
    let regexTimer: ReturnType<typeof setTimeout> | null = null;
    let aiTimer:    ReturnType<typeof setTimeout> | null = null;
    let aiAbort:    AbortController | null = null;

    function clearTimers() {
        if (regexTimer !== null) { clearTimeout(regexTimer); regexTimer = null; }
        if (aiTimer    !== null) { clearTimeout(aiTimer);    aiTimer    = null; }
        aiAbort?.abort();
        aiAbort = null;
    }

    async function runRegex(snapshot: string) {
        try {
            const result = await callModeration('regex', snapshot);
            if (element.value !== snapshot) return;
            if (!result.isAcceptable && result.reason)
                callbacks.onBlock(result.reason, 'Regex');
            else
                callbacks.onClear();
        } catch { /* AbortError or network — ignore */ }
    }

    async function runAi(snapshot: string) {
        aiAbort?.abort();
        aiAbort = new AbortController();
        try {
            const result = await callModeration('ai', snapshot, aiAbort.signal);
            if (element.value !== snapshot) return;
            if (!result.isAcceptable && result.reason)
                callbacks.onBlock(result.reason, result.flaggedBy ?? 'AI');
            else
                callbacks.onClear();
        } catch { /* AbortError or network — ignore */ }
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
        aiTimer    = setTimeout(() => runAi(snapshot),    AI_DEBOUNCE_MS);
    }

    element.addEventListener('input', onInput);
    return () => { element.removeEventListener('input', onInput); clearTimers(); };
}

/**
 * Attaches live regex + AI moderation to a single textarea or input.
 * Injects an inline error <p> directly after the element.
 * Returns a cleanup function.
 */
export function wireModerationField(
    input: HTMLTextAreaElement | HTMLInputElement,
): () => void {
    const errorEl = document.createElement('p');
    errorEl.className =
        'moderation-error hidden text-red-500 text-xs mt-1 font-medium ' +
        'animate-[fade-up_0.2s_ease-out_both]';
    input.insertAdjacentElement('afterend', errorEl);

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

    const detach = attachModeration(input, { onBlock: showError, onClear: clearError });
    return () => { detach(); errorEl.remove(); };
}

/**
 * Wires live moderation to every <textarea> in the form and intercepts submit.
 *
 * On submit, for each flagged textarea:
 *   1. Calls /api/moderation/suggest for a rewrite.
 *   2. Shows the popup from _ModerationPopup.cshtml.
 *   3a. Accepted  → textarea value replaced with the clean rewrite.
 *   3b. Declined  → textarea gets data-flagged="true" for the server to store as flagged.
 *   3c. No rewrite possible → user told to edit manually, submit blocked.
 *
 * Before the final requestSubmit(), stamps any hidden input[name="moderationStatus"]
 * with "Flagged" if any textarea was declined, otherwise "Unflagged".
 *
 * Returns a cleanup function.
 */
export function wireModerationForm(
    form: HTMLFormElement,
    onSubmitBlocked?: () => void,
): () => void {
    const textareas = Array.from(form.querySelectorAll<HTMLTextAreaElement>('textarea'));
    const detachers = textareas.map(ta => wireModerationField(ta));

    async function onSubmit(e: SubmitEvent) {
        e.preventDefault();

        const toCheck = textareas.filter(
            textarea => textarea.value.trim().length >= MIN_LENGTH_FOR_CHECK
        );

        let anyBlocked = false;

        for (const textarea of toCheck) {
            const result = await callModeration('ai', textarea.value);
            if (result.isAcceptable) continue;

            const suggestion = await callSuggest(textarea.value);

            const outcome = await showRewritePopup(
                textarea.value,
                suggestion,
                result.reason ?? 'De inhoud is niet toegestaan op dit platform.'
            );

            if (suggestion === null) {
                anyBlocked = true;
                onSubmitBlocked?.();
                break;
            }

            if (outcome.accepted) {
                textarea.value = outcome.text;
                textarea.removeAttribute('data-moderation-blocked');
                textarea.removeAttribute('data-flagged');
            } else {
                textarea.setAttribute('data-flagged', 'true');
                textarea.removeAttribute('data-moderation-blocked');
            }
        }

        if (anyBlocked) return;

        const statusInput = form.querySelector<HTMLInputElement>('input[name="moderationStatus"]');
        if (statusInput) {
            const anyFlagged = textareas.some(ta => ta.dataset.flagged === 'true');
            statusInput.value = anyFlagged ? 'Flagged' : 'Unflagged';
        }

        form.removeEventListener('submit', onSubmit);
        form.requestSubmit();
    }

    form.addEventListener('submit', onSubmit);

    return () => {
        form.removeEventListener('submit', onSubmit);
        detachers.forEach(d => d());
    };
}