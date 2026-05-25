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
    answerChoiceCount: number;
    isMandatory: boolean;
    position: number;
    parentQuestionId: number | null;
    choiceIndex: number | null;
    endingAnswers: string[];
    sectionName: string | null;
    sectionId: number | null;
    mediaUrl: string | null;
    mediaType: number | null;
    mediaIsEmbed: boolean;
    mediaText: string | null;
}

export interface QuestionsI18n {
    addQuestion: string;
    emptyNoQuestions: string;
    emptySectionAdd: string;
    addInSection: string;
    followupsSingular: string;
    followupsPlural: string;
    subDropZone: string;
    promoteDropZone: string;
    endsSurveyLabel: string;
    endsSurveyToggle: string;
    dragTitle: string;
    questionSingular: string;
    questionPlural: string;
    mandatory: string;
    formatOpen: string;
    formatMCQ: string;
    formatSCQ: string;
    formatLikert: string;
    mediaImage: string;
    mediaVideo: string;
    mediaAudio: string;
    mediaGeneric: string;
    statsTitle: string;
    savedToast: string;
    saveFailedToast: string;
}

export interface RenderContext {
    deleteEndpoint: string;
    projectId: number;
    locked: boolean;
    isNew: boolean;
    i18n: QuestionsI18n;
}

function labelForFormat(format: string, i18n: QuestionsI18n): string {
    switch (format) {
        case 'MultipleChoice': return i18n.formatMCQ;
        case 'SingleChoice':   return i18n.formatSCQ;
        case 'Likert':         return i18n.formatLikert;
        default:               return i18n.formatOpen;
    }
}

type ScopeKey = string;

function scopeKey(parentId: number | null, choiceIndex: number | null): ScopeKey {
    return `${parentId ?? ''}|${choiceIndex ?? ''}`;
}

function bucketByScope(questions: Question[]): Map<ScopeKey, Question[]> {
    const buckets = new Map<ScopeKey, Question[]>();
    for (const q of questions) {
        const key = scopeKey(q.parentQuestionId, q.choiceIndex);
        const arr = buckets.get(key);
        if (arr) arr.push(q);
        else buckets.set(key, [q]);
    }
    for (const arr of buckets.values()) arr.sort((a, b) => a.position - b.position);
    return buckets;
}

function choiceEntriesFor(q: Question): Array<[number, string]> {
    if (q.questionFormat === 'SingleChoice' || q.questionFormat === 'MultipleChoice') {
        return q.answerChoices.map((text, idx) => [idx, text]);
    }
    if (q.questionFormat === 'Likert') {
        const count = q.answerChoiceCount > 0 ? q.answerChoiceCount : 10;
        return Array.from({ length: count }, (_, i) => [i, String(i + 1)]);
    }
    return [];
}

