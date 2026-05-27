import type {SurveyState} from './types';
import {navigateNext, navigatePrev} from './navigation';
import {initGestures} from './gestures';
import {updateProgressBar} from './animation';
import {initCharCounter} from './validation';
import {getSubmitSlideIndex, prefillAnswers, questionIdToSlideIndex, updateQuestionCounters} from './branching';
import {getResumeQuestion, getVisiblePath, submitSurvey} from './api';
import {t} from '../lib/i18n';

function syncButtons(state: SurveyState): void {
    const allPrev = document.querySelectorAll<HTMLButtonElement>('.btn-prev');
    const allNext = document.querySelectorAll<HTMLButtonElement>('.btn-next');

    const isSubmitSlide = state.currentIndex === getSubmitSlideIndex(state);

    // Disable back only before a session exists — once the user has answered at
    // least one question navigatePrev handles the "nothing to go back to" case itself.
    allPrev.forEach(btn => btn.disabled = !state.hasSession);
    allNext.forEach(btn => btn.style.visibility = isSubmitSlide ? 'hidden' : 'visible');
}

export function initSurvey(): void {
    const container = document.getElementById('survey-container') as HTMLElement | null;
    if (!container) return;

    const slides = Array.from(container.querySelectorAll<HTMLElement>('.survey-slide'));
    if (!slides.length) return;

    const progressBar = document.getElementById('survey-progress-bar');
    const projectId = parseInt(container.dataset.projectId!, 10);
    const hasSession = container.dataset.hasSession === 'true';

    // Start with all slides "visible" so the counter shows the full total before
    // branching narrows the path. navigateNext trims stale indices as the user proceeds.
    const state: SurveyState = {
        currentIndex: 0,
        slides,
        isAnimating: false,
        progressBar,
        onSlideChange: syncButtons,
        visibleIndices: slides.map((_, i) => i),
        projectId,
        hasSession,
        maxProgressPct: 0,
        lastForwardTargets: new Map(),
    };

    slides.forEach(slide => initCharCounter(slide));

    const existingResponsesEl = document.getElementById('existing-responses');
    const existingResponses = existingResponsesEl
        ? JSON.parse(existingResponsesEl.textContent ?? '{}') as Record<string, string>
        : {};
    prefillAnswers(slides, existingResponses);

    slides[0].classList.add('is-active');
    slides.forEach((slide, idx) => { if (idx !== 0) slide.setAttribute('inert', ''); });
    updateQuestionCounters(slides, state.visibleIndices);
    state.maxProgressPct = updateProgressBar(progressBar, 0, state.visibleIndices.length, 0);
    syncButtons(state);

    if (hasSession) {
        Promise.all([getVisiblePath(projectId), getResumeQuestion(projectId)])
            .then(([pathQuestionIds, resumeQuestionId]) => {
                const submitIndex = getSubmitSlideIndex(state);
                state.visibleIndices = [submitIndex];
                for (const qId of pathQuestionIds) {
                    const idx = questionIdToSlideIndex(slides, qId);
                    if (idx !== -1 && !state.visibleIndices.includes(idx)) {
                        state.visibleIndices.push(idx);
                    }
                }
                state.visibleIndices.sort((a, b) => a - b);

                let resumeIndex = 0;
                if (resumeQuestionId !== null && resumeQuestionId !== undefined) {
                    const idx = questionIdToSlideIndex(slides, resumeQuestionId);
                    if (idx !== -1) resumeIndex = idx;
                }

                if (!state.visibleIndices.includes(resumeIndex)) {
                    state.visibleIndices.push(resumeIndex);
                    state.visibleIndices.sort((a, b) => a - b);
                }

                if (resumeIndex > 0) {
                    slides[0].classList.remove('is-active');
                    slides[0].setAttribute('inert', '');
                    for (const idx of state.visibleIndices) {
                        if (idx < resumeIndex) slides[idx].classList.add('is-above');
                    }
                    slides[resumeIndex].classList.add('is-active');
                    slides[resumeIndex].removeAttribute('inert');
                    state.currentIndex = resumeIndex;
                }

                updateQuestionCounters(slides, state.visibleIndices);
                state.maxProgressPct = updateProgressBar(progressBar, state.visibleIndices.indexOf(state.currentIndex), state.visibleIndices.length, 0);
                syncButtons(state);
            })
            .catch(() => { /* Resume unavailable — survey starts fresh with defaults */ });
    }

    container.querySelectorAll<HTMLButtonElement>('.btn-next')
        .forEach(btn => btn.addEventListener('click', () => navigateNext(state)));
    container.querySelectorAll<HTMLButtonElement>('.btn-prev')
        .forEach(btn => btn.addEventListener('click', () => navigatePrev(state)));

    initGestures(container, state);

    // Responses are already saved progressively, so the final submit is just
    // "finalize this session" — go through the API and redirect to ThankYou.
    const form = document.getElementById('survey-form') as HTMLFormElement | null;
    form?.addEventListener('submit', async (e) => {
        e.preventDefault();

        const submitBtn = form.querySelector<HTMLButtonElement>('button[type="submit"]');
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.textContent = t('surveySubmitting');
        }

        try {
            await submitSurvey(projectId);
            const popup = document.getElementById('thank-you-popup');
            if (popup) {
                popup.classList.remove('hidden');
                popup.classList.add('flex');
            }
        } catch {
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.textContent = t('surveySubmitAnswers');
            }
        }
    });
}
