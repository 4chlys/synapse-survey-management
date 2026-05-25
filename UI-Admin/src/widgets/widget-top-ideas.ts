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

            el.innerHTML = `<p class="text-xs text-base-content/50 mb-2">${titleText}</p>`;
            if (data.topIdeas.length === 0) {
                const empty = document.createElement('p');
                empty.className = 'text-base-content/40 text-xs';
                empty.textContent = emptyText;
                el.appendChild(empty);
                return;
            }

            data.topIdeas.forEach((idea, i) => {
                const row = document.createElement('div');
                row.className = 'flex gap-2 py-1 border-b border-base-300';

                const rank = document.createElement('span');
                rank.className = 'text-base-content/40 text-xs w-4';
                rank.textContent = String(i + 1);

                const desc = document.createElement('span');
                desc.className = 'text-base-content text-xs flex-1';
                desc.textContent = idea.description;

                const reactions = document.createElement('span');
                reactions.className = 'text-blue-400 text-xs whitespace-nowrap';
                reactions.textContent = `${idea.reactionCount} 👍`;

                row.appendChild(rank);
                row.appendChild(desc);
                row.appendChild(reactions);
                el.appendChild(row);
            });
        } catch { /* ignore */ }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
}