function escapeHtml(text: string): string {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

interface FormatConfig { iconBg: string; iconText: string; badge: string; svgPath: string }

const FORMAT_CONFIG: Record<string, FormatConfig> = {
    Open:           { iconBg: 'bg-info/10',      iconText: 'text-info',      badge: 'badge-info',      svgPath: 'M4 6h16M4 10h16M4 14h10' },
    MultipleChoice: { iconBg: 'bg-primary/10',   iconText: 'text-primary',   badge: 'badge-primary',   svgPath: 'M9 12l2 2 4-4M5 8h14M5 16h10' },
    SingleChoice:   { iconBg: 'bg-secondary/10', iconText: 'text-secondary', badge: 'badge-secondary', svgPath: 'M9 12l2 2 4-4M3 12a9 9 0 1018 0 9 9 0 00-18 0z' },
    Likert:         { iconBg: 'bg-warning/10',   iconText: 'text-warning',   badge: 'badge-warning',   svgPath: 'M7 20V10M12 20V4M17 20V14' },
};

const SECTION_COLORS: Array<{ bg: string; text: string; tint: string; gradFrom: string }> = [
    { bg: 'bg-primary/10',   text: 'text-primary',   tint: 'border-primary/30',   gradFrom: 'from-primary/10'   },
    { bg: 'bg-success/10',   text: 'text-success',   tint: 'border-success/30',   gradFrom: 'from-success/10'   },
    { bg: 'bg-info/10',      text: 'text-info',      tint: 'border-info/30',      gradFrom: 'from-info/10'      },
    { bg: 'bg-warning/10',   text: 'text-warning',   tint: 'border-warning/30',   gradFrom: 'from-warning/10'   },
    { bg: 'bg-secondary/10', text: 'text-secondary', tint: 'border-secondary/30', gradFrom: 'from-secondary/10' },
    { bg: 'bg-error/10',     text: 'text-error',     tint: 'border-error/30',     gradFrom: 'from-error/10'     },
];

function formatIconHtml(format: string): string {
    const cfg = FORMAT_CONFIG[format] ?? FORMAT_CONFIG['Open'];
    return `<div class="w-8 h-8 rounded-lg ${cfg.iconBg} flex items-center justify-center flex-shrink-0">
        <svg class="w-4 h-4 ${cfg.iconText}" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" d="${escapeHtml(cfg.svgPath)}"/>
        </svg>
    </div>`;
}

function sectionFolderHtml(color: { bg: string; text: string }): string {
    return `<div class="w-8 h-8 rounded-lg ${color.bg} flex items-center justify-center flex-shrink-0">
        <svg class="w-4 h-4 ${color.text}" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" d="M3 7a2 2 0 012-2h4l2 2h8a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2V7z"/>
        </svg>
    </div>`;
}

function mediaBadgeHtml(q: Question, i18n: QuestionsI18n): string {
    if (!q.mediaType) return '';
    const labels: Record<number, string> = { 1: i18n.mediaImage, 2: i18n.mediaVideo, 3: i18n.mediaAudio };
    return `<span class="badge badge-xs badge-ghost font-normal">${escapeHtml(labels[q.mediaType] ?? i18n.mediaGeneric)}</span>`;
}

function getAntiforgeryToken(): string {
    return document.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]')?.value ?? '';
}

function addQuestionDashedBtn(i18n: QuestionsI18n): string {
    return `
    <button type="button"
            class="open-add-modal drop-zone promote-zone w-full border-2 border-dashed border-base-300 hover:border-primary hover:bg-primary/5 rounded-xl py-4 text-base-content/40 hover:text-primary text-sm font-semibold transition-colors flex items-center justify-center gap-2 cursor-pointer"
            data-parent-id=""
            data-parent-choice-index="">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4"/>
        </svg>
        ${escapeHtml(i18n.addQuestion)}
    </button>`;
}

