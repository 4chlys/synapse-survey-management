import { Chart } from 'chart.js';
import { POLL_INTERVAL_MS } from '../admin/dashboard';
import { createDownloadButton, downloadCanvasPng } from './utils';

export interface QuestionLookupItem {
    questionId: number;
    text: string;
}

export interface WidgetStrings {
    loadFailed: string;
    noAnswers: string;
    noQuestion: string;
}

export type QuestionFormat = 'SingleChoice' | 'MultipleChoice' | 'Likert';

export type RenderFn = (
    body: HTMLElement,
    projectId: number,
    questionId: number,
    apiBaseUrl: string,
    strings: WidgetStrings,
    animate: boolean,
) => Promise<void>;

export interface QuestionWidgetOptions {
    format: QuestionFormat;
    fileName: string;
    render: RenderFn;
}

function antiForgeryToken(): string {
    return document.querySelector<HTMLInputElement>(
        'input[name="__RequestVerificationToken"]')?.value ?? '';
}

async function persistSelection(projectId: number, position: number, questionId: number): Promise<void> {
    if (position < 0) return;

    const body = new URLSearchParams({
        projectId: String(projectId),
        position: String(position),
        questionId: String(questionId),
    });

    try {
        await fetch('/Project/SetWidgetQuestion', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': antiForgeryToken(),
            },
            body,
        });
    } catch {
        // The chart still updates; a failed persist only means the choice
        // is not remembered on the next visit.
    }
}

// Clears the body's render output (chart or message) while preserving the persistent
// download button so it survives question-select changes.
function clearBody(body: HTMLElement): void {
    const existing = body.querySelector<HTMLCanvasElement>('canvas');
    if (existing) Chart.getChart(existing)?.destroy();
    for (const child of Array.from(body.children)) {
        if (!child.hasAttribute('data-download-btn')) child.remove();
    }
}

export function showMessage(body: HTMLElement, text: string, kind: 'error' | 'info' | 'muted'): void {
    clearBody(body);
    body.querySelector('[data-download-btn]')?.classList.add('hidden');
    const p = document.createElement('p');
    p.className = kind === 'error'
        ? 'text-error text-xs'
        : kind === 'info'
            ? 'alert alert-info text-xs text-center mt-4'
            : 'text-base-content/40 text-xs';
    p.textContent = text;
    body.appendChild(p);
}

export function freshCanvas(body: HTMLElement): HTMLCanvasElement {
    clearBody(body);
    const canvas = document.createElement('canvas');
    body.appendChild(canvas);
    body.querySelector('[data-download-btn]')?.classList.remove('hidden');
    return canvas;
}

export function initQuestionWidget(el: HTMLElement, options: QuestionWidgetOptions): () => void {
    const position = Number(el.dataset.position ?? '-1');
    const projectId = Number(el.dataset.projectId ?? '0');
    const apiBaseUrl = el.dataset.apiBase ?? '';
    const initialQuestionId = Number(el.dataset.questionId ?? '0');
    const strings: WidgetStrings = {
        loadFailed: el.dataset.i18nLoadFailed ?? '',
        noAnswers: el.dataset.i18nNoAnswers ?? '',
        noQuestion: el.dataset.i18nNoQuestion ?? '',
    };

    const select = el.querySelector<HTMLSelectElement>('[data-question-select]');
    const body = el.querySelector<HTMLElement>('[data-widget-body]');
    if (!select || !body) return () => { /* nothing to clean up */ };

    let pollTimer: ReturnType<typeof setInterval> | undefined;

    // Attach the download button once; freshCanvas/showMessage toggle its visibility per render.
    const downloadBtn = createDownloadButton(() => {
        const canvas = body.querySelector<HTMLCanvasElement>('canvas');
        if (canvas) downloadCanvasPng(canvas, options.fileName);
    });
    downloadBtn.setAttribute('data-download-btn', '');
    downloadBtn.classList.add('hidden');
    body.appendChild(downloadBtn);

    void (async () => {
        let questions: QuestionLookupItem[];
        try {
            const res = await fetch(
                `${apiBaseUrl}/api/projects/${projectId}/stats/questions?format=${options.format}`);
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            questions = await res.json() as QuestionLookupItem[];
        } catch {
            select.classList.add('hidden');
            showMessage(body, strings.loadFailed, 'error');
            return;
        }

        if (questions.length === 0) {
            select.classList.add('hidden');
            showMessage(body, strings.noQuestion, 'muted');
            return;
        }

        for (const q of questions) {
            const opt = document.createElement('option');
            opt.value = String(q.questionId);
            opt.textContent = q.text;
            select.appendChild(opt);
        }

        let selectedId = questions.some(q => q.questionId === initialQuestionId)
            ? initialQuestionId
            : questions[0].questionId;
        select.value = String(selectedId);

        // First paint and question switches animate; the background poll refreshes silently.
        await options.render(body, projectId, selectedId, apiBaseUrl, strings, true);

        select.addEventListener('change', () => {
            selectedId = Number(select.value);
            void persistSelection(projectId, position, selectedId);
            void options.render(body, projectId, selectedId, apiBaseUrl, strings, true);
        });

        pollTimer = setInterval(() => {
            void options.render(body, projectId, selectedId, apiBaseUrl, strings, false);
        }, POLL_INTERVAL_MS);
    })();

    return () => {
        if (pollTimer) clearInterval(pollTimer);
        const canvas = body.querySelector<HTMLCanvasElement>('canvas');
        if (canvas) Chart.getChart(canvas)?.destroy();
    };
}
