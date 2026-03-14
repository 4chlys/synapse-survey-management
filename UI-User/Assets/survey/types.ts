export interface SurveyState {
    currentIndex: number;
    slides: HTMLElement[];
    track: HTMLElement;
    isAnimating: boolean;
    progressBar: HTMLElement | null;
    onSlideChange?: (state: SurveyState) => void;
}
