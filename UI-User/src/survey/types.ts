export interface SurveyState {
    currentIndex: number;
    slides: HTMLElement[];
    isAnimating: boolean;
    progressBar: HTMLElement | null;
    onSlideChange?: (state: SurveyState) => void;
}
