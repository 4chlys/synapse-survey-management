import { Chart, DoughnutController, ArcElement, Tooltip, Legend } from 'chart.js';
import { fetchSystemStats } from '../lib/system-stats';
import { attachDownloadButton } from '../lib/utils';

Chart.register(DoughnutController, ArcElement, Tooltip, Legend);

export function initSystemProjectStatesWidget(el: HTMLElement): () => void {
    const platformIdAttr = el.dataset.platformId;
    const platformId = platformIdAttr ? Number(platformIdAttr) : null;

    const canvas = document.createElement('canvas');
    el.appendChild(canvas);

    const activeLabel = el.dataset.i18nActive ?? 'Active';
    const draftLabel = el.dataset.i18nDraft ?? 'Draft';
    const archivedLabel = el.dataset.i18nArchived ?? 'Archived';

    const isDark = document.documentElement.getAttribute('data-theme') === 'synapse-dark';
    const labelColor = isDark ? '#d1d5db' : '#374151';

    const chart = new Chart(canvas, {
        type: 'doughnut',
        data: {
            labels: [activeLabel, draftLabel, archivedLabel],
            datasets: [{ data: [0, 0, 0], backgroundColor: ['#22c55e', '#f59e0b', '#9ca3af'] }],
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'right', labels: { color: labelColor, font: { size: 11 } } },
            },
            cutout: '50%',
        },
    });

    fetchSystemStats(platformId).then(stats => {
        chart.data.labels = [
            `${activeLabel} (${stats.activeProjectCount})`,
            `${draftLabel} (${stats.draftProjectCount})`,
            `${archivedLabel} (${stats.archivedProjectCount})`,
        ];
        chart.data.datasets[0].data = [stats.activeProjectCount, stats.draftProjectCount, stats.archivedProjectCount];
        chart.update('none');
    }).catch(() => { /* ignore */ });

    attachDownloadButton(el, canvas, 'project-states.png');

    return () => chart.destroy();
}
