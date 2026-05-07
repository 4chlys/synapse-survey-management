type Strings = {
    submit: string;
    cancel: string;
    back: string;
    next: string;
    uploading: string;
    checking: string;
    contentNotAllowed: string;
    maxLengthExceeded: string;
    submitFallback: string;
    ideaCardStatusFlagged: string;
    ideaCardStatusOwnIdea: string;
    ideaCardUseAsStartingPoint: string;
    imageUploadInvalidType: string;
    imageUploadTooLarge: string;
    imageUploadFailed: string;
    imageUploadNotAllowed: string;
    speechToTextEmptyRecording: string;
    speechToTextTooManyRequests: string;
    speechToTextUnavailable: string;
    surveyYourAnswer: string;
    surveyChoiceOptions: string;
    surveyConfirm: string;
    surveySaveError: string;
    surveyLoadError: string;
    surveyUnknownType: string;
    surveyReadyToSubmit: string;
    surveySubmitError: string;
    surveyDone: string;
    surveyRestartConfirm: string;
    surveyRestartError: string;
    surveySubmitting: string;
    surveySubmitAnswers: string;
    aiChatAssistantUnavailableIdea: string;
    aiChatError: string;
    aiChatDraftLabel: string;
    aiChatDraftYes: string;
    aiChatDraftNo: string;
    aiChatNoProblem: string;
    aiChatRefining: string;
    aiChatRefinedText: string;
    aiChatAlreadyGood: string;
    aiChatRefineError: string;
    aiChatRefine: string;
    aiChatAssistantUnavailableComment: string;
    aiChatChooseVersion: string;
    aiChatYourVersion: string;
    aiChatAiSuggestion: string;
    aiChatYourComment: string;
    surveyAnswerTooLong: string;
    surveyAnswerRequired: string;
};

declare global {
    interface Window {
        __i18n: { culture: string; strings: Strings };
    }
}

export const t = <K extends keyof Strings>(key: K): string => window.__i18n.strings[key];
export const currentCulture = (): string => window.__i18n.culture;
