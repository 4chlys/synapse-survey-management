import type Quill from 'quill';

export interface WidgetOption {
    value: string;
    label: string;
    cat: string;
    span: number;
}

export interface ModalStrings {
    loadFailed: string;
    responsesUnit: string;
    xAxisLabel: string;
}

export interface OpenAnswerPage {
    questionId: number;
    totalAnswers: number;
    answers: { value: string; createdAt: string }[];
}

export interface PreviewRefs {
    shadow: ShadowRoot;
    themeStyle: HTMLStyleElement;
    primaryStyle: HTMLStyleElement;
    userStyle: HTMLStyleElement;
    wrapper: HTMLDivElement;
}

export interface QuestionTranslationData {
    languageCode: string;
    text: string;
    answerChoices: string[];
    mediaText: string | null;
}

export interface EndingAnswerTranslationData {
    languageCode: string;
    value: string;
}

export interface EndingAnswerSlotData {
    endingAnswerId: number;
    translations: EndingAnswerTranslationData[];
}

export interface LangCtx {
    code: string;
    pane: HTMLElement;
    tab: HTMLButtonElement;
    nameInput: HTMLInputElement;
    topicInput: HTMLInputElement;
    introHidden: HTMLInputElement;
    introCounter: HTMLElement | null;
    quill: Quill | null;
}
