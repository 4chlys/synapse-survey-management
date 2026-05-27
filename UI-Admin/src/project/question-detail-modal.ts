import { Chart } from 'chart.js';
import { renderInto as renderPie } from '../widgets/widget-question-single-pie';
import type { SingleChoiceStats } from '../widgets/widget-question-single-pie';
import { renderInto as renderBar } from '../widgets/widget-question-multi-bar';
import type { MultipleChoiceStats } from '../widgets/widget-question-multi-bar';
import { renderInto as renderLikert } from '../widgets/widget-question-likert-bar';
import type { LikertStats } from '../widgets/widget-question-likert-bar';
import type { ModalStrings, OpenAnswerPage } from './types';

let activeChart: Chart | null = null;

export function init(): void {
    const questionsList = document.getElementById('questions-list');
    if (!questionsList) return;

    const dialog = document.getElementById('question-detail-modal') as HTMLDialogElement | null;
    if (!dialog) return;

    const projectId = Number(questionsList.dataset.projectId ?? '0');

    questionsList.addEventListener('click', (e) => {
        const btn = (e.target as HTMLElement).closest<HTMLElement>('.question-stats-btn');
        if (!btn) return;

        const questionId = Number(btn.dataset.questionId ?? '0');
        const format = btn.dataset.format ?? '';
        const text = btn.dataset.text ?? '';

        const titleEl = document.getElementById('question-detail-title');
        const canvas = document.getElementById('question-detail-chart') as HTMLCanvasElement | null;
        const answersDiv = document.getElementById('question-detail-answers');
        const statsDiv = document.getElementById('question-detail-stats');

        if (titleEl) titleEl.textContent = text;
        if (statsDiv) statsDiv.innerHTML = '';
        if (answersDiv) {
            answersDiv.classList.add('hidden');
            answersDiv.innerHTML = '';
        }
        if (canvas) {
            canvas.classList.remove('hidden');
            canvas.parentElement?.classList.remove('hidden');
        }

        // Destroy previous Chart instance to prevent memory leak
        if (activeChart) {
            activeChart.destroy();
            activeChart = null;
        }

        dialog.showModal();

        const strings: ModalStrings = {
            loadFailed:    dialog.dataset.i18nLoadFailed    ?? '',
            responsesUnit: dialog.dataset.i18nResponsesUnit ?? '',
            xAxisLabel:    dialog.dataset.i18nXAxisLabel    ?? '',
        };

        if (format === 'Open') {
            if (canvas) {
                canvas.classList.add('hidden');
                canvas.parentElement?.classList.add('hidden');
            }
            if (answersDiv) answersDiv.classList.remove('hidden');
            void fetchAndRenderOpenAnswers(projectId, questionId, answersDiv, statsDiv, dialog, strings);
            return;
        }

        void fetchAndRender(projectId, questionId, format, canvas, statsDiv, strings);
    });
}

async function fetchAndRender(
    projectId: number,
    questionId: number,
    format: string,
    canvas: HTMLCanvasElement | null,
    statsDiv: HTMLElement | null,
    strings: ModalStrings,
): Promise<void> {
    const suffix = format === 'SingleChoice' ? 'single-stats'
        : format === 'MultipleChoice' ? 'multi-stats'
        : 'likert-stats';
    const url = `/api/projects/${projectId}/stats/questions/${questionId}/${suffix}`;

    let res: Response;
    try {
        res = await fetch(url);
    } catch {
        if (statsDiv) statsDiv.innerHTML = `<p class="text-error text-xs">${strings.loadFailed}</p>`;
        if (canvas) canvas.classList.add('hidden');
        return;
    }

    if (!res.ok) {
        if (statsDiv) statsDiv.innerHTML = `<p class="text-error text-xs">${strings.loadFailed}</p>`;
        if (canvas) canvas.classList.add('hidden');
        return;
    }

    if (format === 'SingleChoice') {
        const stats = await res.json() as SingleChoiceStats;
        if (statsDiv) statsDiv.innerHTML = `<span>${stats.totalResponses} ${strings.responsesUnit}</span>`;
        if (stats.totalResponses === 0) {
            if (canvas) canvas.classList.add('hidden');
            return;
        }
        if (canvas) activeChart = renderPie(canvas, stats);
    } else if (format === 'MultipleChoice') {
        const stats = await res.json() as MultipleChoiceStats;
        if (statsDiv) statsDiv.innerHTML = `<span>${stats.totalResponses} ${strings.responsesUnit}</span>`;
        if (stats.totalResponses === 0) {
            if (canvas) canvas.classList.add('hidden');
            return;
        }
        if (canvas) activeChart = renderBar(canvas, stats, strings.xAxisLabel);
    } else {
        const stats = await res.json() as LikertStats;
        if (statsDiv) statsDiv.innerHTML = `<span>${stats.totalResponses} ${strings.responsesUnit}</span>`;
        if (stats.totalResponses === 0) {
            if (canvas) canvas.classList.add('hidden');
            return;
        }
        if (canvas) activeChart = renderLikert(canvas, stats);
    }
}

