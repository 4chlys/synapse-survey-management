// Drives the "Send to all participants" broadcast modal. The form holds one
// Subject+Body pair per language (subjects[en], bodies[en], ...). Submission
// posts to ProjectController.BroadcastParticipantEmail, which fans the data
// out into N EmailJob rows on the server (one per language × BCC chunk).

function readToken(form: HTMLFormElement): string {
    return form.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]')?.value ?? '';
}

document.addEventListener('DOMContentLoaded', () => {
    const trigger = document.querySelector<HTMLElement>('[data-broadcast-trigger]');
    const modal   = document.getElementById('email-broadcast-modal') as HTMLDialogElement | null;
    if (!trigger || !modal) return;

    const form      = document.getElementById('email-broadcast-form') as HTMLFormElement | null;
    const submitBtn = document.getElementById('email-broadcast-submit') as HTMLButtonElement | null;
    const errorBox  = document.getElementById('email-broadcast-error') as HTMLElement | null;
    const okBox     = document.getElementById('email-broadcast-success') as HTMLElement | null;
    if (!form || !submitBtn || !errorBox || !okBox) return;

    trigger.addEventListener('click', () => {
        errorBox.classList.add('hidden');
        okBox.classList.add('hidden');
        modal.showModal();
    });

    submitBtn.addEventListener('click', async () => {
        if (!form.reportValidity()) return;
        errorBox.classList.add('hidden');
        okBox.classList.add('hidden');
        submitBtn.disabled = true;
        try {
            const res = await fetch(form.dataset.sendUrl ?? '', {
                method: 'POST',
                headers: { 'RequestVerificationToken': readToken(form) },
                body: new FormData(form),
            });
            if (!res.ok) {
                const txt = await res.text();
                errorBox.textContent = txt || `Send failed (${res.status})`;
                errorBox.classList.remove('hidden');
                return;
            }
            const body = await res.json() as { jobsEnqueued: number; recipientsReached: number };
            okBox.textContent = `Enqueued ${body.jobsEnqueued} message(s) for ${body.recipientsReached} participant(s).`;
            okBox.classList.remove('hidden');
        } catch (err) {
            errorBox.textContent = (err as Error).message || 'Network error';
            errorBox.classList.remove('hidden');
        } finally {
            submitBtn.disabled = false;
        }
    });
});
