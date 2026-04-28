// Client-side renderer for the admin questionnaire builder.
// Takes the flat list of questions from /api/admin/projects/{id}/questions
// and produces the nested DOM via O(N) bucketing — no server-side Razor
// recursion. Event handling is done via delegation from the stable
// #questions-list container (see questionnaire-dnd.ts), so re-rendering
// the innerHTML here does not break any listeners.

export interface Question {
    questionId: number;
    text: string;
    questionFormat: string;
    answerChoices: string[];
    isMandatory: boolean;
    position: number;
    parentQuestionId: number | null;
    parentTriggerValue: string | null;
    endingAnswers: string[];
    sectionName: string | null;
    sectionId: number | null;
}

export interface RenderContext {
    deleteEndpoint: string;
    aiAssistance: string;
    projectId: number;
    locked: boolean;
}

type ScopeKey = string;

function scopeKey(parentId: number | null, trigger: string | null): ScopeKey {
    return `${parentId ?? ''}|${trigger ?? ''}`;
}

// Bucket all questions by their (parent, trigger) scope. Each bucket is
// sorted by Position. Done once per render; the recursive walk below is
// then O(N) total lookups instead of O(N × children) scans.
function bucketByScope(questions: Question[]): Map<ScopeKey, Question[]> {
    const buckets = new Map<ScopeKey, Question[]>();
    for (const q of questions) {
        const key = scopeKey(q.parentQuestionId, q.parentTriggerValue);
        const arr = buckets.get(key);
        if (arr) arr.push(q);
        else buckets.set(key, [q]);
    }
    for (const arr of buckets.values()) arr.sort((a, b) => a.position - b.position);
    return buckets;
}

function triggerValuesFor(q: Question): string[] {
    if (q.questionFormat === 'SingleChoice' || q.questionFormat === 'MultipleChoice') {
        return q.answerChoices;
    }
    if (q.questionFormat === 'Likert') {
        return Array.from({ length: 10 }, (_, i) => String(i + 1));
    }
    return [];
}

