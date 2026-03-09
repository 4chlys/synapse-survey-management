import type { SurveyState } from './types';
import { navigateNext, navigatePrev } from './navigation';
import { initGestures } from './gestures';
import { animateTrackTo, updateProgressBar } from './animation';

function syncButtons(state: SurveyState): void {
  const btnPrev = document.getElementById('btn-prev') as HTMLButtonElement | null;
  const btnNext = document.getElementById('btn-next') as HTMLButtonElement | null;

  if (btnPrev) btnPrev.disabled = state.currentIndex === 0;
  if (btnNext) btnNext.style.visibility = state.currentIndex >= state.slides.length - 1 ? 'hidden' : 'visible';
}

export function initSurvey(): void {
  const track = document.getElementById('survey-track') as HTMLElement | null;
  if (!track) return;

  const container = document.getElementById('survey-container') as HTMLElement;
  const slides = Array.from(track.querySelectorAll<HTMLElement>('.survey-slide'));
  const progressBar = document.getElementById('survey-progress-bar');
  const btnNext = document.getElementById('btn-next') as HTMLButtonElement | null;
  const btnPrev = document.getElementById('btn-prev') as HTMLButtonElement | null;

  const state: SurveyState = {
    currentIndex: 0,
    slides,
    track,
    isAnimating: false,
    progressBar,
    onSlideChange: syncButtons,
  };

  animateTrackTo(track, 0);
  updateProgressBar(progressBar, 0, slides.length);
  syncButtons(state);

  btnNext?.addEventListener('click', () => navigateNext(state));
  btnPrev?.addEventListener('click', () => navigatePrev(state));

  if (container) initGestures(container, state);
}
