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

export interface ModerationOutcome {
    proceed:    boolean;
    finalText:  string | null;
    wasFlagged: boolean;
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

/**
 * Shows a modal popup with the AI's rewrite suggestion.
 * Resolves with the user's choice:
 *   - { accepted: true,  text: rewrittenText }  → user accepted the rewrite
 *   - { accepted: false, text: originalText  }  → user declined, submit as flagged
 *
 * If suggestion is null (model couldn't rewrite), shows a block-only message
 * and resolves with { accepted: false, text: originalText }.
 */
function showRewritePopup(
    originalText: string,
    suggestion: string | null,
    reason: string,
): Promise<{ accepted: boolean; text: string }> {
    return new Promise(resolve => {
        const overlay = document.createElement('div');
        overlay.className = [
            'fixed inset-0 z-50 flex items-center justify-center',
            'bg-black/40 backdrop-blur-sm',
            'animate-[fade-up_0.2s_ease-out_both]',
        ].join(' ');

        const card = document.createElement('div');
        card.className = [
            'bg-white rounded-2xl shadow-xl mx-4 w-full max-w-md',
            'flex flex-col gap-4 p-6',
        ].join(' ');

        const header = document.createElement('div');
        header.className = 'flex items-start gap-3';

        const icon = document.createElement('div');
        icon.className = 'w-10 h-10 rounded-full bg-amber-100 flex items-center justify-center flex-shrink-0';
        icon.innerHTML = `
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 text-amber-600"
                 fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round"
                      d="M12 9v2m0 4h.01M10.29 3.86L1.82 18a2 2 0
                         001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2
                         2 0 00-3.42 0z" />
            </svg>`;

        const titleBlock = document.createElement('div');
        const title = document.createElement('h3');
        title.className = 'text-gray-900 font-semibold text-base';
        title.textContent = 'Je bericht bevat ongepaste inhoud';

        const subtitle = document.createElement('p');
        subtitle.className = 'text-gray-500 text-sm mt-0.5';
        subtitle.textContent = reason;

        titleBlock.append(title, subtitle);
        header.append(icon, titleBlock);

        card.appendChild(header);

        if (suggestion) {
            const suggestLabel = document.createElement('p');
            suggestLabel.className = 'text-xs font-semibold uppercase tracking-widest text-gray-400';
            suggestLabel.textContent = 'Voorgestelde herschrijving';

            const suggestBox = document.createElement('div');
            suggestBox.className = [
                'rounded-xl border-2 border-emerald-200 bg-emerald-50',
                'px-4 py-3 text-sm text-gray-800 leading-relaxed',
            ].join(' ');
            suggestBox.textContent = suggestion;

            const hint = document.createElement('p');
            hint.className = 'text-xs text-gray-400';
            hint.textContent = 'Accepteer de herschrijving of dien je originele bericht in als anonieme melding.';

            card.append(suggestLabel, suggestBox, hint);

            const btnRow = document.createElement('div');
            btnRow.className = 'flex gap-3 mt-1';

            const btnDecline = document.createElement('button');
            btnDecline.type = 'button';
            btnDecline.className = [
                'flex-1 py-2.5 rounded-xl border-2 border-gray-200',
                'text-gray-600 text-sm font-medium',
                'hover:bg-gray-50 active:scale-[0.98] transition-all',
            ].join(' ');
            btnDecline.textContent = 'Toch insturen (als melding)';

            const btnAccept = document.createElement('button');
            btnAccept.type = 'button';
            btnAccept.className = [
                'flex-1 py-2.5 rounded-xl bg-emerald-500',
                'text-white text-sm font-semibold',
                'hover:bg-emerald-600 active:scale-[0.98] transition-all shadow-sm',
            ].join(' ');
            btnAccept.textContent = 'Herschrijving gebruiken';

            btnDecline.addEventListener('click', () => {
                document.body.removeChild(overlay);
                resolve({ accepted: false, text: originalText });
            });

            btnAccept.addEventListener('click', () => {
                document.body.removeChild(overlay);
                resolve({ accepted: true, text: suggestion });
            });

            btnRow.append(btnDecline, btnAccept);
            card.appendChild(btnRow);

        } else {
            const blockMsg = document.createElement('p');
            blockMsg.className = 'text-sm text-gray-600';
            blockMsg.textContent =
                'Je bericht kan niet worden ingediend. Pas het aan en probeer opnieuw.';

            const btnClose = document.createElement('button');
            btnClose.type = 'button';
            btnClose.className = [
                'w-full py-2.5 rounded-xl bg-gray-100',
                'text-gray-700 text-sm font-medium',
                'hover:bg-gray-200 active:scale-[0.98] transition-all',
            ].join(' ');
            btnClose.textContent = 'Sluiten en aanpassen';

            btnClose.addEventListener('click', () => {
                document.body.removeChild(overlay);
                resolve({ accepted: false, text: originalText });
            });

            card.append(blockMsg, btnClose);
        }

        overlay.appendChild(card);
        document.body.appendChild(overlay);
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
            if (element.value !== snapshot) return; // stale
            if (!result.isAcceptable && result.reason)
                callbacks.onBlock(result.reason, 'Regex');
            else
                callbacks.onClear();
        } catch { }
    }

    async function runAi(snapshot: string) {
        aiAbort?.abort();
        aiAbort = new AbortController();
        try {
            const result = await callModeration('ai', snapshot, aiAbort.signal);
            if (element.value !== snapshot) return; // stale
            if (!result.isAcceptable && result.reason)
                callbacks.onBlock(result.reason, result.flaggedBy ?? 'AI');
            else
                callbacks.onClear();
        } catch { }
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

export function wireModerationField(
    input: HTMLTextAreaElement | HTMLInputElement,
): () => void {
    const errorEl = document.createElement('p');
    errorEl.className =
        'moderation-error hidden text-red-500 text-xs mt-1 font-medium animate-[fade-up_0.2s_ease-out_both]';
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
 * Wires live moderation to every <textarea> in `form` and intercepts submit.
 *
 * On submit for each flagged textarea:
 *   1. Calls /api/moderation/suggest for a rewrite.
 *   2. Shows the popup — user accepts or declines.
 *   3a. Accepted  → textarea value is replaced with the clean rewrite.
 *   3b. Declined  → textarea gets data-flagged="true" so the server layer
 *                   (to be added with the Idea feature) can store it as flagged.
 *   3c. Null suggestion → popup tells user to edit manually, submit is blocked.
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
            ta => ta.value.trim().length >= MIN_LENGTH_FOR_CHECK
        );

        let anyBlocked = false;

        for (const ta of toCheck) {
            const result = await callModeration('ai', ta.value);

            if (result.isAcceptable) continue;
            
            const suggestion = await callSuggest(ta.value);

            const outcome = await showRewritePopup(
                ta.value,
                suggestion,
                result.reason ?? 'De inhoud is niet toegestaan op dit platform.'
            );

            if (suggestion === null) {
                anyBlocked = true;
                onSubmitBlocked?.();
                break;
            }

            if (outcome.accepted) {
                ta.value = outcome.text;
                ta.removeAttribute('data-moderation-blocked');
                ta.removeAttribute('data-flagged');
            } else {
                ta.setAttribute('data-flagged', 'true');
                ta.removeAttribute('data-moderation-blocked');
            }
        }

        if (anyBlocked) return;

        form.removeEventListener('submit', onSubmit);
        form.requestSubmit();
    }

    form.addEventListener('submit', onSubmit);

    return () => {
        form.removeEventListener('submit', onSubmit);
        detachers.forEach(d => d());
    };
}