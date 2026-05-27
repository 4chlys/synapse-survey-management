import {t} from '../lib/i18n';

const MAX_LENGTH = 500;

interface QuestionValidator {
    canValidate(format: string): boolean;
    isAnswered(slide: HTMLElement): boolean;
}

const validators: QuestionValidator[] = [
    {
        canValidate: (f) => f === 'Open',
        isAnswered: (slide) => {
            const ta = slide.querySelector<HTMLTextAreaElement>('textarea');
            return !!ta && ta.value.trim().length > 0;
        },
    },
    {
        canValidate: (f) => f === 'SingleChoice',
        isAnswered: (slide) => !!slide.querySelector<HTMLInputElement>('input[type="radio"]:checked'),
    },
    {
        canValidate: (f) => f === 'MultipleChoice',
        isAnswered: (slide) => !!slide.querySelector<HTMLInputElement>('input[type="checkbox"]:checked'),
    },
    {
        canValidate: (f) => f === 'Likert',
        isAnswered: () => true,
    },
];

export function isSlideAnswered(slide: HTMLElement): boolean {
    if (slide.dataset.mandatory !== 'true') return true;

    const format = slide.dataset.format ?? '';
    const validator = validators.find(v => v.canValidate(format));
    return validator?.isAnswered(slide) ?? true;
}

export function isSlideValid(slide: HTMLElement): { valid: boolean; message: string } {
    const format = slide.dataset.format;

    if (format === 'Open') {
        const ta = slide.querySelector<HTMLTextAreaElement>('textarea');
        if (ta && ta.value.length > MAX_LENGTH) {
            return {valid: false, message: t('surveyAnswerTooLong').replace('{max}', String(MAX_LENGTH))};
        }
    }

    if (!isSlideAnswered(slide)) {
        return {valid: false, message: t('surveyAnswerRequired')};
    }

    return {valid: true, message: ''};
}

export function showValidationError(slide: HTMLElement, message?: string): void {
    let el = slide.querySelector<HTMLElement>('.slide-error');
    if (!el) {
        el = document.createElement('p');
        el.className = 'slide-error text-red-500 text-xs mt-4 font-medium';
        slide.querySelector('.slide-answer-area')?.appendChild(el);
    }
    el.textContent = message ?? t('surveyAnswerRequired');
}

export function clearValidationError(slide: HTMLElement): void {
    slide.querySelector('.slide-error')?.remove();
}

export function initCharCounter(slide: HTMLElement): void {
    const ta = slide.querySelector<HTMLTextAreaElement>('textarea');
    const counter = slide.querySelector<HTMLElement>('.textarea-counter');
    if (!ta || !counter) return;

    const update = () => {
        const len = ta.value.length;
        counter.textContent = `${len} / ${MAX_LENGTH}`;
        counter.classList.toggle('text-error', len > MAX_LENGTH);
        counter.classList.toggle('text-base-content/40', len <= MAX_LENGTH);
    };

    ta.addEventListener('input', update);
    update();
}