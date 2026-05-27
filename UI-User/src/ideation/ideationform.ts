import {callModerationAi, callSuggest, showRewritePopup} from './moderation';
import {clearIdeaImageField, showImageRejectionPopup, uploadSelectedImage} from './ideaimage';
import {t} from '../lib/i18n';

const MIN_LENGTH = 2;
const MAX_LENGTH: Record<'idea' | 'comment', number> = { idea: 1000, comment: 500 };

export function wireIdeationForm(
    form: HTMLFormElement,
    textareaName: string,
    projectId: number,
    contentType: 'idea' | 'comment' = 'idea',
): void {
    form.addEventListener('submit', async (e: SubmitEvent) => {
        e.preventDefault();

        const textarea = form.querySelector<HTMLTextAreaElement>(`textarea[name="${textareaName}"]`);
        const tokenInput = form.querySelector<HTMLInputElement>('input[name="moderationToken"]');
        const submitBtn = form.querySelector<HTMLButtonElement>('button[type="submit"]');

        if (!textarea || !tokenInput) {
            return;
        }

        const text = textarea.value.trim();

        if (text.length > MAX_LENGTH[contentType]) {
            textarea.setCustomValidity(t('maxLengthExceeded').replace('{max}', String(MAX_LENGTH[contentType])));
            textarea.reportValidity();
            return;
        }
        textarea.setCustomValidity('');

        if (text.length < MIN_LENGTH) {
            tokenInput.value = '';
            await submitFormViaFetch(form);
            return;
        }

        const originalLabel = submitBtn?.textContent ?? t('submitFallback');
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.textContent = t('uploading');
        }

        try {
            // Upload image first (ideas only; comments have no image)
            if (contentType === 'idea') {
                const imageResult = await uploadSelectedImage(form, projectId);
                if (imageResult.status === 'rejected') {
                    const choice = await showImageRejectionPopup(imageResult.reason);
                    if (choice === 'cancel') {
                        clearIdeaImageField(form);
                        return;
                    }
                    // submitFlagged: hidden inputs already carry the Flagged token — continue to submit
                } else if (imageResult.status === 'error') {
                    return; // inline error already rendered
                }
            }

            if (submitBtn) submitBtn.textContent = t('checking');

            const result = await callModerationAi(text, projectId, contentType);

            if (result.isAcceptable) {
                tokenInput.value = result.moderationToken ?? '';
                await submitFormViaFetch(form);
                return;
            }

            const suggestResult = await callSuggest(text, projectId);

            const outcome = await showRewritePopup(
                text,
                suggestResult.suggestion,
                result.reason ?? t('contentNotAllowed'),
            );

            if (outcome.action === 'accepted') {
                textarea.value = outcome.text;
                tokenInput.value = suggestResult.moderationToken ?? '';
                await submitFormViaFetch(form);
            } else if (outcome.action === 'submitFlagged') {
                tokenInput.value = result.moderationToken ?? '';
                await submitFormViaFetch(form);
            } else {
                form.dispatchEvent(new CustomEvent('idea:needs-edit', { bubbles: true }));
                return;
            }

        } catch {
            // Moderation check failed — allow submission to proceed
        } finally {
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.textContent = originalLabel;
            }
        }
    });
}

export async function submitIdeaDirectly(
    form: HTMLFormElement,
    descriptionEl: HTMLTextAreaElement,
    moderationEl: HTMLInputElement | null,
    draftText: string,
    projectId: number,
    onModerationBlocked: () => void,
): Promise<void> {
    descriptionEl.value = draftText;
    if (moderationEl) moderationEl.value = '';

    try {
        const result = await callModerationAi(draftText, projectId, 'idea');

        if (result.isAcceptable) {
            if (moderationEl) moderationEl.value = result.moderationToken ?? '';
            await submitFormViaFetch(form);
            return;
        }

        const suggestResult = await callSuggest(draftText, projectId);
        const outcome = await showRewritePopup(
            draftText,
            suggestResult.suggestion,
            result.reason ?? t('contentNotAllowed'),
        );

        if (outcome.action === 'accepted') {
            descriptionEl.value = outcome.text;
            if (moderationEl) moderationEl.value = suggestResult.moderationToken ?? '';
            await submitFormViaFetch(form);
        } else if (outcome.action === 'submitFlagged') {
            if (moderationEl) moderationEl.value = result.moderationToken ?? '';
            await submitFormViaFetch(form);
        } else {
            onModerationBlocked();
        }
    } catch {
        onModerationBlocked();
    }
}

async function submitFormViaFetch(form: HTMLFormElement): Promise<void> {
    const data = new FormData(form);
    const response = await fetch(form.action, {
        method: 'POST',
        body: data,
        redirect: 'follow',
    });

    if (response.ok) {
        window.location.href = response.url;
    }
}