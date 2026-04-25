import type {SurveyState} from './types';
import {navigateNext, navigatePrev} from './navigation';

export function initGestures(container: HTMLElement, state: SurveyState): void {
    let touchStartX = 0;
    let touchStartY = 0;

    // Mouse wheel / trackpad debounce.
    //
    // Uses a fixed cooldown measured from navigation time rather than an
    // event-resetting timer. Event-resetting timers cause two failure modes:
    //   - Lockout: continuous momentum events endlessly defer the timer.
    //   - Double scroll: a timer that expires early lets stray momentum fire a second nav.
    //
    // Anything arriving within POST_NAV_BLOCK_MS of the last navigation is dropped
    // silently. isAnimating covers the CSS transition; the timestamp check covers
    // the trackpad momentum tail (macOS can send momentum for ~1 s after gesture ends).
    const POST_NAV_BLOCK_MS = 900;
    let lastNavTime = 0;

    container.addEventListener('wheel', (e: WheelEvent) => {
        e.preventDefault();

        if (state.isAnimating) return;
        if (Date.now() - lastNavTime < POST_NAV_BLOCK_MS) return;

        if (e.deltaY > 0) {
            lastNavTime = Date.now();
            void navigateNext(state);
        } else if (e.deltaY < 0) {
            lastNavTime = Date.now();
            void navigatePrev(state);
        }
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
