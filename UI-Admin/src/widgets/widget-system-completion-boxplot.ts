import { Chart, LinearScale, CategoryScale, Tooltip } from 'chart.js';
import { BoxPlotController, BoxAndWiskers } from '@sgratzl/chartjs-chart-boxplot';
import { fetchSystemStats } from '../lib/system-stats';
import { attachDownloadButton } from '../lib/utils';

Chart.register(BoxPlotController, BoxAndWiskers, LinearScale, CategoryScale, Tooltip);

export function initSystemCompletionBoxplotWidget(el: HTMLElement): () => void {
    const platformIdAttr = el.dataset.platformId;
    const platformId = platformIdAttr ? Number(platformIdAttr) : null;

    const canvas = document.createElement('canvas');
    el.appendChild(canvas);

    const seriesLabel = el.dataset.i18nSeriesCompletionTime ?? 'Completion time (s)';

    const isDark = document.documentElement.getAttribute('data-theme') === 'synapse-dark';
    const tickColor = isDark ? '#9ca3af' : '#6b7280';
    const gridColor = isDark ? '#374151' : '#e5e7eb';

    // BoxPlot plugin accepts pre-computed quartile data via `min/q1/median/q3/max`.
    const chart = new Chart(canvas, {
        type: 'boxplot',
        data: {
            labels: [],
            datasets: [{
                label: seriesLabel,
                data: [],
                backgroundColor: 'rgba(96, 165, 250, 0.5)',
                borderColor: '#3b82f6',
                outlierBackgroundColor: '#3b82f6',
            }],
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: {
                x: { ticks: { color: tickColor, font: { size: 9 } }, grid: { color: gridColor } },
                y: { ticks: { color: tickColor, font: { size: 9 } }, grid: { color: gridColor }, beginAtZero: true },
            },
        },
    });

    fetchSystemStats(platformId).then(stats => {
        const projects = stats.projects
            .filter(p => p.participantCount >= 1)
            .sort((a, b) => a.projectName.localeCompare(b.projectName));
        (chart.data.labels as string[]) = projects.map(p => p.projectName);
        (chart.data.datasets[0].data as unknown[]) = projects.map(p => ({
            min: p.minCompletionSeconds,
            q1: p.q1CompletionSeconds,
            median: p.medianCompletionSeconds,
            q3: p.q3CompletionSeconds,
            max: p.maxCompletionSeconds,
            mean: p.avgCompletionSeconds,
            items: [],
        }));
        chart.update('none');
    }).catch(() => { /* ignore */ });

    attachDownloadButton(el, canvas, 'completion-time-boxplot.png');

    return () => chart.destroy();
}
