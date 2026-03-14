/** Translates the track so slide at `index` is visible. */
export function animateTrackTo(track: HTMLElement, index: number): void {
    track.style.transform = `translateY(calc(${index} * -1 * var(--slide-h)))`;
}

/** Shakes the answer area to indicate a blocked action. */
export function shakeSlide(slide: HTMLElement): void {
    const target = slide.querySelector<HTMLElement>('.slide-answer-area') ?? slide;
    target.classList.remove('slide--shake');
    void target.offsetWidth; // force reflow to restart animation
    target.classList.add('slide--shake');
    target.addEventListener('animationend', () => target.classList.remove('slide--shake'), {once: true});
}

export function updateProgressBar(bar: HTMLElement | null, index: number, total: number): void {
    if (!bar) return;
    const pct = total <= 1 ? 100 : Math.round((index / (total - 1)) * 100);
    bar.style.width = `${pct}%`;
}
