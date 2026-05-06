/**
 * speechtotext-attach.ts
 *
 * Wires a mic <button> to a target <textarea>: handles permission flow,
 * recording, silence-VAD auto-stop, hard 60s cap, transcription, and
 * append-at-cursor insertion. UI strings are Dutch (matches project default).
 *
 * Usage:
 *   attachSpeechToText(btn, textarea, projectId);
 *
 * Or declarative bulk wiring:
 *   initSpeechToTextButtons();  // scans [data-stt-target] buttons in document
 */

import {
    SpeechRecorder,
    SpeechStartError,
    transcribeAudio,
    type SpeechErrorReason,
} from './speechtotext';

// --- Public API --------------------------------------------------------------

export interface AttachOptions {
    /** Where to mount the inline status panel. Default: button.parentElement. */
    statusAnchor?: HTMLElement;
    /** Max recording length in ms. Default: 60_000. */
    maxDurationMs?: number;
    /** Auto-stop after this much silence (ms). 0 disables. Default: 1_800. */
    silenceMs?: number;
    /** RMS threshold above which we consider the user is speaking. Default: 0.04. */
    speakingThreshold?: number;
    /** Called after successful transcript insertion. */
    onTranscribed?: (transcript: string) => void;
}

const DEFAULT_OPTS: Required<Omit<AttachOptions, 'statusAnchor' | 'onTranscribed'>> = {
    maxDurationMs:     60_000,
    silenceMs:          1_800,
    speakingThreshold:   0.04,
};

// WeakMap-based idempotency: no string keys on DOM elements.
const attached = new WeakMap<HTMLButtonElement, () => void>();

/**
 * Wires one mic button to one textarea. Returns a detach() cleanup.
 * Re-attaching the same button is a no-op (returns the existing detach).
 */
export function attachSpeechToText(
    button: HTMLButtonElement,
    target: HTMLTextAreaElement,
    projectId: number,
    opts: AttachOptions = {},
): () => void {
    const existing = attached.get(button);
    if (existing) return existing;

    const config = { ...DEFAULT_OPTS, ...opts };
    const anchor = opts.statusAnchor ?? button.parentElement ?? document.body;

    let recorder:   SpeechRecorder | null = null;
    let panel:      ReturnType<typeof createStatusPanel> | null = null;
    let timerHandle:  ReturnType<typeof setInterval>  | null = null;
    let maxCapHandle: ReturnType<typeof setTimeout>   | null = null;
    let startedAt  = 0;
    let lastLoudAt = 0;
    let hasSpoken  = false;
    let stopping   = false;
    let isStarting = false;

    const onClick = async (e: Event) => {
        e.preventDefault();
        if (recorder?.isRecording || stopping) {
            void finish('user');
        } else {
            void begin();
        }
    };

    const begin = async () => {
        if (isStarting) return;
        isStarting = true;

        button.classList.add('stt-active');
        panel = createStatusPanel(anchor, () => void finish('user'), () => cancel());
        panel.setState('requesting');

        recorder = new SpeechRecorder({
            onLevel: rms => {
                if (!recorder?.isRecording) return;
                panel?.setLevel(rms);
                const now = performance.now();
                if (rms >= config.speakingThreshold) {
                    lastLoudAt = now;
                    hasSpoken  = true;
                }
                if (config.silenceMs > 0 && hasSpoken &&
                    now - lastLoudAt > config.silenceMs) {
                    void finish('silence');
                }
            },
        });

        try {
            await recorder.start();
        } catch (err) {
            const reason = err instanceof SpeechStartError ? err.reason : 'unknown';
            cleanup();
            panel?.showError(messageForStartError(reason));
            isStarting = false;
            return;
        }

        startedAt    = performance.now();
        lastLoudAt   = startedAt;
        hasSpoken    = false;
        stopping     = false;
        isStarting   = false;

        panel.setState('recording');

        timerHandle = setInterval(() => {
            if (!startedAt) return;
            panel?.setTimer(performance.now() - startedAt, config.maxDurationMs);
        }, 200);

        maxCapHandle = setTimeout(() => void finish('cap'), config.maxDurationMs);
    };

    const finish = async (_reason: 'user' | 'silence' | 'cap') => {
        if (stopping || !recorder) return;
        stopping = true;
        clearTimers();
        panel?.setState('transcribing');

        let blob: Blob;
        try {
            blob = await recorder.stop();
        } catch {
            panel?.showError('Spraakherkenning niet beschikbaar.');
            cleanup();
            return;
        }

        const result = await transcribeAudio(blob, projectId);

        if (!result.isSuccess || !result.transcript) {
            panel?.showError(result.errorMessage ?? 'Niets verstaan — probeer opnieuw.');
            cleanup();
            return;
        }

        insertAtCursor(target, result.transcript);
        opts.onTranscribed?.(result.transcript);
        panel?.dismiss();
        cleanup();
    };

    const cancel = () => {
        clearTimers();
        recorder?.release();
        panel?.dismiss();
        cleanup();
    };

    const cleanup = () => {
        clearTimers();
        button.classList.remove('stt-active');
        recorder  = null;
        panel     = null;
        startedAt = 0;
        stopping  = false;
        hasSpoken = false;
    };

    const clearTimers = () => {
        if (timerHandle  != null) { clearInterval(timerHandle);  timerHandle  = null; }
        if (maxCapHandle != null) { clearTimeout(maxCapHandle);   maxCapHandle = null; }
    };

    button.addEventListener('click', onClick);

    const detach = () => {
        button.removeEventListener('click', onClick);
        cancel();
        attached.delete(button);
    };

    attached.set(button, detach);
    return detach;
}

