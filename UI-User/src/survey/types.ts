export interface SurveyState {
    currentIndex: number;
    slides: HTMLElement[];
    isAnimating: boolean;
    progressBar: HTMLElement | null;
    onSlideChange?: (state: SurveyState) => void;
    visibleIndices: number[];
    projectId: number;
    hasSession: boolean;
    maxProgressPct: number;
    lastForwardTargets: Map<number, number>;
}

export interface QuestionData {
    questionId: number;
    text: string;
    format: number;
    answerChoices?: string[];
    questionNumber: number;
    totalQuestions: number;
    isMandatory: boolean;
    position: number;
    mediaUrl?: string;
    mediaType?: number;
    mediaIsEmbed: boolean;
    mediaText?: string;
}

export interface QuestionEntry {
    qid: number;
    sysBubble: HTMLElement;
    ansBubble: HTMLElement | null;
}

export interface BubbleMedia {
    imageUrl?: string;
    videoUrl?: string;
    audioUrl?: string;
    isEmbed?: boolean;
    extraText?: string;
    isHtml?: boolean;
}

export interface ChipsOptions {
    multi: boolean;
    onSelect?: (value: string, sourceEl?: HTMLElement) => void;
    onConfirm?: (values: string[], sourceEl?: HTMLElement) => void;
}

export interface Ctx {
    projectId: number;
    questions: QuestionData[];
    questionIndexById: Map<number, number>;
    existingResponses: Record<string, string>;
    hasSession: boolean;
    messagesEl: HTMLElement;
    optionsRow: HTMLElement;
    inputRow: HTMLElement;
    inputEl: HTMLTextAreaElement;
    sendBtn: HTMLButtonElement;
    rootEl: HTMLElement;
    backBtns: HTMLButtonElement[];
    restartBtns: HTMLButtonElement[];
    questionStack: QuestionEntry[];
    activeChips: HTMLElement | null;
    confirmBubble: HTMLElement | null;
    userTypingBubble: HTMLElement | null;
    openHandlers: { send: () => void; key: (e: KeyboardEvent) => void } | null;
    introText: string;
    introImage: string;
    introShown: boolean;
}