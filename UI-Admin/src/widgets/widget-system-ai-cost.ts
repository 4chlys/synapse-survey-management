import { Chart, BarController, BarElement, LinearScale, CategoryScale, Tooltip } from 'chart.js';
import { POLL_INTERVAL_MS } from '../admin/dashboard';
import { fetchSystemStats } from '../lib/system-stats';
import { attachDownloadButton } from '../lib/utils';

Chart.register(BarController, BarElement, LinearScale, CategoryScale, Tooltip);

const DAY_MS = 24 * 60 * 60 * 1000;

export function initSystemAiCostWidget(el: HTMLElement): () => void {
    const platformIdAttr = el.dataset.platformId;
    const platformId = platformIdAttr ? Number(platformIdAttr) : null;

    const canvas = document.createElement('canvas');
    el.appendChild(canvas);

    const seriesLabel = el.dataset.i18nSeriesCost ?? 'EUR';

    const isDark = document.documentElement.getAttribute('data-theme') === 'synapse-dark';
    const tickColor = isDark ? '#9ca3af' : '#6b7280';
    const gridColor = isDark ? '#374151' : '#e5e7eb';

    const chart = new Chart(canvas, {
        type: 'bar',
        data: { labels: [], datasets: [{ label: seriesLabel, data: [], backgroundColor: '#a78bfa' }] },
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

    // Graphic shows a sliding last-24h window; the tables elsewhere show all-time totals.
    async function load(): Promise<void> {
        try {
            const aiFrom = new Date(Date.now() - DAY_MS).toISOString();
            const stats = await fetchSystemStats(platformId, aiFrom);
            const byService = new Map<string, number>();
            for (const row of stats.aiUsageBySystem) {
                byService.set(row.service, (byService.get(row.service) ?? 0) + row.costEur);
            }
            const entries = [...byService.entries()].sort((a, b) => b[1] - a[1]);
            (chart.data.labels as string[]) = entries.map(([service]) => service);
            (chart.data.datasets[0].data as number[]) = entries.map(([, cost]) => cost);
            chart.update('none');
        } catch {
            // ignore network errors during polling
        }
    }

    void load();
    const interval = setInterval(() => void load(), POLL_INTERVAL_MS);

    attachDownloadButton(el, canvas, 'ai-cost-per-service.png');

    return () => { clearInterval(interval); chart.destroy(); };
}