function escapeHtml(text: string): string {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function renderCard(q: Question, depth: number, buckets: Map<ScopeKey, Question[]>, ctx: RenderContext): string {
    const triggers = triggerValuesFor(q);
    const canBranch = triggers.length > 0;
    const arrow = canBranch ? (depth === 0 ? 'v' : '>') : '';
    const panelHidden = depth === 0 ? '' : 'hidden';

    const deleteUrl = `${ctx.deleteEndpoint}?projectId=${ctx.projectId}&questionId=${q.questionId}&aiAssistance=${encodeURIComponent(ctx.aiAssistance)}`;

    const endingToggle = (trigger: string, endsSurvey: boolean) => ctx.locked
        ? `<span class="text-xs text-base-content/40">${endsSurvey ? 'Beëindigt enquête' : ''}</span>`
        : `<label class="text-xs text-base-content/60 flex items-center gap-1">
               <input type="checkbox"
                      class="ending-answer-toggle"
                      data-question-id="${q.questionId}"
                      data-trigger-value="${escapeHtml(trigger)}"
                      ${endsSurvey ? 'checked' : ''} />
               Beëindig enquête
           </label>`;

    const answerGroups = canBranch
        ? triggers.map((trigger) => {
            const children = buckets.get(scopeKey(q.questionId, trigger)) ?? [];
            const endsSurvey = q.endingAnswers.includes(trigger);
            const dropZone = ctx.locked ? '' : `
                    <div class="drop-zone border border-dashed border-base-content/25 text-base-content/40 text-center py-2 my-1 text-xs"
                         data-parent-id="${q.questionId}"
                         data-parent-trigger="${escapeHtml(trigger)}">
                        Sleep hier om een subvraag aan dit antwoord toe te voegen
                    </div>`;
            return `
                <div class="answer-group pl-4 border-l border-base-300 ml-2 mt-2">
                    <div class="flex items-center gap-2 py-1 text-base-content/80">
                        <span class="flex-1">↳ ${escapeHtml(trigger)}</span>
                        ${endingToggle(trigger, endsSurvey)}
                    </div>
                    ${children.map((c) => renderCard(c, depth + 1, buckets, ctx)).join('')}
                    ${dropZone}
                </div>`;
        }).join('')
        : '';

    const gripAndActions = ctx.locked
        ? ''
        : `<span class="question-grip px-2 py-3 cursor-move text-base-content/40 select-none" title="Versleep om te verplaatsen">⠿</span>`;

    const editBtn = ctx.locked ? '' : `
                <button type="button"
                        class="question-edit-btn border-l border-base-300 px-3 text-base-content/50 hover:text-base-content hover:bg-base-200 transition"
                        data-question-id="${q.questionId}"
                        data-text="${escapeHtml(q.text)}"
                        data-format="${escapeHtml(q.questionFormat)}"
                        data-answers='${escapeHtml(JSON.stringify(q.answerChoices))}'
                        data-section-id="${q.sectionId ?? ''}"
                        data-is-mandatory="${q.isMandatory}">✎</button>`;

    const deleteBtn = ctx.locked ? '' : `
                <form method="post" action="${escapeHtml(deleteUrl)}">
                    <input type="hidden" name="__RequestVerificationToken" value="${getAntiforgeryToken()}" />
                    <button type="submit"
                            class="border-l border-base-300 px-3 text-base-content/50 hover:text-error hover:bg-error/10 transition h-full">✕</button>
                </form>`;

    return `
        <div class="question-card border border-base-300 bg-base-100 mb-2 rounded-lg overflow-hidden"
             ${ctx.locked ? '' : 'draggable="true"'}
             data-question-id="${q.questionId}"
             data-format="${escapeHtml(q.questionFormat)}"
             data-parent-id="${q.parentQuestionId ?? ''}"
             data-parent-trigger="${escapeHtml(q.parentTriggerValue ?? '')}"
             data-position="${q.position}"
             style="margin-left:${depth * 16}px">

            <div class="flex items-stretch">
                ${gripAndActions}
                <button type="button"
                        class="question-toggle flex-1 px-3 py-3 text-left flex justify-between items-center hover:bg-base-200 transition"
                        data-question-id="${q.questionId}">
                    <span class="text-base-content">${escapeHtml(q.text)} <span class="text-base-content/40 ml-2">[${escapeHtml(q.questionFormat)}]</span>${q.sectionName ? ` <span class="text-base-content/50 ml-2 italic">${escapeHtml(q.sectionName)}</span>` : ''}</span>
                    <span id="arrow-${q.questionId}" class="text-base-content/60 ml-2">${arrow}</span>
                </button>
                ${editBtn}
                ${deleteBtn}
            </div>

            ${canBranch ? `<div id="answers-${q.questionId}" class="${panelHidden}">${answerGroups}</div>` : ''}
        </div>`;
}

function getAntiforgeryToken(): string {
    return document.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]')?.value ?? '';
}

const promoteDropZone = `
    <div class="drop-zone promote-zone border border-dashed border-base-content/25 text-base-content/50 text-center py-3 my-3 text-xs rounded-lg"
         data-parent-id=""
         data-parent-trigger="">
        Sleep een subvraag hierheen om er een hoofdvraag van te maken
    </div>`;

export function renderTree(list: HTMLElement, questions: Question[], ctx: RenderContext): void {
    const buckets = bucketByScope(questions);
    const topLevel = buckets.get(scopeKey(null, null)) ?? [];

    if (topLevel.length === 0) {
        list.innerHTML = `<p class="text-base-content/40 italic mb-4 p-4 text-sm">Nog geen vragen toegevoegd.</p>${ctx.locked ? '' : promoteDropZone}`;
        return;
    }

    const cards = topLevel.map((q) => renderCard(q, 0, buckets, ctx)).join('');
    list.innerHTML = `${cards}${ctx.locked ? '' : promoteDropZone}`;
}
