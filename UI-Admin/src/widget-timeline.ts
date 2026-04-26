import { Chart, LineController, LineElement, PointElement, LinearScale, CategoryScale, Tooltip, Legend } from 'chart.js';
import { POLL_INTERVAL_MS } from './dashboard';

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

    const canvas = document.createElement('canvas');
    canvas.style.width = '100%';
    el.innerHTML = '<p class="text-xs text-gray-400 mb-2">Deelname (30 dagen)</p>';
    el.appendChild(canvas);

    const chart = new Chart(canvas, {
        type: 'line',
        data: { labels: [], datasets: [
            { label: 'Gestart', data: [], borderColor: '#60a5fa', backgroundColor: 'transparent', tension: 0.3, pointRadius: 2 },
            { label: 'Voltooid', data: [], borderColor: '#34d399', backgroundColor: 'transparent', tension: 0.3, pointRadius: 2 },
        ]},
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { labels: { color: '#d1d5db', font: { size: 10 } } } },
            scales: {
                x: { ticks: { color: '#9ca3af', font: { size: 9 }, maxTicksLimit: 7 }, grid: { color: '#374151' } },
                y: { ticks: { color: '#9ca3af', font: { size: 9 } }, grid: { color: '#374151' } },
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

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => { clearInterval(interval); chart.destroy(); };
}