function renderCard(q: Question, depth: number, buckets: Map<ScopeKey, Question[]>, ctx: RenderContext, index = 0): string {
    const entries = choiceEntriesFor(q);
    const canBranch = entries.length > 0;
    const panelHidden = depth === 0 ? '' : 'hidden';
    const cfg = FORMAT_CONFIG[q.questionFormat] ?? FORMAT_CONFIG['Open'];
    const i18n = ctx.i18n;
    const formatLabel = labelForFormat(q.questionFormat, i18n);

    const deleteUrl = `${ctx.deleteEndpoint}?projectId=${ctx.projectId}&questionId=${q.questionId}&isNew=${ctx.isNew}`;

    const endingToggle = (trigger: string, endsSurvey: boolean) => ctx.locked
        ? (endsSurvey ? `<span class="text-xs text-base-content/40">${escapeHtml(i18n.endsSurveyLabel)}</span>` : '')
        : `<label class="text-xs text-base-content/60 flex items-center gap-1 cursor-pointer">
               <input type="checkbox"
                      class="ending-answer-toggle checkbox checkbox-xs"
                      data-question-id="${q.questionId}"
                      data-trigger-value="${escapeHtml(trigger)}"
                      ${endsSurvey ? 'checked' : ''} />
               ${escapeHtml(i18n.endsSurveyToggle)}
           </label>`;

    const childCount = entries.reduce((sum, [idx]) => {
        return sum + (buckets.get(scopeKey(q.questionId, idx))?.length ?? 0);
    }, 0);

    const answerGroups = canBranch
        ? entries.map(([idx, trigger]) => {
            const children = buckets.get(scopeKey(q.questionId, idx)) ?? [];
            const endsSurvey = q.endingAnswers.includes(trigger);
            const dropZone = ctx.locked ? '' : `
                <button type="button"
                        class="drop-zone add-sub-question-btn w-full border border-dashed border-base-content/20 hover:border-primary/40 hover:bg-primary/5 text-base-content/40 hover:text-primary text-center py-2 my-1 text-xs rounded-lg transition-colors cursor-pointer flex items-center justify-center gap-1.5"
                        data-parent-id="${q.questionId}"
                        data-parent-choice-index="${idx}">
                    <svg class="w-3 h-3" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4"/>
                    </svg>
                    ${escapeHtml(i18n.subDropZone)}
                </button>`;
            return `
                <div class="answer-group pl-4 border-l-2 border-base-300 ml-2 mt-2">
                    <div class="flex items-center gap-2 py-1.5 text-base-content/70 text-sm">
                        <span class="flex-1">↳ ${escapeHtml(trigger)}</span>
                        ${endingToggle(trigger, endsSurvey)}
                    </div>
                    ${children.map((c, ci) => renderCard(c, depth + 1, buckets, ctx, ci + 1)).join('')}
                    ${dropZone}
                </div>`;
        }).join('')
        : '';

    const grip = ctx.locked
        ? ''
        : `<span class="question-grip cursor-move text-base-content/25 select-none flex-shrink-0" title="${escapeHtml(i18n.dragTitle)}">⠿</span>`;

    const editBtn = ctx.locked ? '' : `
        <button type="button"
                class="question-edit-btn btn btn-ghost btn-xs btn-circle text-base-content/70 hover:text-base-content flex-shrink-0"
                data-question-id="${q.questionId}"
                data-text="${escapeHtml(q.text)}"
                data-format="${escapeHtml(q.questionFormat)}"
                data-answers='${escapeHtml(JSON.stringify(q.answerChoices))}'
                data-section-id="${q.sectionId ?? ''}"
                data-is-mandatory="${q.isMandatory}"
                data-media-url="${escapeHtml(q.mediaUrl ?? '')}"
                data-media-type="${q.mediaType ?? 0}"
                data-media-is-embed="${q.mediaIsEmbed}"
                data-media-text="${escapeHtml(q.mediaText ?? '')}">
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z"/>
            </svg>
        </button>`;

    const deleteBtn = ctx.locked ? '' : `
        <form method="post" action="${escapeHtml(deleteUrl)}" class="question-delete-form flex flex-shrink-0">
            <input type="hidden" name="__RequestVerificationToken" value="${getAntiforgeryToken()}" />
            <button type="submit"
                    class="btn btn-ghost btn-xs btn-circle text-base-content/70 hover:text-error">
                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/>
                </svg>
            </button>
        </form>`;

    const statsBtn = `
        <button type="button"
                class="question-stats-btn btn btn-ghost btn-xs btn-circle text-base-content/70 hover:text-primary flex-shrink-0"
                data-question-id="${q.questionId}"
                data-format="${escapeHtml(q.questionFormat)}"
                data-text="${escapeHtml(q.text)}"
                title="${escapeHtml(i18n.statsTitle)}">
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M18 20V10M12 20V4M6 20v-6"/>
            </svg>
        </button>`;

    const numDisplay = String(index).padStart(2, '0');

    const mandatoryBadge = q.isMandatory
        ? `<span class="badge badge-sm badge-success font-normal">${escapeHtml(i18n.mandatory)}</span>`
        : '';

    const subQBadge = childCount > 0
        ? `<span class="badge badge-sm badge-ghost font-normal">↳ ${childCount} ${escapeHtml(childCount === 1 ? i18n.followupsSingular : i18n.followupsPlural)}</span>`
        : '';

    if (depth === 0) {
        return `
            <div class="question-card bg-base-100 border border-base-200 rounded-xl overflow-hidden hover:border-primary/40 hover:shadow-sm transition-all cursor-pointer"
                 ${ctx.locked ? '' : 'draggable="true"'}
                 data-question-id="${q.questionId}"
                 data-format="${escapeHtml(q.questionFormat)}"
                 data-parent-id="${q.parentQuestionId ?? ''}"
                 data-parent-choice-index="${q.choiceIndex ?? ''}"
                 data-position="${q.position}">
                <div class="flex items-center gap-2 px-4 py-3 transition-colors cursor-pointer">
                    ${grip}
                    ${formatIconHtml(q.questionFormat)}
                    <button type="button"
                            class="question-toggle flex-1 text-left min-w-0 cursor-pointer"
                            data-question-id="${q.questionId}">
                        <div class="min-w-0 pointer-events-none">
                            <p class="text-sm font-medium text-base-content leading-snug">${escapeHtml(q.text)}</p>
                            <div class="flex gap-1 mt-1 flex-wrap items-center">
                                <span class="badge badge-sm ${escapeHtml(cfg.badge)} font-normal">${escapeHtml(formatLabel)}</span>
                                ${mandatoryBadge}
                                ${subQBadge}
                                ${mediaBadgeHtml(q, i18n)}
                            </div>
                        </div>
                    </button>
                    <span id="arrow-${q.questionId}" class="text-base-content/60 text-xs flex-shrink-0 pointer-events-none">${canBranch ? 'v' : ''}</span>
                    <span class="text-base-content/60 text-xs tabular-nums flex-shrink-0 pointer-events-none">${numDisplay}</span>
                    ${statsBtn}
                    ${editBtn}
                    ${deleteBtn}
                </div>
                ${canBranch ? `
                    <div id="answers-${q.questionId}" class="${panelHidden} border-t border-base-300 bg-base-200/20 px-4 py-3">
                        ${answerGroups}
                    </div>` : ''}
            </div>`;
    }

    // Sub-question (depth > 0)
    return `
        <div class="question-card border border-base-300 bg-base-100 mb-1.5 rounded-lg overflow-hidden"
             ${ctx.locked ? '' : 'draggable="true"'}
             data-question-id="${q.questionId}"
             data-format="${escapeHtml(q.questionFormat)}"
             data-parent-id="${q.parentQuestionId ?? ''}"
             data-parent-choice-index="${q.choiceIndex ?? ''}"
             data-position="${q.position}">
            <div class="flex items-center gap-2 px-3 py-2">
                ${grip}
                <button type="button"
                        class="question-toggle flex-1 text-left min-w-0"
                        data-question-id="${q.questionId}">
                    <span class="block text-sm text-base-content/80">${escapeHtml(q.text)}</span>
                    <div class="flex gap-1 mt-0.5 flex-wrap">
                        <span class="badge badge-xs badge-ghost text-base-content/50 font-normal">${escapeHtml(formatLabel)}</span>
                        ${mandatoryBadge}
                    </div>
                </button>
                <span id="arrow-${q.questionId}" class="text-base-content/25 text-xs flex-shrink-0">${canBranch ? '>' : ''}</span>
                ${statsBtn}
                ${editBtn}
                ${deleteBtn}
            </div>
            ${canBranch ? `
                <div id="answers-${q.questionId}" class="${panelHidden} border-t border-base-300 px-3 py-2">
                    ${answerGroups}
                </div>` : ''}
        </div>`;
}

