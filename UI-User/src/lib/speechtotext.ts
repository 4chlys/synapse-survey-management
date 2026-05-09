import { t, currentCulture } from './i18n';

/**
 * speechtotext.ts
 *
 * Browser MediaRecorder wrapper + transcription API client.
 * UI/state-machine glue lives in speechtotext-attach.ts.
 */

import { getCsrfToken, fetchWithTimeout } from '../ideation/ai-support-base';

// --- Types -------------------------------------------------------------------

export interface TranscribeResult {
    transcript: string | null;
    errorMessage: string | null;
    /** Machine-readable error code from the server. UI maps this to a localized message. */
    errorCode?: string | null;
    isSuccess: boolean;
}

const SERVER_ERROR_CODE_KEYS: Record<string, keyof typeof window.__i18n.strings> = {
    unsupported_format:  'speechToTextErrorUnsupportedFormat',
    empty_audio:         'speechToTextEmptyRecording',
    too_long:            'speechToTextErrorTooLong',
    no_speech:           'speechToTextErrorNothingHeard',
    service_unavailable: 'speechToTextUnavailable',
    invalid_audio_data:  'speechToTextErrorInvalidAudioData',
    unexpected_response: 'speechToTextErrorUnexpectedResponse',
};

function localizedErrorFromServer(result: TranscribeResult): TranscribeResult {
    if (result.isSuccess || !result.errorCode) return result;
    const key = SERVER_ERROR_CODE_KEYS[result.errorCode];
    return key
        ? { ...result, errorMessage: t(key) }
        : result;
}

/** Why start() failed in a way the UI can map to a user message. */
export type SpeechErrorReason =
    | 'permission-denied'
    | 'no-microphone'
    | 'unsupported'
    | 'unknown';

export class SpeechStartError extends Error {
    constructor(public readonly reason: SpeechErrorReason, message: string) {
        super(message);
        this.name = 'SpeechStartError';
    }
}

// --- MIME type resolution ----------------------------------------------------

/**
 * Returns the best supported audio MIME type for MediaRecorder in this browser.
 * Prefers WebM/Opus (Chrome, Edge, Firefox) then falls back to OGG/Opus
 * and explicit MP4/AAC for Safari.
 */
function getSupportedMimeType(): string {
    if (typeof MediaRecorder === 'undefined') return '';
    const candidates = [
        'audio/webm;codecs=opus',
        'audio/webm',
        'audio/ogg;codecs=opus',
        'audio/ogg',
        'audio/mp4;codecs=mp4a.40.2',
        'audio/mp4',
    ];
    for (const type of candidates) {
        if (MediaRecorder.isTypeSupported(type)) return type;
    }
    return '';
}

// --- SpeechRecorder ----------------------------------------------------------

export interface SpeechRecorderOptions {
    /**
     * Optional callback receiving a normalised RMS level (0-1) about every
     * 100 ms while recording. Used for VAD silence detection and level meters.
     */
    onLevel?: (rms: number) => void;
}

/**
 * Wraps the browser MediaRecorder API into a simple start/stop interface,
 * with optional Web Audio level metering for silence detection.
 *
 * Call `start()` to request microphone access and begin recording.
 * Call `stop()` to end the recording and receive the audio as a Blob.
 * Call `release()` to stop the mic stream without a recording (e.g. on cancel).
 */
export class SpeechRecorder {
    private _mediaRecorder: MediaRecorder | null = null;
    private _chunks: Blob[] = [];
    private _stream: MediaStream | null = null;
    private _mimeType = '';

    private _audioCtx: AudioContext | null = null;
    private _analyser: AnalyserNode | null = null;
    private _source: MediaStreamAudioSourceNode | null = null;
    private _levelTimer: ReturnType<typeof setInterval> | null = null;

    constructor(private readonly _options: SpeechRecorderOptions = {}) {}

    /** Whether a recording is currently in progress. */
    get isRecording(): boolean {
        return this._mediaRecorder?.state === 'recording';
    }

    /**
     * Requests microphone permission and starts recording.
     * Throws SpeechStartError on permission/support issues so the UI can
     * show the right Dutch message.
     */
    async start(): Promise<void> {
        if (this.isRecording) return;

        if (typeof MediaRecorder === 'undefined' ||
            !navigator.mediaDevices?.getUserMedia) {
            throw new SpeechStartError(
                'unsupported',
                'MediaRecorder or getUserMedia is unavailable in this browser.',
            );
        }

        try {
            this._stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        } catch (err) {
            const reason = mapGetUserMediaError(err);
            throw new SpeechStartError(reason, (err as Error)?.message ?? 'getUserMedia failed');
        }

        this._mimeType = getSupportedMimeType();
        this._chunks = [];

        const options: MediaRecorderOptions = this._mimeType
            ? { mimeType: this._mimeType }
            : {};

        this._mediaRecorder = new MediaRecorder(this._stream, options);
        this._mediaRecorder.addEventListener('dataavailable', (e: BlobEvent) => {
            if (e.data.size > 0) this._chunks.push(e.data);
        });

        this._setupLevelMetering();
        this._mediaRecorder.start(250); // collect data every 250 ms
    }

