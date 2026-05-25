import { Chart, ArcElement, Tooltip, Legend, PieController } from 'chart.js';
import { initQuestionWidget, freshCanvas, showMessage } from '../lib/question-widget-shell';
import type { WidgetStrings } from '../lib/question-widget-shell';

Chart.register(ArcElement, Tooltip, Legend, PieController);

interface OptionCount {
    option: string;
    count: number;
}

export interface SingleChoiceStats {
    questionId: number;
    totalResponses: number;
    counts: OptionCount[];
}

const PALETTE = [
    '#6366f1', '#f59e0b', '#10b981', '#ef4444', '#3b82f6',
    '#8b5cf6', '#f97316', '#14b8a6', '#ec4899', '#84cc16',
];

export function renderInto(canvasEl: HTMLCanvasElement, stats: SingleChoiceStats): Chart {
    return new Chart(canvasEl, {
        type: 'pie',
        data: {
            labels: stats.counts.map(c => c.option),
            datasets: [{
                data: stats.counts.map(c => c.count),
                backgroundColor: stats.counts.map((_, i) => PALETTE[i % PALETTE.length]),
            }],
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'right' },
            },
        },
    });
}

async function render(
    body: HTMLElement,
    projectId: number,
    questionId: number,
    apiBaseUrl: string,
    strings: WidgetStrings,
): Promise<void> {
    let stats: SingleChoiceStats;
    try {
        const res = await fetch(
            `${apiBaseUrl}/api/projects/${projectId}/stats/questions/${questionId}/single-stats`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        stats = await res.json() as SingleChoiceStats;
    } catch {
        showMessage(body, strings.loadFailed, 'error');
        return;
    }

    if (stats.totalResponses === 0) {
        showMessage(body, strings.noAnswers, 'info');
        return;
    }

    renderInto(freshCanvas(body), stats);
}

export function initQuestionSinglePieWidget(el: HTMLElement): void {
    initQuestionWidget(el, { format: 'SingleChoice', render });
}
