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
    // Maps each slide index to the next slide the user went to from it.
    // Used to detect when a changed answer sends the user down a different branch.
    lastForwardTargets: Map<number, number>;
}
