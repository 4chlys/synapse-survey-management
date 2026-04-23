// Native HTML5 drag-and-drop for the questionnaire builder.
// Moves sub-questions between answer-groups, promotes them back to top level,
// reorders within a scope, and toggles "ends survey" per answer.

interface MovePayload {
    questionId: number;
    parentQuestionId: number | null;
    parentTriggerValue: string | null;
    position: number;
}

interface EndingPayload {
    questionId: number;
    endingAnswers: string[];
}

function captureExpandedIds(list: HTMLElement): Set<string> {
    const ids = new Set<string>();
    list.querySelectorAll<HTMLElement>('.question-card').forEach((card) => {
        const qid = card.dataset.questionId;
        if (!qid) return;
        const panel = list.querySelector<HTMLElement>(`#answers-${qid}`);
        if (panel && !panel.classList.contains('hidden')) ids.add(qid);
    });
    return ids;
}

function applyExpandedIds(list: HTMLElement, ids: Set<string>): void {
    list.querySelectorAll<HTMLElement>('.question-card').forEach((card) => {
        const qid = card.dataset.questionId;
        if (!qid) return;
        const panel = list.querySelector<HTMLElement>(`#answers-${qid}`);
        const arrow = list.querySelector<HTMLElement>(`#arrow-${qid}`);
        if (!panel) return;
        const shouldExpand = ids.has(qid);
        panel.classList.toggle('hidden', !shouldExpand);
        if (arrow && arrow.textContent !== '') arrow.textContent = shouldExpand ? 'v' : '>';
    });
}

async function refreshTree(list: HTMLElement, treeUrl: string): Promise<boolean> {
    const expanded = captureExpandedIds(list);
    try {
        const response = await fetch(treeUrl, { headers: { 'Accept': 'text/html' } });
        if (!response.ok) return false;
        const html = await response.text();
        list.innerHTML = html;
        applyExpandedIds(list, expanded);
        return true;
    } catch {
        return false;
    }
}

document.addEventListener('DOMContentLoaded', () => {
    const list = document.getElementById('questions-list') as HTMLElement | null;
    if (!list) return;

    const moveUrl = list.dataset.moveUrl ?? '';
    const endingUrl = list.dataset.endingUrl ?? '';
    const treeUrl = list.dataset.treeUrl ?? '';
    const projectId = parseInt(list.dataset.projectId ?? '0', 10);

    let dragged: HTMLElement | null = null;

    list.addEventListener('dragstart', (e) => {
        const card = (e.target as HTMLElement).closest<HTMLElement>('.question-card');
        if (!card) return;
        dragged = card;
        card.classList.add('opacity-50');
        e.dataTransfer?.setData('text/plain', card.dataset.questionId ?? '');
        if (e.dataTransfer) e.dataTransfer.effectAllowed = 'move';
    });

    list.addEventListener('dragend', () => {
        dragged?.classList.remove('opacity-50');
        dragged = null;
        document.querySelectorAll('.drop-zone.active').forEach((z) => z.classList.remove('active', 'bg-gray-700'));
    });

    list.addEventListener('dragover', (e) => {
        const zone = (e.target as HTMLElement).closest<HTMLElement>('.drop-zone');
        if (!zone || !dragged) return;

        if (isDescendantOf(zone, dragged)) return; // can't drop into own subtree

        e.preventDefault();
        if (e.dataTransfer) e.dataTransfer.dropEffect = 'move';
        zone.classList.add('active', 'bg-gray-700');
    });

    list.addEventListener('dragleave', (e) => {
        const zone = (e.target as HTMLElement).closest<HTMLElement>('.drop-zone');
        zone?.classList.remove('active', 'bg-gray-700');
    });

    list.addEventListener('drop', async (e) => {
        const zone = (e.target as HTMLElement).closest<HTMLElement>('.drop-zone');
        if (!zone || !dragged) return;
        e.preventDefault();
        zone.classList.remove('active', 'bg-gray-700');

        const questionId = parseInt(dragged.dataset.questionId ?? '0', 10);
        const parentIdStr = zone.dataset.parentId ?? '';
        const triggerStr = zone.dataset.parentTrigger ?? '';
        const payload: MovePayload = {
            questionId,
            parentQuestionId: parentIdStr === '' ? null : parseInt(parentIdStr, 10),
            parentTriggerValue: parentIdStr === '' ? null : triggerStr,
            position: 9999, // repack on the server clamps to end
        };

        const ok = await postJson(moveUrl, { ...payload, projectId });
        if (ok) await refreshTree(list, treeUrl);
    });

    // "Ends survey" toggle per answer row.
    list.addEventListener('change', async (e) => {
        const input = e.target as HTMLInputElement;
        if (!input.classList.contains('ending-answer-toggle')) return;

        const questionId = parseInt(input.dataset.questionId ?? '0', 10);
        const card = input.closest<HTMLElement>('.question-card');
        if (!card) return;

        const current = Array.from(card.querySelectorAll<HTMLInputElement>('.ending-answer-toggle'))
            .filter((c) => c.checked)
            .map((c) => c.dataset.triggerValue ?? '')
            .filter((v) => v !== '');

        const payload: EndingPayload = { questionId, endingAnswers: current };
        await postJson(endingUrl, { ...payload, projectId });
    });
});

function isDescendantOf(zone: HTMLElement, card: HTMLElement): boolean {
    // If the drop zone is nested inside the card's own answer-groups, skip.
    return card.contains(zone);
}

async function postJson(url: string, body: unknown): Promise<boolean> {
    try {
        const headers: Record<string, string> = { 'Content-Type': 'application/json' };
        const token = document.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]')?.value;
        if (token) headers['RequestVerificationToken'] = token;

        const response = await fetch(url, {
            method: 'POST',
            headers,
            body: JSON.stringify(body),
        });
        return response.ok;
    } catch {
        return false;
    }
}
