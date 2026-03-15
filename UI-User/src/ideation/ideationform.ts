import {callModerationAi, callSuggest, showRewritePopup} from './moderation';

const MIN_LENGTH = 3;

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

        if (text.length < MIN_LENGTH) {
            tokenInput.value = '';
            await submitFormViaFetch(form);
            return;
        }

        const originalLabel = submitBtn?.textContent ?? 'Plaatsen';
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.textContent = 'Controleren...';
        }

        try {
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
                result.reason ?? 'De inhoud is niet toegestaan op dit platform.',
            );

            if (outcome.action === 'accepted') {
                textarea.value = outcome.text;
                tokenInput.value = suggestResult.moderationToken ?? '';
                await submitFormViaFetch(form);
            } else if (outcome.action === 'submitFlagged') {
                tokenInput.value = result.moderationToken ?? '';
                await submitFormViaFetch(form);
            } else {
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