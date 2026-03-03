export function isSlideAnswered(slide: HTMLElement): boolean {
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
      return true; // range always has a value
    default:
      return true; // submit slide or unknown
  }
}

export function showValidationError(slide: HTMLElement): void {
  if (slide.querySelector('.slide-error')) return;

  const msg = document.createElement('p');
  msg.className = 'slide-error text-red-500 text-xs mt-4 font-medium';
  msg.textContent = 'Geef eerst een antwoord voor je verdergaat.';
  slide.querySelector('.slide-answer-area')?.appendChild(msg);
}

export function clearValidationError(slide: HTMLElement): void {
  slide.querySelector('.slide-error')?.remove();
}
