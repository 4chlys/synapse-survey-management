import { POLL_INTERVAL_MS } from '../admin/dashboard';

interface ModerationQueue {
    approved: number;
    declined: number;
    flagged: number;
    unflagged: number;
}

interface StatusConfig {
    key: keyof ModerationQueue;
    color: string;
    dotColor: string;
    icon: string;
}

export function initModerationQueueWidget(el: HTMLElement): () => void {
    const projectId = el.dataset.projectId ?? '';
    const apiBase = el.dataset.apiBase ?? '';
    const url = `${apiBase}/api/projects/${projectId}/stats/moderation-queue`;
    const titleText = el.dataset.i18nModerationTitle ?? '';
    const approvedLabel = el.dataset.i18nModerationApproved ?? '';
    const newLabel = el.dataset.i18nModerationNew ?? '';
    const flaggedLabel = el.dataset.i18nModerationFlagged ?? '';
    const declinedLabel = el.dataset.i18nModerationDeclined ?? '';

    const statuses: (StatusConfig & { label: string })[] = [
        { key: 'approved', label: approvedLabel, color: 'text-success', dotColor: 'bg-success', icon: `<svg width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M5 13l4 4L19 7"/></svg>` },
        { key: 'unflagged', label: newLabel, color: 'text-warning', dotColor: 'bg-warning', icon: `<svg width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><path d="M12 8v4M12 16h.01"/></svg>` },
        { key: 'flagged', label: flaggedLabel, color: 'text-error', dotColor: 'bg-error', icon: `<svg width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z"/><line x1="4" y1="22" x2="4" y2="15"/></svg>` },
        { key: 'declined', label: declinedLabel, color: 'text-base-content/50', dotColor: 'bg-base-content/30', icon: `<svg width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><path d="M15 9l-6 6M9 9l6 6"/></svg>` },
    ];

    async function fetch_and_render(): Promise<void> {
        try {
            const res = await fetch(url);
            if (!res.ok) return;
            const q: ModerationQueue = await res.json();

            const total = q.approved + q.unflagged + q.flagged + q.declined;

            el.innerHTML = '';

            const header = document.createElement('div');
            header.className = 'flex items-center justify-between mb-3';
            header.innerHTML = `
                <p class="text-xs font-semibold text-base-content/50 uppercase tracking-wider">${titleText}</p>
                <span class="text-[10px] font-bold text-base-content/30 tabular-nums">${total}</span>
            `;
            el.appendChild(header);

            if (total > 0) {
                const bar = document.createElement('div');
                bar.className = 'flex h-1.5 rounded-full overflow-hidden mb-4 bg-base-200';
                const colors: Record<string, string> = { approved: 'bg-success', unflagged: 'bg-warning', flagged: 'bg-error', declined: 'bg-base-content/20' };
                for (const s of statuses) {
                    const pct = (q[s.key] / total) * 100;
                    if (pct > 0) {
                        const seg = document.createElement('div');
                        seg.className = `${colors[s.key]} transition-all duration-500`;
                        seg.style.width = `${pct}%`;
                        bar.appendChild(seg);
                    }
                }
                el.appendChild(bar);
            }

            const grid = document.createElement('div');
            grid.className = 'flex flex-col gap-2';

            for (const s of statuses) {
                const val = q[s.key];
                const row = document.createElement('div');
                row.className = 'flex items-center gap-3 py-1.5 px-2 -mx-2 rounded-lg';

                row.innerHTML = `
                    <div class="w-2 h-2 rounded-full ${s.dotColor} shrink-0"></div>
                    <span class="text-sm text-base-content/70 flex-1">${s.label}</span>
                    <span class="text-sm font-bold tabular-nums ${s.color}">${val}</span>
                `;
                grid.appendChild(row);
            }

            el.appendChild(grid);
        } catch {
            // ignore
        }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
}
