import { Chart, BarController, BarElement, LinearScale, CategoryScale, Tooltip } from 'chart.js';
import { initQuestionWidget, freshCanvas, showMessage } from '../lib/question-widget-shell';
import type { WidgetStrings } from '../lib/question-widget-shell';

Chart.register(BarController, BarElement, LinearScale, CategoryScale, Tooltip);

interface LikertLabels {
    l1: string;
    l2: string;
    l3: string;
    l4: string;
    l5: string;
}

const DEFAULT_LABELS: LikertLabels = {
    l1: 'Strongly disagree (1)',
    l2: 'Disagree (2)',
    l3: 'Neutral (3)',
    l4: 'Agree (4)',
    l5: 'Strongly agree (5)',
};

interface LikertPointCount {
    point: number;
    count: number;
}

export interface LikertStats {
    questionId: number;
    totalResponses: number;
    distribution: LikertPointCount[];
}

// Tailwind palette hex values — do not import Tailwind into TS
const COLOR_STRONGLY_DISAGREE = '#ef4444'; // red-500
const COLOR_DISAGREE          = '#f59e0b'; // amber-500
const COLOR_NEUTRAL           = '#9ca3af'; // gray-400
const COLOR_AGREE             = '#86efac'; // green-300
const COLOR_STRONGLY_AGREE    = '#22c55e'; // green-500

function getCount(distribution: LikertPointCount[], point: number): number {
    return distribution.find(d => d.point === point)?.count ?? 0;
}

// Datasets are ordered so Chart.js stacks from the zero line outward on each side:
//   neg side (innermost→outermost): neutral-neg, disagree, strongly-disagree
//   pos side (innermost→outermost): neutral-pos, agree, strongly-agree
// rawCounts is parallel to this dataset order and holds the actual response count
// for each segment (neutral halves both map back to the full c3).
export function renderInto(canvasEl: HTMLCanvasElement, stats: LikertStats, labels: LikertLabels = DEFAULT_LABELS): Chart {
    const c1 = getCount(stats.distribution, 1);
    const c2 = getCount(stats.distribution, 2);
    const c3 = getCount(stats.distribution, 3);
    const c4 = getCount(stats.distribution, 4);
    const c5 = getCount(stats.distribution, 5);
    const c3Neg = Math.floor(c3 / 2);
    const c3Pos = c3 - c3Neg;

    const rawCounts: number[] = [c3, c2, c1, c3, c4, c5];

    return new Chart(canvasEl, {
        type: 'bar',
        data: {
            labels: [''],
            datasets: [
                { label: labels.l3, data: [-c3Neg], backgroundColor: COLOR_NEUTRAL },
                { label: labels.l2, data: [-c2],    backgroundColor: COLOR_DISAGREE },
                { label: labels.l1, data: [-c1],    backgroundColor: COLOR_STRONGLY_DISAGREE },
                { label: labels.l3, data: [c3Pos],  backgroundColor: COLOR_NEUTRAL },
                { label: labels.l4, data: [c4],     backgroundColor: COLOR_AGREE },
                { label: labels.l5, data: [c5],     backgroundColor: COLOR_STRONGLY_AGREE },
            ],
        },
        options: {
            indexAxis: 'y',
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: (ctx) => {
                            const raw = rawCounts[ctx.datasetIndex];
                            return ` ${ctx.dataset.label ?? ''}: ${raw}`;
                        },
                    },
                },
            },
            scales: {
                x: {
                    stacked: true,
                    ticks: {
                        precision: 0,
                        callback: (val) => typeof val === 'number' ? Math.abs(val) : val,
                    },
                },
                y: {
                    stacked: true,
                    display: false,
                },
            },
        },
    });
}

export function initQuestionLikertBarWidget(el: HTMLElement): void {
    const labels: LikertLabels = {
        l1: el.dataset.i18nLikert1 ?? DEFAULT_LABELS.l1,
        l2: el.dataset.i18nLikert2 ?? DEFAULT_LABELS.l2,
        l3: el.dataset.i18nLikert3 ?? DEFAULT_LABELS.l3,
        l4: el.dataset.i18nLikert4 ?? DEFAULT_LABELS.l4,
        l5: el.dataset.i18nLikert5 ?? DEFAULT_LABELS.l5,
    };

    const render = async (
        body: HTMLElement,
        projectId: number,
        questionId: number,
        apiBaseUrl: string,
        strings: WidgetStrings,
    ): Promise<void> => {
        let stats: LikertStats;
        try {
            const res = await fetch(
                `${apiBaseUrl}/api/projects/${projectId}/stats/questions/${questionId}/likert-stats`);
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            stats = await res.json() as LikertStats;
        } catch {
            showMessage(body, strings.loadFailed, 'error');
            return;
        }

        if (stats.totalResponses === 0) {
            showMessage(body, strings.noAnswers, 'info');
            return;
        }

        renderInto(freshCanvas(body), stats, labels);
    };

    initQuestionWidget(el, { format: 'Likert', render });
}
