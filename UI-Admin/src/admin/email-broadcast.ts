// Drives the "Send to all participants" flow:
//   1. [data-broadcast-trigger] opens the choice modal (platform vs own mail
//      program), mirroring the single-send choice.
//   2. "Own mail program" fetches every participant email and opens a mailto
//      with them all BCC'd (no per-language split — that's platform-only).
//   3. "Via platform" opens the per-language compose modal, which posts to
//      ProjectController.BroadcastParticipantEmail and fans out into N rows.

import { getAntiForgeryToken } from '../lib/utils';

export function initEmailBroadcast(): void {
    const trigger      = document.querySelector<HTMLElement>('[data-broadcast-trigger]');
    const choiceModal  = document.getElementById('email-broadcast-choice-modal') as HTMLDialogElement | null;
    const composeModal = document.getElementById('email-broadcast-modal') as HTMLDialogElement | null;
    if (!trigger || !choiceModal || !composeModal) return;

    const platformBtn  = document.getElementById('email-broadcast-choice-platform') as HTMLButtonElement | null;
    const mailtoBtn    = document.getElementById('email-broadcast-choice-mailto') as HTMLButtonElement | null;
    const choiceErr    = document.getElementById('email-broadcast-choice-error') as HTMLElement | null;
    const form         = document.getElementById('email-broadcast-form') as HTMLFormElement | null;
    const submitBtn    = document.getElementById('email-broadcast-submit') as HTMLButtonElement | null;
    const errorBox     = document.getElementById('email-broadcast-error') as HTMLElement | null;
    const okBox        = document.getElementById('email-broadcast-success') as HTMLElement | null;
    if (!platformBtn || !mailtoBtn || !choiceErr || !form || !submitBtn || !errorBox || !okBox) return;

    trigger.addEventListener('click', () => {
        choiceErr.classList.add('hidden');
        choiceModal.showModal();
    });

    platformBtn.addEventListener('click', () => {
        choiceModal.close();
        errorBox.classList.add('hidden');
        okBox.classList.add('hidden');
        composeModal.showModal();
    });

    // Own-mail-program branch: pull the flat participant list and hand it to
    // the OS mail client with everyone in BCC. Multilingual nuance is lost
    // here by design — the admin writes one message in their own client.
    mailtoBtn.addEventListener('click', async () => {
        choiceErr.classList.add('hidden');
        mailtoBtn.disabled = true;
        try {
            const res = await fetch(mailtoBtn.dataset.emailsUrl ?? '');
            if (!res.ok) { choiceErr.textContent = `Could not load participants (${res.status})`; choiceErr.classList.remove('hidden'); return; }
            const emails = await res.json() as string[];
            if (emails.length === 0) { choiceErr.textContent = 'No participants with an email address.'; choiceErr.classList.remove('hidden'); return; }
            window.location.href = `mailto:?bcc=${encodeURIComponent(emails.join(','))}`;
            choiceModal.close();
        } catch (err) {
            choiceErr.textContent = (err as Error).message || 'Network error';
            choiceErr.classList.remove('hidden');
        } finally {
            mailtoBtn.disabled = false;
        }
    });

    submitBtn.addEventListener('click', async () => {
        if (!form.reportValidity()) return;
        errorBox.classList.add('hidden');
        okBox.classList.add('hidden');
        submitBtn.disabled = true;
        try {
            const res = await fetch(form.dataset.sendUrl ?? '', {
                method: 'POST',
                headers: { 'RequestVerificationToken': getAntiForgeryToken() },
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
}
