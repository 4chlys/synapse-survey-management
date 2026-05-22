import { Chart, LineController, LineElement, PointElement, LinearScale, CategoryScale, Tooltip, Legend } from 'chart.js';
import { POLL_INTERVAL_MS } from '../admin/dashboard';
import { attachDownloadButton } from '../lib/system-stats';

Chart.register(LineController, LineElement, PointElement, LinearScale, CategoryScale, Tooltip, Legend);

interface TimelineData {
    dates: string[];
    starts: number[];
    completions: number[];
}

export function initTimelineWidget(el: HTMLElement): () => void {
    const projectId = el.dataset.projectId ?? '';
    const apiBase = el.dataset.apiBase ?? '';
    const url = `${apiBase}/api/projects/${projectId}/stats/timeline?days=30`;
    const titleText = el.dataset.i18nTimelineTitle ?? '';
    const startedLabel = el.dataset.i18nSeriesStarted ?? '';
    const completedLabel = el.dataset.i18nSeriesCompleted ?? '';

    const canvas = document.createElement('canvas');
    canvas.style.width = '100%';
    el.innerHTML = `<p class="text-xs text-base-content/50 mb-2">${titleText}</p>`;
    el.appendChild(canvas);

    const isDark = document.documentElement.getAttribute('data-theme') === 'synapse-dark';
    const labelColor = isDark ? '#d1d5db' : '#374151';
    const tickColor = isDark ? '#9ca3af' : '#6b7280';
    const gridColor = isDark ? '#374151' : '#e5e7eb';

    const chart = new Chart(canvas, {
        type: 'line',
        data: { labels: [], datasets: [
            { label: startedLabel, data: [], borderColor: '#60a5fa', backgroundColor: 'transparent', tension: 0.3, pointRadius: 2 },
            { label: completedLabel, data: [], borderColor: '#34d399', backgroundColor: 'transparent', tension: 0.3, pointRadius: 2 },
        ]},
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { labels: { color: labelColor, font: { size: 10 } } } },
            scales: {
                x: { ticks: { color: tickColor, font: { size: 9 }, maxTicksLimit: 7 }, grid: { color: gridColor } },
                y: { ticks: { color: tickColor, font: { size: 9 } }, grid: { color: gridColor } },
            },
        },
    });

    async function fetch_and_render(): Promise<void> {
        try {
            const res = await fetch(url);
            if (!res.ok) return;
            const data: TimelineData = await res.json();
            (chart.data.labels as string[]) = data.dates.map(d => d.slice(5));
            (chart.data.datasets[0].data as number[]) = data.starts;
            (chart.data.datasets[1].data as number[]) = data.completions;
            chart.update('none');
        } catch {
            // ignore
        }
    }

    attachDownloadButton(el, canvas, 'participation-timeline.png');

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => { clearInterval(interval); chart.destroy(); };
}
