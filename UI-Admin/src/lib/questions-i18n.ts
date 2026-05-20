// Loads the page-specific i18n blob rendered by Questions.cshtml's
// <script id="qs-i18n" type="application/json"> element.

import type { QuestionsI18n } from './questionnaire-tree';

export interface QuestionsPageI18n extends QuestionsI18n {
    modalTitleCreate: string;
    modalTitleEdit: string;
    answerPlaceholder: string;
    savedToast: string;
    saveFailedToast: string;
    fieldRequired: string;
    maxQuestionText: string;
    minTwoAnswers: string;
    allAnswersFilled: string;
    addOneQuestion: string;
}

let cached: QuestionsPageI18n | null = null;

export function loadQuestionsPageI18n(): QuestionsPageI18n {
    if (cached) return cached;
    const el = document.getElementById('qs-i18n');
    const raw = el?.textContent?.trim() ?? '{}';
    cached = JSON.parse(raw) as QuestionsPageI18n;
    return cached;
}

export function loadQuestionsI18n(): QuestionsI18n {
    return loadQuestionsPageI18n();
}
