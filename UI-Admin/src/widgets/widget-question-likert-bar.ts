import { Chart, BarController, BarElement, LinearScale, CategoryScale, Tooltip } from 'chart.js';
import type { Plugin } from 'chart.js';
import { initQuestionWidget, freshCanvas, showMessage } from '../lib/question-widget-shell';
import type { WidgetStrings } from '../lib/question-widget-shell';
import { attachDownloadButton } from '../lib/system-stats';

Chart.register(BarController, BarElement, LinearScale, CategoryScale, Tooltip);

// Likert questions are a fixed 1–10 magnitude scale (no UI exists to configure another scale;
// AnswerChoiceCount stays 0 and the backend treats that as 1–10). The chart is therefore a plain
// histogram: one bar per point, all 10 points always shown so the scale is not distorted.
const MIN_POINT = 1;
const MAX_POINT = 10;

const BAR_COLOR = '#60a5fa';        // blue-400
const MEDIAN_COLOR = '#1f2937';     // gray-800

interface LikertPointCount {
    point: number;
    count: number;
}

export interface LikertStats {
    questionId: number;
    totalResponses: number;
    distribution: LikertPointCount[];
}

function countFor(distribution: LikertPointCount[], point: number): number {
    return distribution.find(d => d.point === point)?.count ?? 0;
}

// Median of the discrete distribution: the value at the middle response position, averaging the
// two middle values when the response count is even (the usual sample-median convention).
function median(distribution: LikertPointCount[], total: number): number {
    if (total === 0) return NaN;
    const lower = Math.floor((total - 1) / 2);
    const upper = Math.ceil((total - 1) / 2);
    let cumulative = 0;
    let low = NaN;
    let high = NaN;
    for (let point = MIN_POINT; point <= MAX_POINT; point++) {
        const end = cumulative + countFor(distribution, point) - 1;
        if (Number.isNaN(low) && lower <= end) low = point;
        if (upper <= end) { high = point; break; }
        cumulative = end + 1;
    }
    return (low + high) / 2;
}

// Draws the median as a vertical dashed line plus language-neutral "n =" / "x̃ =" captions,
// avoiding any extra i18n plumbing for what is universally understood statistical notation.
function summaryPlugin(total: number, medianValue: number): Plugin {
    return {
        id: 'likertSummary',
        afterDatasetsDraw(chart) {
            const { ctx, chartArea, scales } = chart;
            ctx.save();
            ctx.font = '12px sans-serif';
            ctx.fillStyle = MEDIAN_COLOR;
            ctx.textBaseline = 'top';
            ctx.fillText(`n = ${total}`, chartArea.left + 4, chartArea.top + 2);

            if (!Number.isNaN(medianValue)) {
                // The category axis places point p at data index p - 1; interpolate for a fractional median.
                const idx = medianValue - MIN_POINT;
                const lo = Math.floor(idx);
                const hi = Math.ceil(idx);
                const xLo = scales.x.getPixelForValue(lo);
                const xHi = scales.x.getPixelForValue(hi);
                const x = xLo + (xHi - xLo) * (idx - lo);

                ctx.setLineDash([5, 4]);
                ctx.strokeStyle = MEDIAN_COLOR;
                ctx.lineWidth = 1.5;
                ctx.beginPath();
                ctx.moveTo(x, chartArea.top);
                ctx.lineTo(x, chartArea.bottom);
                ctx.stroke();

                ctx.setLineDash([]);
                ctx.textAlign = x > (chartArea.left + chartArea.right) / 2 ? 'right' : 'left';
                const labelX = ctx.textAlign === 'right' ? x - 4 : x + 4;
                ctx.fillText(`x̃ = ${medianValue}`, labelX, chartArea.top + 2);
            }
            ctx.restore();
        },
    };
}

export function renderInto(canvasEl: HTMLCanvasElement, stats: LikertStats): Chart {
    const points = Array.from({ length: MAX_POINT - MIN_POINT + 1 }, (_, i) => MIN_POINT + i);
    const counts = points.map(p => countFor(stats.distribution, p));
    const medianValue = median(stats.distribution, stats.totalResponses);

    return new Chart(canvasEl, {
        type: 'bar',
        data: {
            labels: points.map(String),
            datasets: [{ data: counts, backgroundColor: BAR_COLOR }],
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        title: (items) => `${items[0].label}`,
                        label: (ctx) => ` ${ctx.parsed.y}`,
                    },
                },
            },
            scales: {
                x: { grid: { display: false } },
                y: { beginAtZero: true, ticks: { precision: 0 } },
            },
        },
        plugins: [summaryPlugin(stats.totalResponses, medianValue)],
    });
}

export function initQuestionLikertBarWidget(el: HTMLElement): void {
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

        const canvas = freshCanvas(body);
        renderInto(canvas, stats);
        attachDownloadButton(body, canvas, 'question-likert.png');
    };

    initQuestionWidget(el, { format: 'Likert', render });
}