async function fetchAndRenderOpenAnswers(
    projectId: number,
    questionId: number,
    container: HTMLElement | null,
    statsDiv: HTMLElement | null,
    dialog: HTMLDialogElement,
    strings: ModalStrings,
): Promise<void> {
    if (!container) return;

    const noAnswersText = dialog.dataset.i18nNoAnswers ?? '';
    const loadMoreText = dialog.dataset.i18nLoadMore ?? '';
    const loadFailedText = strings.loadFailed;
    const take = 50;

    container.innerHTML = '';

    let res: Response;
    try {
        res = await fetch(
            `/api/projects/${projectId}/stats/questions/${questionId}/open-answers?skip=0&take=${take}`
        );
    } catch {
        const err = document.createElement('p');
        err.className = 'text-error text-xs';
        err.textContent = loadFailedText;
        container.appendChild(err);
        return;
    }

    if (!res.ok) {
        const err = document.createElement('p');
        err.className = 'text-error text-xs';
        err.textContent = loadFailedText;
        container.appendChild(err);
        return;
    }

    const page = await res.json() as OpenAnswerPage;

    if (statsDiv) statsDiv.innerHTML = `<span>${page.totalAnswers} ${strings.responsesUnit}</span>`;

    if (page.totalAnswers === 0) {
        const empty = document.createElement('p');
        empty.className = 'text-base-content/40 text-xs italic';
        empty.textContent = noAnswersText;
        container.appendChild(empty);
        return;
    }

    const list = document.createElement('div');
    list.className = 'flex flex-col gap-2 overflow-y-auto max-h-72 mb-2';
    page.answers.forEach(a => appendAnswerCard(list, a));
    container.appendChild(list);

    let skip = page.answers.length;

    if (skip < page.totalAnswers) {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-ghost btn-sm w-full';
        btn.textContent = loadMoreText;
        container.appendChild(btn);

        btn.addEventListener('click', async () => {
            btn.disabled = true;
            let moreRes: Response;
            try {
                moreRes = await fetch(
                    `/api/projects/${projectId}/stats/questions/${questionId}/open-answers?skip=${skip}&take=${take}`
                );
            } catch {
                btn.disabled = false;
                return;
            }
            if (!moreRes.ok) {
                btn.disabled = false;
                return;
            }
            const morePage = await moreRes.json() as OpenAnswerPage;
            morePage.answers.forEach(a => appendAnswerCard(list, a));
            skip += morePage.answers.length;
            if (skip >= morePage.totalAnswers) {
                btn.remove();
            } else {
                btn.disabled = false;
            }
        });
    }
}

function appendAnswerCard(list: HTMLElement, answer: { value: string; createdAt: string }): void {
    const card = document.createElement('div');
    card.className = 'card card-compact bg-base-200';
    const body = document.createElement('div');
    body.className = 'card-body';
    const text = document.createElement('p');
    text.className = 'text-sm';
    text.textContent = answer.value;
    const date = document.createElement('p');
    date.className = 'text-xs text-base-content/40';
    date.textContent = new Date(answer.createdAt).toLocaleDateString(undefined, {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
    });
    body.appendChild(text);
    body.appendChild(date);
    card.appendChild(body);
    list.appendChild(card);
}

