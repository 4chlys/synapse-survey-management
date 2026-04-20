import type {SurveyState} from './types';
import {isSlideValid, showValidationError, clearValidationError} from './validation';
import {activateSlide, shakeSlide, updateProgressBar} from './animation';
import {
    getAnswerFromSlide,
    getQuestionId,
    questionIdToSlideIndex,
    getSubmitSlideIndex,
    updateQuestionCounters,
} from './branching';
import {getNextQuestion, getPreviousQuestion, saveResponse, startSession} from './api';

const TRANSITION_MS = 580;

function getVisiblePosition(state: SurveyState): number {
    return state.visibleIndices.indexOf(state.currentIndex);
}

function navigateTo(state: SurveyState, targetIndex: number): void {
    state.isAnimating = true;
    const prevIndex = state.currentIndex;
    state.currentIndex = targetIndex;
    updateQuestionCounters(state.slides, state.visibleIndices);
    activateSlide(state.slides, prevIndex, targetIndex);
    updateProgressBar(state.progressBar, getVisiblePosition(state), state.visibleIndices.length);
    state.onSlideChange?.(state);

    setTimeout(() => { state.isAnimating = false; }, TRANSITION_MS);
}

export async function navigateNext(state: SurveyState): Promise<void> {
    if (state.isAnimating) return;

    const current = state.slides[state.currentIndex];
    if (current.dataset.format === 'Submit') return;

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
        // Lazily create the session on first forward nav so anonymous loads cost nothing.
        if (!state.hasSession) {
            await startSession(state.projectId);
            state.hasSession = true;
        }

        if (answer) await saveResponse(state.projectId, questionId, answer);

        const nextQuestionId = await getNextQuestion(state.projectId, questionId);

        const submitIndex = getSubmitSlideIndex(state);
        let targetIndex: number;
        if (nextQuestionId === null || nextQuestionId === undefined) {
            targetIndex = submitIndex;
        } else {
            targetIndex = questionIdToSlideIndex(state.slides, nextQuestionId);
            if (targetIndex === -1) targetIndex = submitIndex;
        }

        // Keep slides up to current position, plus the new target and submit.
        // Drops stale slides that were on a previously projected branch.
        const currentVisiblePos = getVisiblePosition(state);
        state.visibleIndices = state.visibleIndices.filter(
            (idx, pos) => pos <= currentVisiblePos || idx === targetIndex || idx === submitIndex,
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