function renderSection(
    sectionName: string | null,
    sectionId: number | null,
    questions: Question[],
    buckets: Map<ScopeKey, Question[]>,
    ctx: RenderContext,
    colorIdx: number,
): string {
    const cards = questions.map((q, idx) => renderCard(q, 0, buckets, ctx, idx + 1)).join('');

    if (!sectionName) {
        return `<div class="unsectioned-group mb-4 flex flex-col gap-2">${cards}</div>`;
    }

    const color = SECTION_COLORS[colorIdx % SECTION_COLORS.length];
    const count = questions.length;
    const i18n = ctx.i18n;
    const addBtn = (!ctx.locked && sectionId !== null) ? `
        <button type="button"
                class="add-in-section-btn inline-flex items-center gap-1.5 text-xs font-semibold ${color.text} bg-base-100 border border-base-200 hover:border-current rounded-md px-2.5 py-1.5 transition-colors flex-shrink-0"
                data-section-id="${sectionId}">
            <svg class="w-3 h-3" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4"/>
            </svg>
            ${escapeHtml(i18n.addInSection)}
        </button>` : '';

    const emptyState = (count === 0 && !ctx.locked && sectionId !== null) ? `
        <button type="button"
                class="add-in-section-btn w-full border-2 border-dashed border-base-300 hover:border-primary/40 hover:bg-primary/5 text-base-content/40 hover:text-primary text-sm rounded-lg py-5 flex items-center justify-center gap-2 transition-colors"
                data-section-id="${sectionId}">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4"/>
            </svg>
            ${escapeHtml(i18n.emptySectionAdd)}
        </button>` : '';

    return `
        <div class="section-group mb-4 bg-base-100 border border-base-200 rounded-2xl overflow-hidden shadow-sm">
            <div class="flex items-center gap-3 px-4 py-3 border-b border-base-200 bg-gradient-to-r ${color.gradFrom} to-transparent">
                ${sectionFolderHtml(color)}
                <div class="flex-1 min-w-0">
                    <div class="font-bold text-sm text-base-content">${escapeHtml(sectionName)}</div>
                    <div class="text-base-content/50 text-xs">${count} ${escapeHtml(count === 1 ? i18n.questionSingular : i18n.questionPlural)}</div>
                </div>
                ${addBtn}
            </div>
            <div class="p-3 flex flex-col gap-2">
                ${cards}
                ${emptyState}
            </div>
        </div>`;
}

