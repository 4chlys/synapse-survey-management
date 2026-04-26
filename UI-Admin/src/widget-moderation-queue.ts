import { POLL_INTERVAL_MS } from './dashboard';

interface ModerationQueue {
    approved: number;
    declined: number;
    flagged: number;
    unflagged: number;
}

export function initModerationQueueWidget(el: HTMLElement): () => void {
    const projectId = el.dataset.projectId ?? '';
    const apiBase = el.dataset.apiBase ?? '';
    const url = `${apiBase}/api/projects/${projectId}/stats/moderation-queue`;

    async function fetch_and_render(): Promise<void> {
        try {
            const res = await fetch(url);
            if (!res.ok) return;
            const q: ModerationQueue = await res.json();
            el.innerHTML = `
                <p class="text-xs text-gray-400 mb-3">Moderatiestatus</p>
                <div class="grid grid-cols-2 gap-2 text-center">
                    <div class="bg-gray-700 rounded p-2">
                        <p class="text-lg font-bold text-green-400">${q.approved}</p>
                        <p class="text-xs text-gray-400">Goedgekeurd</p>
                    </div>
                    <div class="bg-gray-700 rounded p-2">
                        <p class="text-lg font-bold text-yellow-400">${q.unflagged}</p>
                        <p class="text-xs text-gray-400">Nieuw</p>
                    </div>
                    <div class="bg-gray-700 rounded p-2">
                        <p class="text-lg font-bold text-red-400">${q.flagged}</p>
                        <p class="text-xs text-gray-400">Gemarkeerd</p>
                    </div>
                    <div class="bg-gray-700 rounded p-2">
                        <p class="text-lg font-bold text-gray-400">${q.declined}</p>
                        <p class="text-xs text-gray-400">Afgewezen</p>
                    </div>
                </div>
            `;
        } catch {
            // ignore
        }
    }

    fetch_and_render();
    const interval = setInterval(fetch_and_render, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
}
