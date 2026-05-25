import { POLL_INTERVAL_MS } from '../admin/dashboard';

interface IdeaFeedItem {
    ideaId: number;
    description: string;
    categoryName: string;
    reactionCount: number;
    createdAt: string;
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

            el.innerHTML = `<p class="text-xs text-base-content/50 mb-2">${titleText}</p>`;
            if (items.length === 0) {
                const empty = document.createElement('p');
                empty.className = 'text-base-content/40 text-xs';
                empty.textContent = emptyText;
                el.appendChild(empty);
                return;
            }

            items.forEach(item => {
                const time = new Date(item.createdAt).toLocaleDateString(locale, { day: 'numeric', month: 'short' });

                const row = document.createElement('div');
                row.className = 'py-1 border-b border-base-300';

                const top = document.createElement('div');
                top.className = 'flex justify-between items-start gap-1';

                const descSpan = document.createElement('span');
                descSpan.className = 'text-base-content text-xs flex-1';
                descSpan.textContent = item.description;

                const timeSpan = document.createElement('span');
                timeSpan.className = 'text-base-content/40 text-xs whitespace-nowrap';
                timeSpan.textContent = time;

                const catSpan = document.createElement('span');
                catSpan.className = 'text-xs text-blue-400';
                catSpan.textContent = item.categoryName;

                top.appendChild(descSpan);
                top.appendChild(timeSpan);
                row.appendChild(top);
                row.appendChild(catSpan);
                el.appendChild(row);
            });
        } catch { /* ignore */ }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
}
