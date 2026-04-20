import type {SurveyState} from './types';
import {navigateNext, navigatePrev} from './navigation';
import {initGestures} from './gestures';
import {updateProgressBar} from './animation';
import {initCharCounter} from './validation';

function syncButtons(state: SurveyState): void {
    const allPrev = document.querySelectorAll<HTMLButtonElement>('.btn-prev');
    const allNext = document.querySelectorAll<HTMLButtonElement>('.btn-next');

    allPrev.forEach(btn => btn.disabled = state.currentIndex === 0);
    allNext.forEach(btn => btn.style.visibility =
        state.currentIndex >= state.slides.length - 1 ? 'hidden' : 'visible');
}

export function initSurvey(): void {
    const container = document.getElementById('survey-container') as HTMLElement | null;
    if (!container) return;

    const slides = Array.from(container.querySelectorAll<HTMLElement>('.survey-slide'));
    if (!slides.length) return;

    const progressBar = document.getElementById('survey-progress-bar');


    const state: SurveyState = {
        currentIndex: 0,
        slides,
        isAnimating: false,
        progressBar,
        onSlideChange: syncButtons,
    };

    slides.forEach(slide => initCharCounter(slide));

    // Activate first slide — skip transition by adding class directly
    slides[0].classList.add('is-active');
    updateProgressBar(progressBar, 0, slides.length);
    syncButtons(state);

    container.querySelectorAll<HTMLButtonElement>('.btn-next')
        .forEach(btn => btn.addEventListener('click', () => navigateNext(state)));
    container.querySelectorAll<HTMLButtonElement>('.btn-prev')
        .forEach(btn => btn.addEventListener('click', () => navigatePrev(state)));

    initGestures(container, state);
}
