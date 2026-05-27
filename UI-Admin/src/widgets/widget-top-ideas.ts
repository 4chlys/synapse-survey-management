import { POLL_INTERVAL_MS, fetchJson } from '../admin/dashboard';

interface IdeaMetric {
    description: string;
    reactionCount: number;
    commentCount: number;
}

interface DashboardData {
    topIdeas: IdeaMetric[];
}

export function initTopIdeasWidget(el: HTMLElement): () => void {
    const projectId = el.dataset.projectId ?? '';
    const apiBase = el.dataset.apiBase ?? '';
    const url = `${apiBase}/api/projects/${projectId}/dashboard`;
    const titleText = el.dataset.i18nTopIdeasTitle ?? '';
    const emptyText = el.dataset.i18nNoIdeas ?? '';

    async function fetch_and_render(): Promise<void> {
        try {
            const data = await fetchJson<DashboardData>(url);

            el.innerHTML = '';

            const header = document.createElement('p');
            header.className = 'text-xs font-semibold text-base-content/50 uppercase tracking-wider mb-3';
            header.textContent = titleText;
            el.appendChild(header);

            if (data.topIdeas.length === 0) {
                const empty = document.createElement('p');
                empty.className = 'text-base-content/30 text-xs py-4 text-center';
                empty.textContent = emptyText;
                el.appendChild(empty);
                return;
            }

            const maxReactions = Math.max(...data.topIdeas.map(i => i.reactionCount), 1);

            const list = document.createElement('div');
            list.className = 'flex flex-col gap-1';

            data.topIdeas.forEach((idea, i) => {
                const row = document.createElement('div');
                row.className = 'flex items-start gap-3 py-2 px-2 -mx-2 rounded-lg hover:bg-base-200/60 transition-colors';

                const rank = document.createElement('span');
                rank.className = `text-xs font-bold tabular-nums mt-0.5 shrink-0 w-5 h-5 rounded flex items-center justify-center ${i === 0 ? 'bg-primary/10 text-primary' : 'text-base-content/25'}`;
                rank.textContent = String(i + 1);

                const body = document.createElement('div');
                body.className = 'flex-1 min-w-0';

                const desc = document.createElement('p');
                desc.className = 'text-sm text-base-content leading-snug line-clamp-2';
                desc.textContent = idea.description;

                const barWrap = document.createElement('div');
                barWrap.className = 'flex items-center gap-2 mt-1.5';

                const bar = document.createElement('div');
                bar.className = 'flex-1 h-1 rounded-full bg-base-200 overflow-hidden';
                const fill = document.createElement('div');
                fill.className = 'h-full rounded-full bg-primary/30 transition-all duration-500';
                fill.style.width = `${(idea.reactionCount / maxReactions) * 100}%`;
                bar.appendChild(fill);

                const count = document.createElement('span');
                count.className = 'text-[10px] text-base-content/40 tabular-nums shrink-0';
                count.textContent = `${idea.reactionCount}`;

                barWrap.appendChild(bar);
                barWrap.appendChild(count);

                body.appendChild(desc);
                body.appendChild(barWrap);

                row.appendChild(rank);
                row.appendChild(body);
                list.appendChild(row);
            });

            el.appendChild(list);
        } catch { /* ignore */ }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
}