/**
 * Scan `root` for `[data-stt-target]` buttons and wire each to its target
 * textarea. Idempotent — re-running is safe.
 * Reads projectId from <meta name="project-id">.
 */
export function initSpeechToTextButtons(root: ParentNode = document): void {
    const projectId = readProjectId();
    if (projectId == null) return;
    root.querySelectorAll<HTMLButtonElement>('button[data-stt-target]').forEach(btn => {
        const selector = btn.dataset.sttTarget;
        if (!selector) return;
        const target = document.querySelector<HTMLTextAreaElement>(selector);
        if (target) attachSpeechToText(btn, target, projectId);
    });
}

// --- Helpers -----------------------------------------------------------------

function readProjectId(): number | null {
    const raw = document.querySelector<HTMLMetaElement>('meta[name="project-id"]')?.content?.trim();
    if (!raw) return null;
    const n = Number(raw);
    return Number.isFinite(n) && n > 0 ? n : null;
}

function insertAtCursor(target: HTMLTextAreaElement, text: string): void {
    const trimmed = text.trim();
    if (!trimmed) return;

    const max   = target.maxLength > 0 ? target.maxLength : Infinity;
    const start = target.selectionStart ?? target.value.length;
    const end   = target.selectionEnd   ?? target.value.length;

    const before = target.value.slice(0, start);
    const after  = target.value.slice(end);

    const insert =
        (before.length > 0 && !/\s$/.test(before) ? ' ' : '') +
        trimmed +
        (after.length  > 0 && !/^\s/.test(after)  ? ' ' : '');

    let next = before + insert + after;
    if (next.length > max) next = next.slice(0, max);

    target.value = next;
    const caret = Math.min(before.length + insert.length, max);
    try { target.setSelectionRange(caret, caret); } catch { /* ignore in readonly/number inputs */ }

    target.dispatchEvent(new Event('input', { bubbles: true }));
    target.focus({ preventScroll: true });
}

function messageForStartError(reason: SpeechErrorReason): string {
    switch (reason) {
        case 'permission-denied': return 'Geef toestemming voor de microfoon om te spreken.';
        case 'no-microphone':     return 'Geen microfoon gevonden.';
        case 'unsupported':       return 'Spraakherkenning werkt niet in deze browser.';
        default:                  return 'Spraakherkenning niet beschikbaar.';
    }
}

// --- Status panel (factory) --------------------------------------------------

type PanelState = 'requesting' | 'recording' | 'transcribing';

