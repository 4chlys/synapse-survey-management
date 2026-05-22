import { POLL_INTERVAL_MS, fetchJson } from '../admin/dashboard';

interface DashboardStats {
    viewCount: number;
    completionCount: number;
    completionRate: number;
}

function formatValue(type: string, stats: DashboardStats): string {
    if (type === 'ViewCount') return String(stats.viewCount);
    if (type === 'CompletionCount') return String(stats.completionCount);
    if (type === 'CompletionRate') return `${Math.round(stats.completionRate * 100)}%`;
    return '–';
}

function renderStat(el: HTMLElement, label: string, value: string): void {
    el.innerHTML = `
        <p class="text-3xl font-bold text-base-content">${value}</p>
        <p class="text-xs text-base-content/50 mt-1">${label}</p>
    `;
}

export function initStatWidget(el: HTMLElement): () => void {
    const type = el.dataset.widget ?? '';
    const projectId = el.dataset.projectId ?? '';
    const apiBase = el.dataset.apiBase ?? '';
    const url = `${apiBase}/api/projects/${projectId}/dashboard`;

    const labels: Record<string, string> = {
        ViewCount: el.dataset.i18nStatViewCount ?? type,
        CompletionCount: el.dataset.i18nStatCompletionCount ?? type,
        CompletionRate: el.dataset.i18nStatCompletionRate ?? type,
    };

    async function fetch_and_render(): Promise<void> {
        try {
            const stats = await fetchJson<DashboardStats>(url);
            renderStat(el, labels[type] ?? type, formatValue(type, stats));
        } catch {
            // ignore network errors during polling
        }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
}
