import { callModerationAi, callSuggest, showRewritePopup } from './moderation';

const MIN_LENGTH = 3;

export function wireIdeationForm(
    form: HTMLFormElement,
    textareaName: string,
    projectId: number,
): void {
    console.log('[moderation] wireIdeationForm called', { textareaName, projectId });

    form.addEventListener('submit', async (e: SubmitEvent) => {
        e.preventDefault();
        console.log('[moderation] submit intercepted');

        const textarea    = form.querySelector<HTMLTextAreaElement>(`textarea[name="${textareaName}"]`);
        const statusInput = form.querySelector<HTMLInputElement>('input[name="moderationStatus"]');
        const submitBtn   = form.querySelector<HTMLButtonElement>('button[type="submit"]');

        if (!textarea || !statusInput) {
            console.error('[moderation] textarea or moderationStatus input not found — wired incorrectly');
            return; // Block, don't submit without moderation
        }

        const text = textarea.value.trim();
        console.log('[moderation] checking text:', text.substring(0, 40));

        if (text.length < MIN_LENGTH) {
            statusInput.value = 'Unflagged';
            await submitFormViaFetch(form);
            return;
        }

        const originalLabel = submitBtn?.textContent ?? 'Plaatsen';
        if (submitBtn) {
            submitBtn.disabled    = true;
            submitBtn.textContent = 'Controleren...';
        }

        try {
            console.log('[moderation] calling AI moderation for project', projectId);
            const result = await callModerationAi(text, projectId);
            console.log('[moderation] result:', result);

            if (result.isAcceptable) {
                statusInput.value = 'Unflagged';
                await submitFormViaFetch(form);
                return;
            }

            const suggestion = await callSuggest(text, projectId);

            const outcome = await showRewritePopup(
                text,
                suggestion,
                result.reason ?? 'De inhoud is niet toegestaan op dit platform.',
            );

            if (suggestion === null) {
                // Pure spam — no rewrite possible, stay on page
                return;
            }

            if (outcome.accepted) {
                textarea.value    = outcome.text;
                statusInput.value = 'Unflagged';
            } else {
                statusInput.value = 'Flagged';
            }

            await submitFormViaFetch(form);

        } catch (err) {
            console.error('[moderation] error during check:', err);
            // Block on unexpected errors
        } finally {
            if (submitBtn) {
                submitBtn.disabled    = false;
                submitBtn.textContent = originalLabel;
            }
        }
    });
}

async function submitFormViaFetch(form: HTMLFormElement): Promise<void> {
    const data = new FormData(form);
    const response = await fetch(form.action, {
        method:   'POST',
        body:     data,
        redirect: 'follow',
    });

    if (response.ok) {
        window.location.href = response.url;
    } else {
        console.error('[moderation] fetch POST failed:', response.status, await response.text());
    }
}