function createStatusPanel(
    anchor: HTMLElement,
    onStop:   () => void,
    onCancel: () => void,
) {
    const el = document.createElement('div');
    el.className =
        'stt-panel z-40 mt-2 rounded-2xl border border-base-300 bg-base-100 shadow-md ' +
        'px-3 py-2 flex items-center gap-3 text-xs text-base-content';
    el.setAttribute('role',      'status');
    el.setAttribute('aria-live', 'polite');

    el.innerHTML = `
        <span class="stt-pulse w-2 h-2 rounded-full bg-base-content/40 inline-block" aria-hidden="true"></span>
        <span class="stt-status font-medium">Microfoon toegang nodig…</span>
        <span class="stt-meter inline-flex items-end gap-0.5 h-3 ml-1" aria-hidden="true">
            <span class="w-1 bg-base-content/30 rounded-sm" style="height:25%"></span>
            <span class="w-1 bg-base-content/30 rounded-sm" style="height:50%"></span>
            <span class="w-1 bg-base-content/30 rounded-sm" style="height:75%"></span>
            <span class="w-1 bg-base-content/30 rounded-sm" style="height:100%"></span>
        </span>
        <span class="stt-timer ml-auto tabular-nums text-base-content/70">1:00</span>
        <button type="button" class="stt-stop hidden btn btn-xs btn-primary rounded-full">Stop</button>
        <button type="button" class="stt-cancel hidden btn btn-xs btn-ghost rounded-full" aria-label="Annuleren">✕</button>
    `;

    const statusEl  = q<HTMLElement>(el, '.stt-status');
    const timerEl   = q<HTMLElement>(el, '.stt-timer');
    const pulse     = q<HTMLElement>(el, '.stt-pulse');
    const meterBars = Array.from(el.querySelectorAll<HTMLElement>('.stt-meter > span'));
    const stopBtn   = q<HTMLButtonElement>(el, '.stt-stop');
    const cancelBtn = q<HTMLButtonElement>(el, '.stt-cancel');

    stopBtn.addEventListener('click',   onStop);
    cancelBtn.addEventListener('click', onCancel);

    anchor.appendChild(el);

    let dismissTimer: ReturnType<typeof setTimeout> | null = null;

    return {
        setState(state: PanelState): void {
            switch (state) {
                case 'requesting':
                    statusEl.textContent = 'Microfoon toegang nodig…';
                    stopBtn.classList.add('hidden');
                    cancelBtn.classList.add('hidden');
                    pulse.className = 'stt-pulse w-2 h-2 rounded-full bg-base-content/40 inline-block';
                    break;
                case 'recording':
                    statusEl.textContent = 'Aan het opnemen';
                    stopBtn.classList.remove('hidden');
                    cancelBtn.classList.remove('hidden');
                    pulse.className = 'stt-pulse w-2 h-2 rounded-full bg-error inline-block animate-pulse';
                    break;
                case 'transcribing':
                    statusEl.textContent = 'Spraak omzetten…';
                    stopBtn.classList.add('hidden');
                    cancelBtn.classList.add('hidden');
                    pulse.className = 'stt-pulse w-2 h-2 rounded-full bg-base-content/40 inline-block';
                    break;
            }
        },

        setTimer(elapsedMs: number, maxMs: number): void {
            const sec = Math.floor(Math.max(0, maxMs - elapsedMs) / 1000);
            timerEl.textContent = `${Math.floor(sec / 60)}:${(sec % 60).toString().padStart(2, '0')}`;
        },

        setLevel(rms: number): void {
            const lit = Math.round(Math.min(1, rms / 0.1) * meterBars.length);
            meterBars.forEach((bar, i) => {
                bar.classList.toggle('bg-primary',         i < lit);
                bar.classList.toggle('bg-base-content/30', i >= lit);
            });
        },

        showError(message: string): void {
            statusEl.textContent = message;
            stopBtn.classList.add('hidden');
            cancelBtn.classList.add('hidden');
            pulse.className = 'stt-pulse w-2 h-2 rounded-full bg-base-content/40 inline-block';
            meterBars.forEach(b => { b.classList.remove('bg-primary'); b.classList.add('bg-base-content/20'); });
            el.classList.add('border-error', 'text-error');
            if (dismissTimer != null) clearTimeout(dismissTimer);
            dismissTimer = setTimeout(() => this.dismiss(), 4000);
        },

        dismiss(): void {
            if (dismissTimer != null) { clearTimeout(dismissTimer); dismissTimer = null; }
            stopBtn.removeEventListener('click',   onStop);
            cancelBtn.removeEventListener('click', onCancel);
            el.remove();
        },
    };
}

function q<T extends HTMLElement>(root: HTMLElement, sel: string): T {
    const el = root.querySelector<T>(sel);
    if (!el) throw new Error(`STT panel missing element: ${sel}`);
    return el;
}