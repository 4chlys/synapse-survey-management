import type { SurveyState } from './types';
import { navigateNext, navigatePrev } from './navigation';

export function initGestures(container: HTMLElement, state: SurveyState): void {
  let touchStartY = 0;
  let canScroll = true;

  // Mouse wheel
  container.addEventListener('wheel', (e: WheelEvent) => {
    e.preventDefault();
    if (!canScroll) return;
    canScroll = false;

    if (e.deltaY > 0) navigateNext(state);
    else if (e.deltaY < 0) navigatePrev(state);

    setTimeout(() => { canScroll = true; }, 800);
  }, { passive: false });

  // Touch swipe
  container.addEventListener('touchstart', (e: TouchEvent) => {
    touchStartY = e.touches[0].clientY;
  }, { passive: true });

  container.addEventListener('touchend', (e: TouchEvent) => {
    const delta = touchStartY - e.changedTouches[0].clientY;
    if (Math.abs(delta) < 50) return;
    if (delta > 0) navigateNext(state);
    else navigatePrev(state);
  }, { passive: true });

  // Keyboard
  window.addEventListener('keydown', (e: KeyboardEvent) => {
    const active = document.activeElement?.tagName;
    if (active === 'TEXTAREA' || active === 'INPUT') return;

    if (e.key === 'ArrowDown' || e.key === 'ArrowRight') {
      e.preventDefault();
      navigateNext(state);
    } else if (e.key === 'ArrowUp' || e.key === 'ArrowLeft') {
      e.preventDefault();
      navigatePrev(state);
    }
  });
}
