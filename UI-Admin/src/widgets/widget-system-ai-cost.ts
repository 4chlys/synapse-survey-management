import { Chart, BarController, BarElement, LinearScale, CategoryScale, Tooltip } from 'chart.js';
import { attachDownloadButton, fetchSystemStats } from '../lib/system-stats';

Chart.register(BarController, BarElement, LinearScale, CategoryScale, Tooltip);

export function initSystemAiCostWidget(el: HTMLElement): () => void {
    const platformIdAttr = el.dataset.platformId;
    const platformId = platformIdAttr ? Number(platformIdAttr) : null;

    const canvas = document.createElement('canvas');
    el.appendChild(canvas);

    const isDark = document.documentElement.getAttribute('data-theme') === 'synapse-dark';
    const tickColor = isDark ? '#9ca3af' : '#6b7280';
    const gridColor = isDark ? '#374151' : '#e5e7eb';

    const chart = new Chart(canvas, {
        type: 'bar',
        data: { labels: [], datasets: [{ label: 'EUR', data: [], backgroundColor: '#a78bfa' }] },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: {
                x: { ticks: { color: tickColor, font: { size: 10 } }, grid: { color: gridColor } },
                y: { ticks: { color: tickColor, font: { size: 10 } }, grid: { color: gridColor }, beginAtZero: true },
            },
        },
    });

    fetchSystemStats(platformId).then(stats => {
        const byService = new Map<string, number>();
        for (const row of stats.aiUsageBySystem) {
            byService.set(row.service, (byService.get(row.service) ?? 0) + row.costEur);
        }
        const entries = [...byService.entries()].sort((a, b) => b[1] - a[1]);
        (chart.data.labels as string[]) = entries.map(([service]) => service);
        (chart.data.datasets[0].data as number[]) = entries.map(([, cost]) => cost);
        chart.update('none');
    }).catch(() => { /* ignore */ });

    attachDownloadButton(el, canvas, 'ai-cost-per-service.png');

    return () => chart.destroy();
}
