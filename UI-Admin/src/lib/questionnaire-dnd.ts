// Native HTML5 drag-and-drop + fetch wiring for the questionnaire builder.
// Owns the data lifecycle: fetches the flat question list, delegates
// rendering to questionnaire-tree.ts, and re-fetches on move / ending
// toggle. Event handlers are attached once to the stable #questions-list
// container; re-rendering its innerHTML does not invalidate them.

import { renderTree, Question, RenderContext, QuestionsI18n } from './questionnaire-tree';
import { readQuestionsI18n } from './questions-i18n';
import { toast } from './notify';
import { getAntiForgeryToken } from './utils';
import type { MovePayload, EndingPayload, AdminQuestionResponse } from './questionnaire-types';

async function sendJson(url: string, method: 'PUT' | 'POST', body: unknown): Promise<Response | null> {
    try {
        return await fetch(url, {
            method,
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken(),
            },
            body: JSON.stringify(body),
        });
    } catch {
        return null;
    }
}

async function fetchQuestions(endpoint: string, defaultLang: string): Promise<Question[]> {
    const response = await fetch(endpoint, { headers: { Accept: 'application/json' } });
    if (!response.ok) throw new Error(`Failed to fetch questions: ${response.status}`);
    const raw = (await response.json()) as AdminQuestionResponse[];
    return raw.map(q => flattenToDefault(q, defaultLang));
}

function flattenToDefault(q: AdminQuestionResponse, defaultLang: string): Question {
    const t = q.translations.find(tr => tr.languageCode === defaultLang)
        ?? q.translations[0];
    const endings = q.endingAnswers.map(slot => {
        const et = slot.translations.find(tr => tr.languageCode === defaultLang)
            ?? slot.translations[0];
        return et?.value ?? '';
    }).filter(v => v !== '');
    return {
        questionId:        q.questionId,
        text:              t?.text ?? '',
        questionFormat:    q.questionFormat,
        answerChoices:     t?.answerChoices ?? [],
        answerChoiceCount: q.answerChoiceCount,
        isMandatory:       q.isMandatory,
        position:          q.position,
        parentQuestionId:  q.parentQuestionId,
        choiceIndex:       q.choiceIndex,
        endingAnswers:     endings,
        sectionName:       q.sectionName,
        sectionId:         q.sectionId,
        mediaUrl:          q.mediaUrl,
        mediaType:         q.mediaType,
        mediaIsEmbed:      q.mediaIsEmbed,
        mediaText:         t?.mediaText ?? null,
        translations:      q.translations,
        endingAnswerSlots: q.endingAnswers,
    };
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

let _list: HTMLElement | null = null;
let _questionsEndpoint = '';
let _defaultLang = 'nl';
let _ctx: RenderContext | null = null;

async function reload(): Promise<void> {
    if (!_list || !_ctx) return;
    const expanded = captureExpandedIds(_list);
    try {
        const questions = await fetchQuestions(_questionsEndpoint, _defaultLang);
        renderTree(_list, questions, _ctx);
        applyExpandedIds(_list, expanded);
        const nextBtn = document.getElementById('questions-next-btn') as HTMLButtonElement | null;
        if (nextBtn) nextBtn.dataset.questionCount = String(questions.length);
    } catch (err) {
        _list.innerHTML = `<p class="text-red-400 text-sm">${String(err)}</p>`;
    }
}

export async function initQuestionnaireDnd(): Promise<void> {
    const list = document.getElementById('questions-list') as HTMLElement | null;
    if (!list) return;

    const projectId = parseInt(list.dataset.projectId ?? '0', 10);
    const questionsEndpoint = list.dataset.questionsEndpoint ?? '';
    const deleteEndpoint = list.dataset.deleteEndpoint ?? '';
    const locked = list.dataset.locked === 'true';
    const isNew = list.dataset.isNew === 'true';
    const defaultLang = list.dataset.defaultLanguageCode ?? 'nl';

    const i18n: QuestionsI18n = readQuestionsI18n(list);
    const ctx: RenderContext = { deleteEndpoint, projectId, locked, isNew, i18n };

    _list = list;
    _questionsEndpoint = questionsEndpoint;
    _defaultLang = defaultLang;
    _ctx = ctx;

    await reload();

    document.addEventListener('questions-reload', () => { void reload(); });

    if (locked) return;

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
        document.querySelectorAll('.drop-zone.active').forEach((z) => z.classList.remove('active', 'bg-base-200'));
    });

    list.addEventListener('dragover', (e) => {
        const zone = (e.target as HTMLElement).closest<HTMLElement>('.drop-zone');
        if (!zone || !dragged) return;
        if (dragged.contains(zone)) return; // no dropping into own subtree

        e.preventDefault();
        if (e.dataTransfer) e.dataTransfer.dropEffect = 'move';
        zone.classList.add('active', 'bg-base-200');
    });

    list.addEventListener('dragleave', (e) => {
        const zone = (e.target as HTMLElement).closest<HTMLElement>('.drop-zone');
        zone?.classList.remove('active', 'bg-base-200');
    });

    list.addEventListener('drop', async (e) => {
        const zone = (e.target as HTMLElement).closest<HTMLElement>('.drop-zone');
        if (!zone || !dragged) return;
        e.preventDefault();
        zone.classList.remove('active', 'bg-base-200');

        const questionId = parseInt(dragged.dataset.questionId ?? '0', 10);
        const parentIdStr = zone.dataset.parentId ?? '';
        const choiceIndexStr = zone.dataset.parentChoiceIndex ?? '';
        const payload: MovePayload = {
            parentQuestionId: parentIdStr === '' ? null : parseInt(parentIdStr, 10),
            choiceIndex: (parentIdStr === '' || choiceIndexStr === '') ? null : parseInt(choiceIndexStr, 10),
            position: 9999, // server clamps to end of scope
        };

        const response = await sendJson(`${questionsEndpoint}/${questionId}/location`, 'PUT', payload);
        if (response?.ok) {
            toast(i18n.savedToast, 'success');
            await reload();
        } else {
            toast(i18n.saveFailedToast, 'error');
        }
    });

    list.addEventListener('change', async (e) => {
        const input = e.target as HTMLInputElement;
        if (!input.classList.contains('ending-answer-toggle')) return;

        const questionId = parseInt(input.dataset.questionId ?? '0', 10);
        const card = input.closest<HTMLElement>('.question-card');
        if (!card) return;

        const values = Array.from(card.querySelectorAll<HTMLInputElement>('.ending-answer-toggle'))
            .filter((c) => c.checked)
            .map((c) => c.dataset.triggerValue ?? '')
            .filter((v) => v !== '');

        const payload: EndingPayload = { endingAnswers: values };
        const endingResponse = await sendJson(`${questionsEndpoint}/${questionId}/ending-answers`, 'PUT', payload);
        if (endingResponse?.ok) {
            toast(i18n.savedToast, 'success');
        } else {
            toast(i18n.saveFailedToast, 'error');
        }
    });

    list.addEventListener('submit', async (e) => {
        const form = (e.target as HTMLElement).closest<HTMLFormElement>('.question-delete-form');
        if (!form) return;
        e.preventDefault();
        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
            });
            if (response.ok) {
                toast(i18n.savedToast, 'success');
                await reload();
            } else {
                toast(i18n.saveFailedToast, 'error');
            }
        } catch {
            toast(i18n.saveFailedToast, 'error');
        }
    });
}
