import type {SurveyState} from './types';
import {navigateNext, navigatePrev} from './navigation';

export function initGestures(container: HTMLElement, state: SurveyState): void {
    let touchStartX = 0;
    let touchStartY = 0;
    let canScroll = true;

    // Mouse wheel
    container.addEventListener('wheel', (e: WheelEvent) => {
        e.preventDefault();
        if (!canScroll) return;
        canScroll = false;

        if (e.deltaY > 0) void navigateNext(state);
        else if (e.deltaY < 0) void navigatePrev(state);

        setTimeout(() => {
            canScroll = true;
        }, 800);
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
        if (!canScroll) return;
        canScroll = false;
        if (deltaY > 0) void navigateNext(state);
        else void navigatePrev(state);
        setTimeout(() => {
            canScroll = true;
        }, 800);
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
