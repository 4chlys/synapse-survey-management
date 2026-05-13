import type {SurveyState} from './types';

export function getQuestionId(slide: HTMLElement): number {
    return parseInt(slide.dataset.questionId ?? '0', 10);
}

export function questionIdToSlideIndex(slides: HTMLElement[], questionId: number): number {
    return slides.findIndex(s => getQuestionId(s) === questionId);
}

export function getAnswerFromSlide(slide: HTMLElement): string {
    const format = slide.dataset.format ?? '';

    if (format === 'SingleChoice') {
        const checked = slide.querySelector<HTMLInputElement>('input[type="radio"]:checked');
        return checked?.value ?? '';
    }

    if (format === 'MultipleChoice') {
        const checked = slide.querySelectorAll<HTMLInputElement>('input[type="checkbox"]:checked');
        return Array.from(checked).map(c => c.value).join(', ');
    }

    if (format === 'Likert') {
        const range = slide.querySelector<HTMLInputElement>('input[type="range"]');
        return range?.value ?? '';
    }

    if (format === 'Open') {
        const ta = slide.querySelector<HTMLTextAreaElement>('textarea');
        return ta?.value?.trim() ?? '';
    }

    return '';
}

export function updateQuestionCounters(slides: HTMLElement[], visibleIndices: number[]): void {
    const visibleQuestionIndices = visibleIndices.filter(i => slides[i]?.dataset.format !== 'Submit');

    visibleQuestionIndices.forEach((slideIndex, position) => {
        const slide = slides[slideIndex];
        const numEl = slide.querySelector('.q-number');
        if (numEl) numEl.textContent = String(position + 1);
    });
}

export function prefillAnswers(slides: HTMLElement[], responses: Record<string, string>): void {
    for (const slide of slides) {
        const questionId = getQuestionId(slide);
        const value = responses[String(questionId)];
        if (!value) continue;

        const format = slide.dataset.format ?? '';

        if (format === 'SingleChoice') {
            const radio = slide.querySelector<HTMLInputElement>(`input[type="radio"][value="${CSS.escape(value)}"]`);
            if (radio) radio.checked = true;
        } else if (format === 'MultipleChoice') {
            const values = value.split(', ');
            for (const v of values) {
                const cb = slide.querySelector<HTMLInputElement>(`input[type="checkbox"][value="${CSS.escape(v)}"]`);
                if (cb) cb.checked = true;
            }
        } else if (format === 'Likert') {
            const range = slide.querySelector<HTMLInputElement>('input[type="range"]');
            if (range) range.value = value;
        } else if (format === 'Open') {
            const ta = slide.querySelector<HTMLTextAreaElement>('textarea');
            if (ta) ta.value = value;
        }
    }
}

export function getSubmitSlideIndex(state: SurveyState): number {
    return state.slides.length - 1;
}
