import type {SurveyState} from './types';
import {navigateNext, navigatePrev} from './navigation';
import {initGestures} from './gestures';
import {updateProgressBar} from './animation';
import {initCharCounter} from './validation';

function syncButtons(state: SurveyState): void {
    const btnPrev = document.getElementById('btn-prev') as HTMLButtonElement | null;
    const btnNext = document.getElementById('btn-next') as HTMLButtonElement | null;

    if (btnPrev) btnPrev.disabled = state.currentIndex === 0;
    if (btnNext) btnNext.style.visibility =
        state.currentIndex >= state.slides.length - 1 ? 'hidden' : 'visible';
}

export function initSurvey(): void {
    const container = document.getElementById('survey-container') as HTMLElement | null;
    if (!container) return;

    const slides = Array.from(container.querySelectorAll<HTMLElement>('.survey-slide'));
    if (!slides.length) return;

    const progressBar = document.getElementById('survey-progress-bar');
    const btnNext = document.getElementById('btn-next') as HTMLButtonElement | null;
    const btnPrev = document.getElementById('btn-prev') as HTMLButtonElement | null;

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

    btnNext?.addEventListener('click', () => navigateNext(state));
    btnPrev?.addEventListener('click', () => navigatePrev(state));

    initGestures(container, state);
}
