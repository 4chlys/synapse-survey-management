import { POLL_INTERVAL_MS } from '../admin/dashboard';

interface IdeaFeedItem {
    ideaId: number;
    description: string;
    categoryName: string;
    reactionCount: number;
    createdAt: string;
}

function timeAgo(dateStr: string, locale: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60_000);
    if (mins < 1) return locale === 'nl' ? 'nu' : 'now';
    if (mins < 60) return `${mins}m`;
    const hours = Math.floor(mins / 60);
    if (hours < 24) return `${hours}h`;
    const days = Math.floor(hours / 24);
    return `${days}d`;
}

export function initIdeaFeedWidget(el: HTMLElement): () => void {
    const projectId = el.dataset.projectId ?? '';
    const apiBase = el.dataset.apiBase ?? '';
    const url = `${apiBase}/api/projects/${projectId}/stats/idea-feed?count=10`;
    const titleText = el.dataset.i18nIdeaFeedTitle ?? '';
    const emptyText = el.dataset.i18nNoIdeas ?? '';
    const locale = document.documentElement.lang || 'nl';

    async function fetch_and_render(): Promise<void> {
        try {
            const res = await fetch(url);
            if (!res.ok) return;
            const items: IdeaFeedItem[] = await res.json();

            el.innerHTML = '';

            const header = document.createElement('div');
            header.className = 'flex items-center justify-between mb-3';
            header.innerHTML = `
                <p class="text-xs font-semibold text-base-content/50 uppercase tracking-wider">${titleText}</p>
                <span class="text-[10px] font-bold text-primary/60 tabular-nums">${items.length}</span>
            `;
            el.appendChild(header);

            if (items.length === 0) {
                const empty = document.createElement('p');
                empty.className = 'text-base-content/30 text-xs py-4 text-center';
                empty.textContent = emptyText;
                el.appendChild(empty);
                return;
            }

            const list = document.createElement('div');
            list.className = 'flex flex-col gap-0.5';

            items.forEach(item => {
                const ago = timeAgo(item.createdAt, locale);

                const row = document.createElement('div');
                row.className = 'group flex items-start gap-2.5 py-2 px-2 -mx-2 rounded-lg hover:bg-base-200/60 transition-colors';

                const dot = document.createElement('div');
                dot.className = 'w-1.5 h-1.5 rounded-full bg-primary/40 mt-1.5 shrink-0';

                const body = document.createElement('div');
                body.className = 'flex-1 min-w-0';

                const descLine = document.createElement('p');
                descLine.className = 'text-sm text-base-content leading-snug line-clamp-2';
                descLine.textContent = item.description;

                const meta = document.createElement('div');
                meta.className = 'flex items-center gap-2 mt-1';

                const cat = document.createElement('span');
                cat.className = 'text-[10px] font-medium text-primary/70 bg-primary/8 px-1.5 py-0.5 rounded';
                cat.textContent = item.categoryName;

                const reactions = document.createElement('span');
                reactions.className = 'text-[10px] text-base-content/40 tabular-nums';
                reactions.textContent = item.reactionCount > 0 ? `${item.reactionCount} reactions` : '';

                meta.appendChild(cat);
                if (item.reactionCount > 0) meta.appendChild(reactions);

                body.appendChild(descLine);
                body.appendChild(meta);

                const time = document.createElement('span');
                time.className = 'text-[10px] text-base-content/30 tabular-nums shrink-0 mt-0.5';
                time.textContent = ago;

                row.appendChild(dot);
                row.appendChild(body);
                row.appendChild(time);
                list.appendChild(row);
            });

            el.appendChild(list);
        } catch { /* ignore */ }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
}
