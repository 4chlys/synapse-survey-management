import type { SurveyState } from './types';
import { isSlideValid, showValidationError, clearValidationError } from './validation';
import { animateTrackTo, shakeSlide, updateProgressBar } from './animation';

const TRANSITION_MS = 580;

export function navigateNext(state: SurveyState): void {
  if (state.isAnimating) return;
  if (state.currentIndex >= state.slides.length - 1) return;

  const current = state.slides[state.currentIndex];

  const { valid, message } = isSlideValid(current);
  if (!valid) {
    showValidationError(current, message);
    shakeSlide(current);
    return;
  }

  clearValidationError(current);
  state.isAnimating = true;
  state.currentIndex++;
  animateTrackTo(state.track, state.currentIndex);
  updateProgressBar(state.progressBar, state.currentIndex, state.slides.length);
  state.onSlideChange?.(state);

  setTimeout(() => { state.isAnimating = false; }, TRANSITION_MS);
}

export function navigatePrev(state: SurveyState): void {
  if (state.isAnimating) return;
  if (state.currentIndex <= 0) return;

  const current = state.slides[state.currentIndex];
  clearValidationError(current);

  state.isAnimating = true;
  state.currentIndex--;
  animateTrackTo(state.track, state.currentIndex);
  updateProgressBar(state.progressBar, state.currentIndex, state.slides.length);
  state.onSlideChange?.(state);

  setTimeout(() => { state.isAnimating = false; }, TRANSITION_MS);
}
