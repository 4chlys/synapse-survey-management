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

        clearSubmitError(form);

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

            const suggestResult = await callSuggest(
                text,
                projectId,
                form.querySelector<HTMLSelectElement>('select[name="languageCode"]')?.value,
            );

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
    clearSubmitError(form);

    try {
        const result = await callModerationAi(draftText, projectId, 'idea');

        if (result.isAcceptable) {
            if (moderationEl) moderationEl.value = result.moderationToken ?? '';
            // Reveal the form before submitting so a failure's inline error is visible.
            if (!await submitFormViaFetch(form)) onModerationBlocked();
            return;
        }

        const suggestResult = await callSuggest(
            draftText,
            projectId,
            form.querySelector<HTMLSelectElement>('select[name="languageCode"]')?.value,
        );
        const outcome = await showRewritePopup(
            draftText,
            suggestResult.suggestion,
            result.reason ?? t('contentNotAllowed'),
        );

        if (outcome.action === 'accepted') {
            descriptionEl.value = outcome.text;
            if (moderationEl) moderationEl.value = suggestResult.moderationToken ?? '';
            if (!await submitFormViaFetch(form)) onModerationBlocked();
        } else if (outcome.action === 'submitFlagged') {
            if (moderationEl) moderationEl.value = result.moderationToken ?? '';
            if (!await submitFormViaFetch(form)) onModerationBlocked();
        } else {
            onModerationBlocked();
        }
    } catch {
        onModerationBlocked();
    }
}

async function submitFormViaFetch(form: HTMLFormElement): Promise<boolean> {
    const data = new FormData(form);

    let response: Response;
    try {
        response = await fetch(form.action, {
            method: 'POST',
            body: data,
            redirect: 'follow',
        });
    } catch {
        showSubmitError(form);
        return false;
    }

    // A successful post is answered with a server-side redirect to the idea hub,
    // so `response.redirected` is the reliable success signal. A non-redirected
    // 200 means the server returned the moderation popup partial (idea NOT saved),
    // and any 4xx/5xx is an outright failure (e.g. an expired image moderation
    // token). In all of those cases navigating to `response.url` would land on the
    // POST-only endpoint and silently dead-end — so surface the error instead.
    if (response.redirected && response.ok) {
        window.location.href = response.url;
        return true;
    }

    showSubmitError(form);
    return false;
}

/**
 * Renders a visible "could not post" error inside the form and clears any
 * committed image URL/token so that a retry re-uploads the image with a fresh
 * (unexpired) moderation token instead of replaying the stale one.
 */
function showSubmitError(form: HTMLFormElement): void {
    const errorEl = form.querySelector<HTMLElement>('[data-submit-error]');
    if (errorEl) {
        errorEl.textContent = t('ideaSubmitFailed');
        errorEl.classList.remove('hidden');
    }

    const urlInput = form.querySelector<HTMLInputElement>('input[name="imageUrl"]');
    const imageTokenInput = form.querySelector<HTMLInputElement>('input[name="imageModerationToken"]');
    if (urlInput) urlInput.value = '';
    if (imageTokenInput) imageTokenInput.value = '';
}

function clearSubmitError(form: HTMLFormElement): void {
    const errorEl = form.querySelector<HTMLElement>('[data-submit-error]');
    if (errorEl) {
        errorEl.textContent = '';
        errorEl.classList.add('hidden');
    }
}