export function renderTree(list: HTMLElement, questions: Question[], ctx: RenderContext): void {
    const buckets = bucketByScope(questions);
    const topLevel = buckets.get(scopeKey(null, null)) ?? [];

    if (topLevel.length === 0) {
        list.innerHTML = `
            <p class="text-base-content/40 italic mb-4 text-sm">${escapeHtml(ctx.i18n.emptyNoQuestions)}</p>
            ${ctx.locked ? '' : addQuestionDashedBtn(ctx.i18n)}`;
        return;
    }

    // Group top-level questions by section, preserving encounter order
    const sectionOrder: Array<{ name: string | null; id: number | null }> = [];
    const sectionMap = new Map<string, Question[]>();

    for (const q of topLevel) {
        const key = q.sectionId !== null ? String(q.sectionId) : '__none__';
        if (!sectionMap.has(key)) {
            sectionOrder.push({ name: q.sectionName, id: q.sectionId });
            sectionMap.set(key, []);
        }
        sectionMap.get(key)!.push(q);
    }

    let html = '';
    let colorIdx = 0;
    for (const { name, id } of sectionOrder) {
        const key = id !== null ? String(id) : '__none__';
        const qs = sectionMap.get(key) ?? [];
        html += renderSection(name, id, qs, buckets, ctx, name ? colorIdx++ : -1);
    }

    if (!ctx.locked) {
        html += addQuestionDashedBtn(ctx.i18n);
    }

    list.innerHTML = html;
}
