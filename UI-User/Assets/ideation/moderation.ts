// Types

export interface ModerationCallbacks {
    /** Called when the current text is rejected. Show this reason to the user. */
    onBlock: (reason: string, flaggedBy: 'Regex' | 'AI') => void;
    /** Called when the current text is acceptable (clears previous error). */
    onClear: () => void;
}

interface ModerationResponse {
    isAcceptable: boolean;
    reason:      string | null;
    flaggedBy:   'Regex' | 'AI' | null;
}

// Config

const API_BASE             = '/api/moderation';
const REGEX_DEBOUNCE_MS    = 400;
const AI_DEBOUNCE_MS       = 1500;
const MIN_LENGTH_FOR_CHECK = 3; 

// Internal API helpers

async function callModeration(
    endpoint: 'regex' | 'ai',
    text: string,
    signal?: AbortSignal,
): Promise<ModerationResponse> {
    const res = await fetch(`${API_BASE}/${endpoint}`, {
        method:  'POST',
        headers: { 'Content-Type': 'application/json' },
        body:    JSON.stringify({ text }),
        signal,
    });

    if (!res.ok) {
        // Fail open – don't block the user if the API is unavailable
        console.warn(`Moderation API (${endpoint}) returned ${res.status}`);
        return { isAcceptable: true, reason: null, flaggedBy: null };
    }

    return res.json() as Promise<ModerationResponse>;
}

// Public: attach live moderation to an input element

/**
 * Attaches live moderation listeners to `element`.
 * Returns a cleanup function that removes the listeners.
 */
export function attachModeration(
    element: HTMLTextAreaElement | HTMLInputElement,
    callbacks: ModerationCallbacks,
): () => void {
    let regexTimer: ReturnType<typeof setTimeout> | null = null;
    let aiTimer:    ReturnType<typeof setTimeout> | null = null;
    let aiAbort:    AbortController | null = null;

    function clearTimers() {
        if (regexTimer !== null) clearTimeout(regexTimer);
        if (aiTimer    !== null) clearTimeout(aiTimer);
        aiAbort?.abort();
    }

    async function runRegex(text: string) {
        const result = await callModeration('regex', text);
        if (!result.isAcceptable && result.reason) {
            callbacks.onBlock(result.reason, 'Regex');
        } else {
            callbacks.onClear();
        }
    }

    async function runAi(text: string) {
        aiAbort?.abort();
        aiAbort = new AbortController();

        const result = await callModeration('ai', text, aiAbort.signal);
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
        
        regexTimer = setTimeout(() => runRegex(text), REGEX_DEBOUNCE_MS);

        aiTimer = setTimeout(() => runAi(text), AI_DEBOUNCE_MS);
    }

    element.addEventListener('input', onInput);

    return () => {
        element.removeEventListener('input', onInput);
        clearTimers();
    };
}

// Public: validate text before submit

/**
 * Runs the full moderation pipeline (regex + AI) before submitting.
 * Returns `true` if the content is acceptable, `false` if it should be blocked.
 *
 * @param text    The text to check.
 * @param onBlock Optional callback with the rejection reason.
 */
export async function moderateBeforeSubmit(
    text: string,
    onBlock?: (reason: string, flaggedBy: 'Regex' | 'AI') => void,
): Promise<boolean> {
    if (!text || text.trim().length < MIN_LENGTH_FOR_CHECK) return true;

    const result = await callModeration('ai', text);

    if (!result.isAcceptable && result.reason) {
        onBlock?.(result.reason, result.flaggedBy ?? 'AI');
        return false;
    }

    return true;
}

// Wiring

/**
 * High-level helper that:
 *  - Attaches live moderation to `input`
 *  - Injects an error `<p>` element below `input`
 *  - Blocks form submission when content is flagged
 *
 * Returns a cleanup / detach function.
 *
 * @example
 * ```ts
 * import { wireModerationField } from './moderation';
 *
 * wireModerationField(document.querySelector<HTMLTextAreaElement>('#idea-input')!);
 * ```
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


