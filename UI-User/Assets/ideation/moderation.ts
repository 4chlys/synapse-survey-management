// ─── Types ────────────────────────────────────────────────────────────────────

export interface ModerationCallbacks {
    onBlock: (reason: string, flaggedBy: 'Regex' | 'AI') => void;
    onClear: () => void;
}

export interface ModerationResponse {
    isAcceptable:    boolean;
    reason:          string | null;
    flaggedBy:       'Regex' | 'AI' | null;
    moderationToken: string | null;
}

interface SuggestResponse {
    suggestion:      string | null;
    moderationToken: string | null;
}

// ─── CSRF ─────────────────────────────────────────────────────────────────────

function getCsrfToken(): string {
    return document.querySelector<HTMLMetaElement>('meta[name="__RequestVerificationToken"]')?.content ?? '';
}

// ─── Config ───────────────────────────────────────────────────────────────────

const REGEX_DEBOUNCE_MS    = 400;
const AI_DEBOUNCE_MS       = 1500;
const MIN_LENGTH_FOR_CHECK = 3;

// ─── Low-level API calls (routed through UI-User ModerationApiController) ────

async function callModeration(
    endpoint: 'regex' | 'ai',
    text: string,
    projectId: number,
    signal?: AbortSignal,
): Promise<ModerationResponse> {
    try {
        const res = await fetch(`/${projectId}/api/ModerationApi/${endpoint}`, {
            method:  'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getCsrfToken() },
            body:    JSON.stringify({ text }),
            signal,
        });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res.json();
    } catch (err) {
        if ((err as Error).name === 'AbortError') throw err;
        console.warn(`Moderation (${endpoint}) unreachable:`, err);
        return { isAcceptable: true, reason: null, flaggedBy: null, moderationToken: null };
    }
}

/** Called by ideationForm.ts on submit */
export async function callModerationAi(
    text: string,
    projectId: number,
): Promise<ModerationResponse> {
    return callModeration('ai', text, projectId);
}

/** Called by ideationForm.ts on submit. Returns suggestion text + its pre-approved token. */
export async function callSuggest(
    text: string,
    projectId: number,
): Promise<{ suggestion: string | null; moderationToken: string | null }> {
    try {
        const res = await fetch(`/${projectId}/api/ModerationApi/suggest`, {
            method:  'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getCsrfToken() },
            body:    JSON.stringify({ text }),
        });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data: SuggestResponse = await res.json();
        return { suggestion: data.suggestion ?? null, moderationToken: data.moderationToken ?? null };
    } catch (err) {
        console.warn('Moderation suggest unreachable:', err);
        return { suggestion: null, moderationToken: null };
    }
}

// ─── Popup ────────────────────────────────────────────────────────────────────

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
        btnX:            document.getElementById('moderation-btn-x'),
    };
}

/**
 * Shows the moderation popup (rendered by _ModerationPopup.cshtml) as a
 * true overlay — never navigates away from the current page.
 *
 * Resolves with:
 *   { accepted: true,  text: rewrittenText } — user accepted the AI rewrite
 *   { accepted: false, text: originalText  } — user declined (submit flagged)
 *
 * When suggestion === null the "edit and retry" variant is shown.
 * The caller should block submission in that case.
 */
export function showRewritePopup(
    originalText: string,
    suggestion: string | null,
    reason: string,
): Promise<{ accepted: boolean; text: string }> {
    return new Promise(resolve => {
        const els = getPopupEls();

        // Populate reason
        els.reason.textContent = reason;

        // Configure which variant to show
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

        // Show overlay
        els.overlay.classList.remove('hidden');
        els.overlay.classList.add('flex');

        function close() {
            els.overlay.classList.add('hidden');
            els.overlay.classList.remove('flex');
            els.btnAccept.removeEventListener('click',  onAccept);
            els.btnDecline.removeEventListener('click', onDecline);
            els.btnClose.removeEventListener('click',   onClose);
            els.btnX?.removeEventListener('click',      onClose);
        }

        function onAccept()  { close(); resolve({ accepted: true,  text: suggestion! }); }
        function onDecline() { close(); resolve({ accepted: false, text: originalText }); }
        function onClose()   { close(); resolve({ accepted: false, text: originalText }); }

        els.btnAccept.addEventListener('click',  onAccept,  { once: true });
        els.btnDecline.addEventListener('click', onDecline, { once: true });
        els.btnClose.addEventListener('click',   onClose,   { once: true });
        els.btnX?.addEventListener('click',      onClose,   { once: true });
    });
}

// ─── Live inline field moderation (typing feedback) ──────────────────────────

export function attachModeration(
    element: HTMLTextAreaElement | HTMLInputElement,
    callbacks: ModerationCallbacks,
    projectId: number,
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
            const result = await callModeration('regex', snapshot, projectId);
            if (element.value !== snapshot) return;
            if (!result.isAcceptable && result.reason)
                callbacks.onBlock(result.reason, 'Regex');
            else
                callbacks.onClear();
        } catch { /* ignore */ }
    }

    async function runAi(snapshot: string) {
        aiAbort?.abort();
        aiAbort = new AbortController();
        try {
            const result = await callModeration('ai', snapshot, projectId, aiAbort.signal);
            if (element.value !== snapshot) return;
            if (!result.isAcceptable && result.reason)
                callbacks.onBlock(result.reason, result.flaggedBy ?? 'AI');
            else
                callbacks.onClear();
        } catch { /* ignore */ }
    }

    function onInput() {
        const text = element.value;
        clearTimers();
        if (text.length < MIN_LENGTH_FOR_CHECK) { callbacks.onClear(); return; }
        const snapshot = text;
        regexTimer = setTimeout(() => runRegex(snapshot), REGEX_DEBOUNCE_MS);
        aiTimer    = setTimeout(() => runAi(snapshot),    AI_DEBOUNCE_MS);
    }

    element.addEventListener('input', onInput);
    return () => { element.removeEventListener('input', onInput); clearTimers(); };
}

export function wireModerationField(
    input: HTMLTextAreaElement | HTMLInputElement,
    projectId: number,
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

    const detach = attachModeration(input, { onBlock: showError, onClear: clearError }, projectId);
    return () => { detach(); errorEl.remove(); };
}

// ─── Survey form wiring ───────────────────────────────────────────────────────

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

            const suggestResult = await callSuggest(textarea.value, projectId);
            const outcome    = await showRewritePopup(
                textarea.value,
                suggestResult.suggestion,
                result.reason ?? 'De inhoud is niet toegestaan op dit platform.',
            );

            if (suggestResult.suggestion === null) {
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

        const tokenInput = form.querySelector<HTMLInputElement>('input[name="moderationToken"]');
        if (tokenInput) {
            // For survey forms the token from the last moderation check is used
            tokenInput.value = '';
        }

        form.removeEventListener('submit', onSubmit);
        form.requestSubmit();
    }

    form.addEventListener('submit', onSubmit);
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

function getProjectIdFromContext(form: HTMLFormElement): number {
    // 1. data-project-id attribute on the form itself
    const attr = form.dataset.projectId;
    if (attr) return parseInt(attr, 10);
    // 2. <meta name="project-id"> set by the Razor layout
    const meta = document.querySelector<HTMLMetaElement>('meta[name="project-id"]');
    if (meta?.content) return parseInt(meta.content, 10);
    // 3. Parse from URL: /1/Survey/...
    const match = window.location.pathname.match(/^\/(\d+)\//);
    if (match) return parseInt(match[1], 10);
    return 1; // safe fallback
}