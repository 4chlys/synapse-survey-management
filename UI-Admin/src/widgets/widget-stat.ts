import { POLL_INTERVAL_MS, fetchJson } from '../admin/dashboard';

interface DashboardStats {
    viewCount: number;
    completionCount: number;
    completionRate: number;
}

const ICONS: Record<string, string> = {
    ViewCount: `<svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>`,
    CompletionCount: `<svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24"><path d="M9 11l3 3L22 4"/><path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11"/></svg>`,
    CompletionRate: `<svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>`,
};

function formatValue(type: string, stats: DashboardStats): string {
    if (type === 'ViewCount') return stats.viewCount.toLocaleString();
    if (type === 'CompletionCount') return stats.completionCount.toLocaleString();
    if (type === 'CompletionRate') return `${Math.round(stats.completionRate * 100)}%`;
    return '–';
}

function renderStat(el: HTMLElement, type: string, label: string, value: string): void {
    const icon = ICONS[type] ?? '';
    el.innerHTML = `
        <div class="flex items-start justify-between gap-3">
            <div>
                <p class="text-[0.7rem] font-semibold uppercase tracking-wider text-base-content/40 mb-1">${label}</p>
                <p class="text-3xl font-heading font-bold text-base-content leading-none">${value}</p>
            </div>
            <div class="text-primary/50 mt-0.5 shrink-0">${icon}</div>
        </div>
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
            renderStat(el, type, labels[type] ?? type, formatValue(type, stats));
        } catch {
            // ignore network errors during polling
        }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
}
