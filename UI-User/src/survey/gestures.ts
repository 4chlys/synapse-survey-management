import type {SurveyState} from './types';
import {navigateNext, navigatePrev} from './navigation';

export function initGestures(container: HTMLElement, state: SurveyState): void {
    let touchStartX = 0;
    let touchStartY = 0;

    // Mouse wheel / trackpad — two-phase debounce:
    //
    // Phase 1 (isAnimating = true, 580 ms):
    //   Drop every event silently. Do NOT touch the timer — events during
    //   animation must not extend the post-animation lockout.
    //
    // Phase 2 (after animation, timer running):
    //   Momentum events keep arriving. Each one resets the timer to 80 ms.
    //   Only when the trackpad has been truly quiet for 80 ms does the timer
    //   expire and unlock the next navigation.
    //
    // The initial timer after a navigation is set to TRANSITION_MS + 80 ms
    // (= 660 ms) so it cannot fire while the animation is still running —
    // it only becomes relevant for post-animation momentum tails.
    const TRANSITION_MS = 580;
    let wheelTimer: ReturnType<typeof setTimeout> | null = null;

    container.addEventListener('wheel', (e: WheelEvent) => {
        e.preventDefault();

        // Phase 1: silently drop events during animation.
        if (state.isAnimating) return;

        // Phase 2: post-animation momentum — keep resetting the quiet window.
        if (wheelTimer !== null) {
            clearTimeout(wheelTimer);
            wheelTimer = setTimeout(() => { wheelTimer = null; }, 80);
            return;
        }

        // Unlocked — navigate and arm the timer.
        if (e.deltaY > 0) void navigateNext(state);
        else if (e.deltaY < 0) void navigatePrev(state);
        wheelTimer = setTimeout(() => { wheelTimer = null; }, TRANSITION_MS + 80);
    }, {passive: false});

    // Touch swipe (mobile)
    container.addEventListener('touchstart', (e: TouchEvent) => {
        touchStartX = e.touches[0].clientX;
        touchStartY = e.touches[0].clientY;
    }, {passive: true});

    container.addEventListener('touchend', (e: TouchEvent) => {
        const deltaY = touchStartY - e.changedTouches[0].clientY;
        const deltaX = touchStartX - e.changedTouches[0].clientX;
        if (Math.abs(deltaX) > Math.abs(deltaY)) return;
        if (Math.abs(deltaY) < 50) return;
        if (state.isAnimating) return;
        if (deltaY > 0) void navigateNext(state);
        else void navigatePrev(state);
    }, {passive: true});

    // Keyboard
    window.addEventListener('keydown', (e: KeyboardEvent) => {
        const active = document.activeElement?.tagName;
        if (active === 'TEXTAREA' || active === 'INPUT') return;

        if (e.key === 'ArrowDown' || e.key === 'ArrowRight') {
            e.preventDefault();
            void navigateNext(state);
        } else if (e.key === 'ArrowUp' || e.key === 'ArrowLeft') {
            e.preventDefault();
            void navigatePrev(state);
        }
    });
}
