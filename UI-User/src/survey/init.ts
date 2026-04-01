import type {SurveyState} from './types';
import {navigateNext, navigatePrev} from './navigation';
import {initGestures} from './gestures';
import {animateTrackTo, updateProgressBar, syncTrackToVisibleSlides} from './animation';
import {initCharCounter} from './validation';
import {prefillAnswers, questionIdToSlideIndex, getSubmitSlideIndex, updateQuestionCounters} from './branching';
import {getResumeQuestion, getVisiblePath} from './api';

function syncButtons(state: SurveyState): void {
    const btnPrev = document.getElementById('btn-prev') as HTMLButtonElement | null;
    const btnNext = document.getElementById('btn-next') as HTMLButtonElement | null;

    const isFirstVisible = state.visibleIndices.indexOf(state.currentIndex) === 0;
    const isSubmitSlide = state.currentIndex === getSubmitSlideIndex(state);

    if (btnPrev) btnPrev.disabled = isFirstVisible;
    if (btnNext) btnNext.style.visibility = isSubmitSlide ? 'hidden' : 'visible';
}

export function initSurvey(): void {
    const track = document.getElementById('survey-track') as HTMLElement | null;
    if (!track) return;

    const container = document.getElementById('survey-container') as HTMLElement;
    const slides = Array.from(track.querySelectorAll<HTMLElement>('.survey-slide'));
    const progressBar = document.getElementById('survey-progress-bar');
    const btnNext = document.getElementById('btn-next') as HTMLButtonElement | null;
    const btnPrev = document.getElementById('btn-prev') as HTMLButtonElement | null;

    const projectId = parseInt(container.dataset.projectId ?? '0', 10);
    const hasSession = container.dataset.hasSession === 'true';

    // All slides visible initially so the counter shows the correct total from the start.
    // navigateNext trims stale slides when branching kicks in.
    // On resume, this is overwritten by the server-computed visible path.
    const submitIndex = slides.length - 1;
    const state: SurveyState = {
        currentIndex: 0,
        slides,
        track,
        isAnimating: false,
        progressBar,
        onSlideChange: syncButtons,
        visibleIndices: slides.map((_, i) => i),
        projectId,
        hasSession,
    };

    slides.forEach(slide => initCharCounter(slide));

    // Load existing responses for resume and prefill
    const existingResponsesEl = document.getElementById('existing-responses');
    const existingResponses = existingResponsesEl
        ? JSON.parse(existingResponsesEl.textContent ?? '{}') as Record<string, string>
        : {};
    prefillAnswers(slides, existingResponses);

    // If we have a session, get the server-computed visible path and resume point
    if (hasSession) {
        Promise.all([getVisiblePath(projectId), getResumeQuestion(projectId)])
            .then(([pathQuestionIds, resumeQuestionId]) => {
                // Rebuild visibleIndices from the server's computed path
                state.visibleIndices = [submitIndex];
                for (const qId of pathQuestionIds) {
                    const idx = questionIdToSlideIndex(slides, qId);
                    if (idx !== -1 && !state.visibleIndices.includes(idx)) {
                        state.visibleIndices.push(idx);
                    }
                }
                state.visibleIndices.sort((a, b) => a - b);

                // Navigate to the resume question
                if (resumeQuestionId !== null) {
                    const resumeIndex = questionIdToSlideIndex(slides, resumeQuestionId);
                    if (resumeIndex !== -1) {
                        if (!state.visibleIndices.includes(resumeIndex)) {
                            state.visibleIndices.push(resumeIndex);
                            state.visibleIndices.sort((a, b) => a - b);
                        }
                        state.currentIndex = resumeIndex;
                    }
                }

                updateQuestionCounters(slides, state.visibleIndices);
                syncTrackToVisibleSlides(track, slides, state.visibleIndices);
                animateTrackTo(track, state.currentIndex, state.visibleIndices);
                updateProgressBar(progressBar, state.visibleIndices.indexOf(state.currentIndex), state.visibleIndices.length);
                syncButtons(state);
            })
            .catch(() => { /* Resume unavailable — survey starts fresh with defaults */ });
    }

    updateQuestionCounters(slides, state.visibleIndices);
    syncTrackToVisibleSlides(track, slides, state.visibleIndices);
    animateTrackTo(track, state.currentIndex, state.visibleIndices);
    updateProgressBar(progressBar, 0, state.visibleIndices.length);
    syncButtons(state);

    btnNext?.addEventListener('click', () => navigateNext(state));
    btnPrev?.addEventListener('click', () => navigatePrev(state));

    if (container) initGestures(container, state);

    // Intercept form submit to use the API submit endpoint
    const form = document.getElementById('survey-form') as HTMLFormElement | null;
    form?.addEventListener('submit', async (e) => {
        e.preventDefault();

        const submitBtn = form.querySelector<HTMLButtonElement>('button[type="submit"]');
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.textContent = 'Bezig met verzenden...';
        }

        try {
            const {submitSurvey} = await import('./api');
            await submitSurvey(projectId);
            // Redirect to thank you page
            window.location.href = `/${projectId}/Survey/ThankYou`;
        } catch {
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.textContent = 'Antwoorden versturen';
            }
        }
    });
}