    /**
     * Stops the current recording and returns the collected audio as a Blob.
     * Also stops and releases the microphone stream and audio analyser.
     */
    stop(): Promise<Blob> {
        return new Promise((resolve, reject) => {
            if (!this._mediaRecorder || this._mediaRecorder.state === 'inactive') {
                reject(new Error('No active recording.'));
                return;
            }

            this._mediaRecorder.addEventListener('stop', () => {
                const mimeType = this._mimeType || 'audio/webm';
                const blob = new Blob(this._chunks, { type: mimeType });
                this._chunks = [];
                this._teardown();
                resolve(blob);
            }, { once: true });

            this._mediaRecorder.stop();
        });
    }

    /**
     * Stops the microphone stream without completing a recording.
     * Use this to clean up after a cancelled recording.
     */
    release(): void {
        if (this._mediaRecorder?.state === 'recording') {
            try { this._mediaRecorder.stop(); } catch { /* ignore */ }
        }
        this._teardown();
        this._chunks = [];
    }

    private _setupLevelMetering(): void {
        if (!this._options.onLevel || !this._stream) return;
        try {
            const Ctor: typeof AudioContext =
                window.AudioContext ?? (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
            if (!Ctor) return;
            this._audioCtx = new Ctor();
            this._source = this._audioCtx.createMediaStreamSource(this._stream);
            this._analyser = this._audioCtx.createAnalyser();
            this._analyser.fftSize = 1024;
            this._source.connect(this._analyser);

            const buf = new Uint8Array(this._analyser.fftSize);
            this._levelTimer = setInterval(() => {
                if (!this._analyser) return;
                this._analyser.getByteTimeDomainData(buf);
                let sumSquares = 0;
                for (let i = 0; i < buf.length; i++) {
                    const v = (buf[i] - 128) / 128; // -1..1
                    sumSquares += v * v;
                }
                const rms = Math.sqrt(sumSquares / buf.length);
                this._options.onLevel?.(rms);
            }, 100);
        } catch {
            // Web Audio not available — VAD will be disabled, recorder still works.
            this._teardownAnalyser();
        }
    }

    private _teardown(): void {
        this._teardownAnalyser();
        this._stream?.getTracks().forEach(t => t.stop());
        this._stream = null;
    }

    private _teardownAnalyser(): void {
        if (this._levelTimer != null) {
            clearInterval(this._levelTimer);
            this._levelTimer = null;
        }
        try { this._source?.disconnect(); } catch { /* ignore */ }
        try { this._analyser?.disconnect(); } catch { /* ignore */ }
        this._source = null;
        this._analyser = null;
        if (this._audioCtx && this._audioCtx.state !== 'closed') {
            this._audioCtx.close().catch(() => { /* ignore */ });
        }
        this._audioCtx = null;
    }
}

function mapGetUserMediaError(err: unknown): SpeechErrorReason {
    const name = (err as { name?: string })?.name ?? '';
    if (name === 'NotAllowedError' || name === 'SecurityError') return 'permission-denied';
    if (name === 'NotFoundError' || name === 'OverconstrainedError') return 'no-microphone';
    if (name === 'NotSupportedError') return 'unsupported';
    return 'unknown';
}

// --- API client --------------------------------------------------------------

/**
 * Converts an audio Blob to base64 and sends it to the transcription endpoint.
 * Resolved TranscribeResult — never rejects; errors are returned inline.
 */
export async function transcribeAudio(
    audioBlob: Blob,
    projectId: number,
): Promise<TranscribeResult> {
    if (audioBlob.size === 0) {
        return { isSuccess: false, transcript: null, errorMessage: t('speechToTextEmptyRecording') };
    }

    try {
        const base64 = await blobToBase64(audioBlob);
        const mimeType = audioBlob.type || 'audio/webm;codecs=opus';

        const response = await fetchWithTimeout(
            `/${projectId}/api/SpeechToTextApi/transcribe`,
            {
                method:  'POST',
                headers: {
                    'Content-Type':             'application/json',
                    'RequestVerificationToken': getCsrfToken(),
                },
                body: JSON.stringify({ audioData: base64, mimeType, language: currentCulture().split('-')[0] }),
            },
        );

        if (response.status === 429) {
            return {
                isSuccess:    false,
                transcript:   null,
                errorMessage: t('speechToTextTooManyRequests'),
            };
        }

        if (response.status === 413) {
            return {
                isSuccess:    false,
                transcript:   null,
                errorMessage: t('speechToTextErrorTooLong'),
            };
        }

        if (!response.ok) {
            return {
                isSuccess:    false,
                transcript:   null,
                errorMessage: t('speechToTextUnavailable'),
            };
        }

        return localizedErrorFromServer(await response.json() as TranscribeResult);
    } catch {
        return {
            isSuccess:    false,
            transcript:   null,
            errorMessage: t('speechToTextUnavailable'),
        };
    }
}

// --- Helpers -----------------------------------------------------------------

function blobToBase64(blob: Blob): Promise<string> {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => {
            const dataUrl = reader.result as string;
            // Strip the "data:audio/webm;base64," prefix; API expects raw base64.
            resolve(dataUrl.split(',')[1] ?? '');
        };
        reader.onerror = () => reject(reader.error);
        reader.readAsDataURL(blob);
    });
}