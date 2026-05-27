/**
 * Transitions from slide at `prevIndex` to slide at `nextIndex`.
 * Going forward: prev exits upward, next rises from below.
 * Going backward: prev falls back down, next descends from above.
 *
 * `goingForward` must be derived from the visible-position order, not the raw
 * array index, because sub-questions can have a lower array index than later
 * top-level questions (Position is scoped per sibling group, not globally).
 */
function stopSlideMedia(slide: HTMLElement): void {
    slide.querySelectorAll<HTMLVideoElement | HTMLAudioElement>('video, audio').forEach(el => el.pause());
    slide.querySelectorAll<HTMLIFrameElement>('iframe').forEach(iframe => {
        if (iframe.src.includes('youtube.com')) {
            iframe.contentWindow?.postMessage(JSON.stringify({ event: 'command', func: 'pauseVideo', args: '' }), '*');
        } else if (iframe.src.includes('vimeo.com')) {
            iframe.contentWindow?.postMessage(JSON.stringify({ method: 'pause' }), '*');
        }
    });
}

export function activateSlide(
    slides: HTMLElement[],
    prevIndex: number,
    nextIndex: number,
    goingForward: boolean
): void {
    const prev = slides[prevIndex];
    const next = slides[nextIndex];

    if (prev) {
        stopSlideMedia(prev);
        prev.classList.remove('is-active');
        prev.setAttribute('inert', '');
        if (goingForward) {
            prev.classList.add('is-above');
        }
    }

    if (next) {
        next.classList.remove('is-above');
        next.classList.add('is-active');
        next.removeAttribute('inert');
    }
}

/** Shakes the answer area to signal a blocked navigation. */
export function shakeSlide(slide: HTMLElement): void {
    const target = slide.querySelector<HTMLElement>('.slide-answer-area') ?? slide;
    target.classList.remove('slide--shake');
    void target.offsetWidth; // force reflow to restart animation
    target.classList.add('slide--shake');
    target.addEventListener('animationend', () => target.classList.remove('slide--shake'), { once: true });
}

export function updateProgressBar(bar: HTMLElement | null, index: number, total: number, minPct: number): number {
    const raw = total <= 1 ? 100 : Math.round((index / (total - 1)) * 100);
    const pct = Math.max(raw, minPct);
    if (bar) {
        bar.style.width = `${pct}%`;
        bar.parentElement?.setAttribute('aria-valuenow', String(pct));
    }
    return pct;
}
