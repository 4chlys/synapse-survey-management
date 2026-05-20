import { Chart, BarController, BarElement, LinearScale, CategoryScale, Tooltip } from 'chart.js';
import { attachDownloadButton, fetchSystemStats } from '../lib/system-stats';

Chart.register(BarController, BarElement, LinearScale, CategoryScale, Tooltip);

const TopN = 20;

export function initSystemParticipantsWidget(el: HTMLElement): () => void {
    const platformIdAttr = el.dataset.platformId;
    const platformId = platformIdAttr ? Number(platformIdAttr) : null;

    const canvas = document.createElement('canvas');
    el.appendChild(canvas);

    const isDark = document.documentElement.getAttribute('data-theme') === 'synapse-dark';
    const tickColor = isDark ? '#9ca3af' : '#6b7280';
    const gridColor = isDark ? '#374151' : '#e5e7eb';

    const chart = new Chart(canvas, {
        type: 'bar',
        data: { labels: [], datasets: [{ label: 'Participants', data: [], backgroundColor: '#60a5fa' }] },
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
        const top = [...stats.projects].sort((a, b) => b.participantCount - a.participantCount).slice(0, TopN);
        (chart.data.labels as string[]) = top.map(p => p.projectName);
        (chart.data.datasets[0].data as number[]) = top.map(p => p.participantCount);
        chart.update('none');
    }).catch(() => { /* ignore */ });

    attachDownloadButton(el, canvas, 'participants-per-project.png');

    return () => chart.destroy();
}
