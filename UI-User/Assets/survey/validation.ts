const MAX_LENGTH = 500;

export function isSlideAnswered(slide: HTMLElement): boolean {
  if (slide.dataset.mandatory !== 'true') return true;

  const format = slide.dataset.format;

  switch (format) {
    case 'Open': {
      const ta = slide.querySelector<HTMLTextAreaElement>('textarea');
      return !!ta && ta.value.trim().length > 0;
    }
    case 'SingleChoice':
      return !!slide.querySelector<HTMLInputElement>('input[type="radio"]:checked');
    case 'MultipleChoice':
      return !!slide.querySelector<HTMLInputElement>('input[type="checkbox"]:checked');
    case 'Likert':
      return true;
    default:
      return true;
  }
}

export function isSlideValid(slide: HTMLElement): { valid: boolean; message: string } {
  const format = slide.dataset.format;

  if (format === 'Open') {
    const ta = slide.querySelector<HTMLTextAreaElement>('textarea');
    if (ta && ta.value.length > MAX_LENGTH) {
      return { valid: false, message: `Je antwoord mag maximaal ${MAX_LENGTH} tekens bevatten.` };
    }
  }

  if (!isSlideAnswered(slide)) {
    return { valid: false, message: 'Geef eerst een antwoord voor je verdergaat.' };
  }

  return { valid: true, message: '' };
}

export function showValidationError(slide: HTMLElement, message?: string): void {
  let el = slide.querySelector<HTMLElement>('.slide-error');
  if (!el) {
    el = document.createElement('p');
    el.className = 'slide-error text-red-500 text-xs mt-4 font-medium';
    slide.querySelector('.slide-answer-area')?.appendChild(el);
  }
  el.textContent = message ?? 'Geef eerst een antwoord voor je verdergaat.';
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
    counter.classList.toggle('text-red-500', len > MAX_LENGTH);
    counter.classList.toggle('text-gray-400', len <= MAX_LENGTH);
  };

  ta.addEventListener('input', update);
  update();
}
