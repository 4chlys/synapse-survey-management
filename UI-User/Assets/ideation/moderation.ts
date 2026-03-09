export interface ModerationCallbacks {
    /** Called when the current text is rejected. Show this reason to the user. */
    onBlock: (reason: string, flaggedBy: 'Regex' | 'AI') => void;
    /** Called when the current text is acceptable (clears a previous error). */
    onClear: () => void;
}

interface ModerationResponse {
    isAcceptable: boolean;
    reason:       string | null;
    flaggedBy:    'Regex' | 'AI' | null;
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
    const url = `${_apiBase}/api/moderation/${endpoint}`;

    let res: Response;
    try {
        res = await fetch(url, {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ text }),
            signal,
        });
    } catch (err) {
        if ((err as Error).name === 'AbortError') throw err;
        console.warn(`Moderation API (${endpoint}) unreachable:`, err);
        return { isAcceptable: true, reason: null, flaggedBy: null };
    }

    if (!res.ok) {
        console.warn(`Moderation API (${endpoint}) returned ${res.status}`);
        return { isAcceptable: true, reason: null, flaggedBy: null };
    }

    return await res.json() as Promise<ModerationResponse>;
}

/**
 * Attaches live moderation listeners to `element`.
 * Returns a cleanup function that removes the listeners and cancels pending calls.
 */
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
        let result: ModerationResponse;
        try {
            result = await callModeration('regex', snapshot);
        } catch {
            return;
        }

        if (element.value !== snapshot) return;

        if (!result.isAcceptable && result.reason) {
            callbacks.onBlock(result.reason, 'Regex');
        } else {
            callbacks.onClear();
        }
    }

    async function runAi(snapshot: string) {
        aiAbort?.abort();
        aiAbort = new AbortController();

        let result: ModerationResponse;
        try {
            result = await callModeration('ai', snapshot, aiAbort.signal);
        } catch (err) {
            if ((err as Error).name === 'AbortError') return;
            return;
        }

        if (element.value !== snapshot) return;

        if (!result.isAcceptable && result.reason) {
            callbacks.onBlock(result.reason, result.flaggedBy ?? 'AI');
        } else {
            callbacks.onClear();
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
        aiTimer    = setTimeout(() => runAi(snapshot),    AI_DEBOUNCE_MS);
    }

    element.addEventListener('input', onInput);

    return () => {
        element.removeEventListener('input', onInput);
        clearTimers();
    };
}

/**
 * Runs the full moderation pipeline (regex + AI) synchronously before submitting.
 * Returns `true` if acceptable, `false` if blocked.
 *
 * @param text    The text to check.
 * @param onBlock Optional callback with the rejection reason.
 */
export async function moderateBeforeSubmit(
    text: string,
    onBlock?: (reason: string, flaggedBy: 'Regex' | 'AI') => void,
): Promise<boolean> {
    if (!text || text.trim().length < MIN_LENGTH_FOR_CHECK) return true;

    let result: ModerationResponse;
    try {
        result = await callModeration('ai', text);
    } catch {
        return true;
    }

    if (!result.isAcceptable && result.reason) {
        onBlock?.(result.reason, result.flaggedBy ?? 'AI');
        return false;
    }

    return true;
}

/**
 * Wires live moderation to a single textarea or input.
 * - Injects an error <p> directly after the element.
 * - Sets `data-moderation-blocked="true"` on the input when flagged.
 * - Returns a cleanup/detach function.
 *
 * The form submit must be intercepted separately — see wireModerationForm().
 */
export function wireModerationField(
    input: HTMLTextAreaElement | HTMLInputElement,
): () => void {
    const errorEl = document.createElement('p');
    errorEl.className =
        'moderation-error hidden text-red-500 text-xs mt-1 font-medium animate-[fade-up_0.2s_ease-out_both]';

    input.insertAdjacentElement('afterend', errorEl);

    function showError(reason: string) {
        errorEl.textContent = reason;
        errorEl.classList.remove('hidden');
        input.setAttribute('data-moderation-blocked', 'true');
        input.classList.add('border-red-400');
    }

    function clearError() {
        errorEl.textContent = '';
        errorEl.classList.add('hidden');
        input.removeAttribute('data-moderation-blocked');
        input.classList.remove('border-red-400');
    }

    const detach = attachModeration(input, {
        onBlock: showError,
        onClear: clearError,
    });

    return () => {
        detach();
        errorEl.remove();
    };
}

/**
 * Wires moderation to every <textarea> inside `form` and intercepts submit.
 *
 * On submit:
 *   1. Blocks submission if any field already has `data-moderation-blocked`.
 *   2. Runs a final AI check on all open-answer fields before allowing submit.
 *   3. Shows per-field errors and calls `onSubmitBlocked` if any field fails.
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

        const alreadyBlocked = textareas.some(
            ta => ta.getAttribute('data-moderation-blocked') === 'true'
        );
        if (alreadyBlocked) {
            onSubmitBlocked?.();
            return;
        }

        const checks = await Promise.all(
            textareas
                .filter(ta => ta.value.trim().length >= MIN_LENGTH_FOR_CHECK)
                .map(async ta => {
                    const acceptable = await moderateBeforeSubmit(
                        ta.value,
                        (reason, flaggedBy) => {
                            ta.setAttribute('data-moderation-blocked', 'true');
                            ta.classList.add('border-red-400');
                            const existing = ta.nextElementSibling;
                            if (existing?.classList.contains('moderation-error')) {
                                existing.textContent = reason;
                                existing.classList.remove('hidden');
                            }
                        }
                    );
                    return acceptable;
                })
        );

        if (checks.some(ok => !ok)) {
            onSubmitBlocked?.();
            return;
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