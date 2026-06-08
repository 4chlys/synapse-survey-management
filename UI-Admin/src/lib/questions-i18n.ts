// Reads the questionnaire-builder i18n strings from the data-i18n-*
// attributes set by Questions.cshtml: #questions-list for the tree
// renderer (and shared save toasts), #question-form for the modal/form.
// Mirrors how the dashboard widgets read their strings via
// question-widget-shell.

import type { QuestionsI18n } from './questionnaire-tree';

export interface QuestionsFormI18n {
    modalTitleCreate: string;
    modalTitleEdit: string;
    answerPlaceholder: string;
    fieldRequired: string;
    maxQuestionText: string;
    minTwoAnswers: string;
    allAnswersFilled: string;
}

export interface MediaI18n {
    placeholderImage: string;
    placeholderVideo: string;
    placeholderAudio: string;
    labelImage: string;
    labelVideo: string;
    labelAudio: string;
    errNoAccess: string;
    errTooLarge: string;
    errTooManyUploads: string;
    errUploadFailedRetry: string;
    errUploadFailed: string;
    errTooLargeMax: string; // contains a {0} placeholder for the MB limit
    errUrlScheme: string;
    errVideoUrl: string;
    errAudioUrl: string;
    videoUrlLabel: string;
    removeTitle: string;
}

export function readQuestionsI18n(el: HTMLElement | null): QuestionsI18n {
    const ds = el?.dataset;
    return {
        addQuestion:       ds?.i18nAddQuestion       ?? '',
        emptyNoQuestions:  ds?.i18nEmptyNoQuestions  ?? '',
        emptySectionAdd:   ds?.i18nEmptySectionAdd   ?? '',
        addInSection:      ds?.i18nAddInSection      ?? '',
        followupsSingular: ds?.i18nFollowupsSingular ?? '',
        followupsPlural:   ds?.i18nFollowupsPlural   ?? '',
        subDropZone:       ds?.i18nSubDropZone       ?? '',
        promoteDropZone:   ds?.i18nPromoteDropZone   ?? '',
        endsSurveyLabel:   ds?.i18nEndsSurveyLabel   ?? '',
        endsSurveyToggle:  ds?.i18nEndsSurveyToggle  ?? '',
        dragTitle:         ds?.i18nDragTitle         ?? '',
        questionSingular:  ds?.i18nQuestionSingular  ?? '',
        questionPlural:    ds?.i18nQuestionPlural    ?? '',
        mandatory:         ds?.i18nMandatory         ?? '',
        formatOpen:        ds?.i18nFormatOpen        ?? '',
        formatMCQ:         ds?.i18nFormatMcq         ?? '',
        formatSCQ:         ds?.i18nFormatScq         ?? '',
        formatLikert:      ds?.i18nFormatLikert      ?? '',
        mediaImage:        ds?.i18nMediaImage        ?? '',
        mediaVideo:        ds?.i18nMediaVideo        ?? '',
        mediaAudio:        ds?.i18nMediaAudio        ?? '',
        mediaGeneric:      ds?.i18nMediaGeneric      ?? '',
        statsTitle:        ds?.i18nStatsTitle        ?? '',
        statsButtonLabel:  ds?.i18nStatsButtonLabel  ?? '',
        savedToast:        ds?.i18nSavedToast        ?? '',
        saveFailedToast:   ds?.i18nSaveFailedToast   ?? '',
    };
}

export function readFormI18n(el: HTMLElement | null): QuestionsFormI18n {
    const ds = el?.dataset;
    return {
        modalTitleCreate:  ds?.i18nModalTitleCreate  ?? '',
        modalTitleEdit:    ds?.i18nModalTitleEdit    ?? '',
        answerPlaceholder: ds?.i18nAnswerPlaceholder ?? '',
        fieldRequired:     ds?.i18nFieldRequired     ?? '',
        maxQuestionText:   ds?.i18nMaxQuestionText   ?? '',
        minTwoAnswers:     ds?.i18nMinTwoAnswers     ?? '',
        allAnswersFilled:  ds?.i18nAllAnswersFilled  ?? '',
    };
}

export function readMediaI18n(el: HTMLElement | null): MediaI18n {
    const ds = el?.dataset;
    return {
        placeholderImage:     ds?.i18nMediaPlaceholderImage    ?? '',
        placeholderVideo:     ds?.i18nMediaPlaceholderVideo    ?? '',
        placeholderAudio:     ds?.i18nMediaPlaceholderAudio    ?? '',
        labelImage:           ds?.i18nMediaLabelImage          ?? '',
        labelVideo:           ds?.i18nMediaLabelVideo          ?? '',
        labelAudio:           ds?.i18nMediaLabelAudio          ?? '',
        errNoAccess:          ds?.i18nMediaErrNoAccess         ?? '',
        errTooLarge:          ds?.i18nMediaErrTooLarge         ?? '',
        errTooManyUploads:    ds?.i18nMediaErrTooManyUploads   ?? '',
        errUploadFailedRetry: ds?.i18nMediaErrUploadFailedRetry ?? '',
        errUploadFailed:      ds?.i18nMediaErrUploadFailed     ?? '',
        errTooLargeMax:       ds?.i18nMediaErrTooLargeMax      ?? '',
        errUrlScheme:         ds?.i18nMediaErrUrlScheme        ?? '',
        errVideoUrl:          ds?.i18nMediaErrVideoUrl         ?? '',
        errAudioUrl:          ds?.i18nMediaErrAudioUrl         ?? '',
        videoUrlLabel:        ds?.i18nMediaVideoUrlLabel       ?? '',
        removeTitle:          ds?.i18nMediaRemoveTitle         ?? '',
    };
}
