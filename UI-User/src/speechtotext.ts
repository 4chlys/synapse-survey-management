/**
 * speechtotext.ts
 *
 * Speech-to-text framework using the browser's MediaRecorder API and the
 * Gemini-powered backend transcription endpoint.
 *
 * Usage:
 *   const recorder = new SpeechRecorder();
 *   await recorder.start();
 *   // ... user speaks ...
 *   const blob = await recorder.stop();
 *   const result = await transcribeAudio(blob, projectId);
 *   if (result.transcript) textarea.value = result.transcript;
 */

// ─── Types ────────────────────────────────��───────────────────────��───────────

export interface TranscribeResult {
    transcript: string | null;
    errorMessage: string | null;
    isSuccess: boolean;
}

// ─── MIME type resolution ─────────────────────────────────────────────────────

/**
 * Returns the best supported audio MIME type for MediaRecorder in this browser.
 * Prefers WebM/Opus (Chrome, Edge, Firefox) then falls back to OGG/Opus and MP4.
 */
function getSupportedMimeType(): string {
    const candidates = [
        'audio/webm;codecs=opus',
        'audio/webm',
        'audio/ogg;codecs=opus',
        'audio/ogg',
        'audio/mp4',
    ];
    for (const type of candidates) {
        if (MediaRecorder.isTypeSupported(type)) return type;
    }
    return ''; // browser will pick its own default
}

// ─── SpeechRecorder ───────────────────────────────────────────────────────────

/**
 * Wraps the browser MediaRecorder API into a simple start/stop interface.
 *
 * Call `start()` to request microphone access and begin recording.
 * Call `stop()` to end the recording and receive the audio as a Blob.
 * Call `release()` to stop the mic stream without a recording (e.g. on cancel).
 *
 * The instance can be reused for multiple recordings.
 */
export class SpeechRecorder {
    private _mediaRecorder: MediaRecorder | null = null;
    private _chunks: Blob[] = [];
    private _stream: MediaStream | null = null;
    private _mimeType = '';

    /** Whether a recording is currently in progress. */
    get isRecording(): boolean {
        return this._mediaRecorder?.state === 'recording';
    }

    /**
     * Requests microphone permission and starts recording.
     * Throws if permission is denied or MediaRecorder is unavailable.
     */
    async start(): Promise<void> {
        if (this.isRecording) return;

        this._stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        this._mimeType = getSupportedMimeType();
        this._chunks = [];

        const options: MediaRecorderOptions = this._mimeType
            ? { mimeType: this._mimeType }
            : {};

        this._mediaRecorder = new MediaRecorder(this._stream, options);
        this._mediaRecorder.addEventListener('dataavailable', (e: BlobEvent) => {
            if (e.data.size > 0) this._chunks.push(e.data);
        });

        this._mediaRecorder.start(250); // collect data every 250 ms
    }

    /**
     * Stops the current recording and returns the collected audio as a Blob.
     * Also stops and releases the microphone stream.
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
                this._releaseStream();
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
            this._mediaRecorder.stop();
        }
        this._releaseStream();
        this._chunks = [];
    }

    private _releaseStream(): void {
        this._stream?.getTracks().forEach(t => t.stop());
        this._stream = null;
    }
}

// ─── API client ───────────────────────────────────────────────────────────────

function getCsrfToken(): string {
    return document.querySelector<HTMLMetaElement>(
        'meta[name="__RequestVerificationToken"]',
    )?.content ?? '';
}

/**
 * Converts an audio Blob to base64 and sends it to the transcription endpoint.
 *
 * @param audioBlob  - The audio blob produced by SpeechRecorder.stop().
 * @param projectId  - The current project ID (used to build the API path).
 * @returns Resolved TranscribeResult — never rejects; errors are returned inline.
 */
export async function transcribeAudio(
    audioBlob: Blob,
    projectId: number,
): Promise<TranscribeResult> {
    if (audioBlob.size === 0) {
        return { isSuccess: false, transcript: null, errorMessage: 'Lege opname ontvangen.' };
    }

    try {
        const base64 = await blobToBase64(audioBlob);
        const mimeType = audioBlob.type || 'audio/webm;codecs=opus';

        const response = await fetch(
            `/${projectId}/api/SpeechToTextApi/transcribe`,
            {
                method:  'POST',
                headers: {
                    'Content-Type':             'application/json',
                    'RequestVerificationToken': getCsrfToken(),
                },
                body: JSON.stringify({ audioData: base64, mimeType }),
            },
        );

        if (response.status === 429) {
            return {
                isSuccess:    false,
                transcript:   null,
                errorMessage: 'Te veel verzoeken. Wacht even en probeer opnieuw.',
            };
        }

        if (!response.ok) {
            return {
                isSuccess:    false,
                transcript:   null,
                errorMessage: 'Spraakherkenning is momenteel niet beschikbaar.',
            };
        }

        const data = await response.json() as TranscribeResult;
        return data;
    } catch {
        return {
            isSuccess:    false,
            transcript:   null,
            errorMessage: 'Spraakherkenning is momenteel niet beschikbaar.',
        };
    }
}

// ─── Helpers ──────────────────────────────────────────────────────────────���──

function blobToBase64(blob: Blob): Promise<string> {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => {
            const dataUrl = reader.result as string;
            // Strip the "data:audio/webm;base64," prefix — API expects raw base64.
            resolve(dataUrl.split(',')[1] ?? '');
        };
        reader.onerror = () => reject(reader.error);
        reader.readAsDataURL(blob);
    });
}
