import type {SurveyState} from './types';
import {isSlideValid, showValidationError, clearValidationError} from './validation';
import {animateTrackTo, shakeSlide, syncTrackToVisibleSlides, updateProgressBar} from './animation';
import {
    getAnswerFromSlide,
    getQuestionId,
    questionIdToSlideIndex,
    getSubmitSlideIndex,
    updateQuestionCounters
} from './branching';
import {saveResponse, getNextQuestion, getPreviousQuestion, startSession} from './api';

const TRANSITION_MS = 580;

function getVisiblePosition(state: SurveyState): number {
    return state.visibleIndices.indexOf(state.currentIndex);
}

function navigateTo(state: SurveyState, slideIndex: number): void {
    state.isAnimating = true;
    state.currentIndex = slideIndex;
    updateQuestionCounters(state.slides, state.visibleIndices);
    syncTrackToVisibleSlides(state.track, state.slides, state.visibleIndices);
    animateTrackTo(state.track, state.currentIndex, state.visibleIndices);

    const visiblePos = getVisiblePosition(state);
    updateProgressBar(state.progressBar, visiblePos, state.visibleIndices.length);
    state.onSlideChange?.(state);

    setTimeout(() => {
        state.isAnimating = false;
    }, TRANSITION_MS);
}

export async function navigateNext(state: SurveyState): Promise<void> {
    if (state.isAnimating) return;

    const current = state.slides[state.currentIndex];
    const format = current.dataset.format;

    if (format === 'Submit') return;

    const {valid, message} = isSlideValid(current);
    if (!valid) {
        showValidationError(current, message);
        shakeSlide(current);
        return;
    }
    clearValidationError(current);

    const questionId = getQuestionId(current);
    const answer = getAnswerFromSlide(current);

    try {
        // Ensure session exists before first save
        if (!state.hasSession) {
            await startSession(state.projectId);
            state.hasSession = true;
        }

        // Save the current answer
        if (answer) {
            await saveResponse(state.projectId, questionId, answer);
        }

        // Ask server for next question
        const nextQuestionId = await getNextQuestion(state.projectId, questionId);

        let targetIndex: number;
        if (nextQuestionId === null || nextQuestionId === undefined) {
            targetIndex = getSubmitSlideIndex(state);
        } else {
            targetIndex = questionIdToSlideIndex(state.slides, nextQuestionId);
            if (targetIndex === -1) {
                targetIndex = getSubmitSlideIndex(state);
            }
        }

        // Trim future slides that are no longer on this branch path,
        // but keep everything up to and including the current question.
        const currentVisiblePos = state.visibleIndices.indexOf(state.currentIndex);
        const submitIndex = getSubmitSlideIndex(state);
        state.visibleIndices = state.visibleIndices.filter(
            (idx, pos) => pos <= currentVisiblePos || idx === targetIndex || idx === submitIndex
        );

        if (!state.visibleIndices.includes(targetIndex)) {
            state.visibleIndices.push(targetIndex);
            state.visibleIndices.sort((a, b) => a - b);
        }
        navigateTo(state, targetIndex);
    } catch (err) {
        console.error('Survey navigation error:', err);
    }
}

export async function navigatePrev(state: SurveyState): Promise<void> {
    if (state.isAnimating) return;

    const current = state.slides[state.currentIndex];
    clearValidationError(current);

    try {
        if (!state.hasSession) return;

        let targetIndex: number;

        if (current.dataset.format === 'Submit') {
            const visiblePos = getVisiblePosition(state);
            if (visiblePos <= 0) return;
            targetIndex = state.visibleIndices[visiblePos - 1];
        } else {
            const questionId = getQuestionId(current);
            const prevQuestionId = await getPreviousQuestion(state.projectId, questionId);

            if (prevQuestionId === null || prevQuestionId === undefined) return;

            targetIndex = questionIdToSlideIndex(state.slides, prevQuestionId);
            if (targetIndex === -1) return;
        }

        navigateTo(state, targetIndex);
    } catch (err) {
        console.error('Survey back navigation error:', err);
    }
}
