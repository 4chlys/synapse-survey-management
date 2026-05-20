import { POLL_INTERVAL_MS, fetchJson } from '../admin/dashboard';

interface DashboardStats {
    viewCount: number;
    completionCount: number;
    completionRate: number;
}

const LABELS: Record<string, string> = {
    ViewCount: 'Bekeken',
    CompletionCount: 'Ingevuld',
    CompletionRate: 'Voltooiingsgraad',
};

function formatValue(type: string, stats: DashboardStats): string {
    if (type === 'ViewCount') return String(stats.viewCount);
    if (type === 'CompletionCount') return String(stats.completionCount);
    if (type === 'CompletionRate') return `${Math.round(stats.completionRate * 100)}%`;
    return '–';
}

function renderStat(el: HTMLElement, type: string, value: string): void {
    el.innerHTML = `
        <p class="text-3xl font-bold text-base-content">${value}</p>
        <p class="text-xs text-base-content/50 mt-1">${LABELS[type] ?? type}</p>
    `;
}

export function initStatWidget(el: HTMLElement): () => void {
    const type = el.dataset.widget ?? '';
    const projectId = el.dataset.projectId ?? '';
    const apiBase = el.dataset.apiBase ?? '';
    const url = `${apiBase}/api/projects/${projectId}/dashboard`;

    async function fetch_and_render(): Promise<void> {
        try {
            const stats = await fetchJson<DashboardStats>(url);
            renderStat(el, type, formatValue(type, stats));
        } catch {
            // ignore network errors during polling
        }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
}
