import {callModerationAi, callSuggest, showRewritePopup} from './moderation';

const MIN_LENGTH = 3;

export function wireIdeationForm(
    form: HTMLFormElement,
    textareaName: string,
    projectId: number,
): void {
    console.log('[moderation] wireIdeationForm called', {textareaName, projectId});

    form.addEventListener('submit', async (e: SubmitEvent) => {
        e.preventDefault();
        console.log('[moderation] submit intercepted');

        const textarea = form.querySelector<HTMLTextAreaElement>(`textarea[name="${textareaName}"]`);
        const tokenInput = form.querySelector<HTMLInputElement>('input[name="moderationToken"]');
        const submitBtn = form.querySelector<HTMLButtonElement>('button[type="submit"]');

        if (!textarea || !tokenInput) {
            console.error('[moderation] textarea or moderationToken input not found — wired incorrectly');
            return;
        }

        const text = textarea.value.trim();
        console.log('[moderation] checking text:', text.substring(0, 40));

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
            console.log('[moderation] calling AI moderation for project', projectId);
            const result = await callModerationAi(text, projectId);
            console.log('[moderation] result:', result);

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

        } catch (err) {
            console.error('[moderation] error during check:', err);
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
    } else {
        console.error('[moderation] fetch POST failed:', response.status, await response.text());
    }
}