import { Chart, BarController, BarElement, LinearScale, CategoryScale, Tooltip } from 'chart.js';
import { POLL_INTERVAL_MS, fetchJson } from './dashboard';

Chart.register(BarController, BarElement, LinearScale, CategoryScale, Tooltip);

interface CategoryMetric {
    name: string;
    ideaCount: number;
    totalReactions: number;
}

interface DashboardData {
    topCategories: CategoryMetric[];
}

export function initTopCategoriesWidget(el: HTMLElement): () => void {
    const projectId = el.dataset.projectId ?? '';
    const apiBase = el.dataset.apiBase ?? '';
    const url = `${apiBase}/api/projects/${projectId}/dashboard`;

    const canvas = document.createElement('canvas');
    el.innerHTML = '<p class="text-xs text-gray-400 mb-2">Top categorieën</p>';
    el.appendChild(canvas);

    const chart = new Chart(canvas, {
        type: 'bar',
        data: { labels: [], datasets: [
            { label: 'Ideeën', data: [], backgroundColor: '#60a5fa' },
            { label: 'Reacties', data: [], backgroundColor: '#a78bfa' },
        ]},
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { labels: { color: '#d1d5db', font: { size: 10 } } } },
            scales: {
                x: { ticks: { color: '#9ca3af', font: { size: 9 } }, grid: { color: '#374151' } },
                y: { ticks: { color: '#9ca3af', font: { size: 9 } }, grid: { color: '#374151' } },
            },
        },
    });

    async function fetch_and_render(): Promise<void> {
        try {
            const data = await fetchJson<DashboardData>(url);
            (chart.data.labels as string[]) = data.topCategories.map(c => c.name);
            (chart.data.datasets[0].data as number[]) = data.topCategories.map(c => c.ideaCount);
            (chart.data.datasets[1].data as number[]) = data.topCategories.map(c => c.totalReactions);
            chart.update('none');
        } catch { /* ignore */ }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => { clearInterval(interval); chart.destroy(); };
}
