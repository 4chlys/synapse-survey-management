/** Translates the track so slide at `index` is visible. */
export function animateTrackTo(track: HTMLElement, index: number, visibleIndices : number[]): void {
    const visiblePosition = visibleIndices.indexOf(index);
    track.style.transform = `translateY(calc(${visiblePosition} * -1 * var(--slide-h)))`;
}

export function syncTrackToVisibleSlides(track: HTMLElement, slides: HTMLElement[], visibleIndices: number[]):void{
    // Remove all slides from track
    while (track.firstChild) track.removeChild(track.firstChild);
    
    // Re-insert only visible slides in order
    for (const index of visibleIndices) {
        track.appendChild(slides[index]);
    }
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
