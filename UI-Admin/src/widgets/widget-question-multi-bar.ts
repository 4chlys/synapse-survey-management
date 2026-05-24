import { Chart, BarController, BarElement, LinearScale, CategoryScale, Tooltip } from 'chart.js';
import { initQuestionWidget, freshCanvas, showMessage } from '../lib/question-widget-shell';
import type { WidgetStrings } from '../lib/question-widget-shell';

Chart.register(BarController, BarElement, LinearScale, CategoryScale, Tooltip);

interface OptionCount {
    option: string;
    count: number;
}

export interface MultipleChoiceStats {
    questionId: number;
    totalResponses: number;
    counts: OptionCount[];
}

const PALETTE = [
    '#6366f1', '#f59e0b', '#10b981', '#ef4444', '#3b82f6',
    '#8b5cf6', '#f97316', '#14b8a6', '#ec4899', '#84cc16',
];

export function renderInto(canvasEl: HTMLCanvasElement, stats: MultipleChoiceStats, xAxisLabel: string): Chart {
    return new Chart(canvasEl, {
        type: 'bar',
        data: {
            labels: stats.counts.map(c => c.option),
            datasets: [{
                data: stats.counts.map(c => c.count),
                backgroundColor: stats.counts.map((_, i) => PALETTE[i % PALETTE.length]),
            }],
        },
        options: {
            indexAxis: 'y',
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
            },
            scales: {
                x: {
                    beginAtZero: true,
                    title: { display: true, text: xAxisLabel },
                    ticks: { precision: 0 },
                },
            },
        },
    });
}

export function initQuestionMultiBarWidget(el: HTMLElement): void {
    const xAxisLabel = el.dataset.xAxisLabel ?? '';

    const render = async (
        body: HTMLElement,
        projectId: number,
        questionId: number,
        apiBaseUrl: string,
        strings: WidgetStrings,
    ): Promise<void> => {
        let stats: MultipleChoiceStats;
        try {
            const res = await fetch(
                `${apiBaseUrl}/api/projects/${projectId}/stats/questions/${questionId}/multi-stats`);
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            stats = await res.json() as MultipleChoiceStats;
        } catch {
            showMessage(body, strings.loadFailed, 'error');
            return;
        }

        if (stats.totalResponses === 0) {
            showMessage(body, strings.noAnswers, 'info');
            return;
        }

        renderInto(freshCanvas(body), stats, xAxisLabel);
    };

    initQuestionWidget(el, { format: 'MultipleChoice', render });
}
