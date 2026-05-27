// Wires the participant-email compose flow:
//   1. Clicking a [data-email-trigger] (in _IdeasTable) opens the choice modal
//      that asks "send via platform vs your own mail program".
//   2. "Mail program" follows the existing mailto: behaviour.
//   3. "Via platform" opens the compose modal pre-filled with the participant
//      in the (disabled) BCC field, posts to ProjectController.SendParticipantEmail
//      on submit, and closes on success.

function readToken(form: HTMLFormElement): string {
    return form.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]')?.value ?? '';
}

function showError(box: HTMLElement, msg: string) {
    box.textContent = msg;
    box.classList.remove('hidden');
}

document.addEventListener('DOMContentLoaded', () => {
    const choiceModal  = document.getElementById('email-choice-modal') as HTMLDialogElement | null;
    const composeModal = document.getElementById('email-compose-modal') as HTMLDialogElement | null;
    if (!choiceModal || !composeModal) return;

    const composeForm    = document.getElementById('email-compose-form') as HTMLFormElement | null;
    const submitBtn      = document.getElementById('email-compose-submit') as HTMLButtonElement | null;
    const errorBox       = document.getElementById('email-compose-error') as HTMLElement | null;
    const bccField       = document.getElementById('email-compose-bcc') as HTMLInputElement | null;
    const participantHid = document.getElementById('email-compose-participant') as HTMLInputElement | null;
    const ideaHid        = document.getElementById('email-compose-ideaId') as HTMLInputElement | null;
    const languageHid    = document.getElementById('email-compose-language') as HTMLInputElement | null;
    const replyToInput   = document.getElementById('email-compose-replyto') as HTMLInputElement | null;
    const footerPreview  = document.getElementById('email-compose-footer-preview') as HTMLElement | null;
    const platformBtn    = document.getElementById('email-choice-platform') as HTMLButtonElement | null;
    const mailtoLink     = document.getElementById('email-choice-mailto') as HTMLAnchorElement | null;
    if (!composeForm || !submitBtn || !errorBox || !bccField || !participantHid || !platformBtn || !mailtoLink) return;

    let pendingParticipant = '';
    let pendingIdeaId: string | null = null;
    let pendingLanguage: string | null = null;

    // Live-update the (italic, non-editable) footer preview as the admin
    // edits the Reply-To input, so they see the wording recipients will see.
    if (replyToInput && footerPreview) {
        // Template captured from the initial render: "— Antwoorden aub aan {0}".
        // We replace the email substring (the only thing that varies) on input.
        const initial = footerPreview.textContent ?? '';
        const initialEmail = replyToInput.value;
        replyToInput.addEventListener('input', () => {
            footerPreview.textContent = initial.replace(initialEmail, replyToInput.value || initialEmail);
        });
    }

    // Open the choice modal from any clickable participant-email trigger.
    document.body.addEventListener('click', (e) => {
        const trigger = (e.target as HTMLElement).closest<HTMLElement>('[data-email-trigger]');
        if (!trigger) return;
        e.preventDefault();
        pendingParticipant = trigger.dataset.email ?? '';
        pendingIdeaId      = trigger.dataset.ideaId ?? null;
        pendingLanguage    = trigger.dataset.language ?? null;
        if (!pendingParticipant) return;
        mailtoLink.href = `mailto:${pendingParticipant}`;
        choiceModal.showModal();
    });

    platformBtn.addEventListener('click', () => {
        choiceModal.close();
        composeForm.reset();
        errorBox.classList.add('hidden');
        bccField.value          = pendingParticipant;
        participantHid.value    = pendingParticipant;
        if (ideaHid)     ideaHid.value     = pendingIdeaId ?? '';
        if (languageHid) languageHid.value = pendingLanguage ?? '';
        // Re-prefill the Reply-To input the form.reset() blanked.
        if (replyToInput) replyToInput.value = replyToInput.defaultValue;
        composeModal.showModal();
    });

    submitBtn.addEventListener('click', async () => {
        if (!composeForm.reportValidity()) return;
        errorBox.classList.add('hidden');
        submitBtn.disabled = true;
        try {
            const formData = new FormData(composeForm);
            const res = await fetch(composeForm.dataset.sendUrl ?? '', {
                method: 'POST',
                headers: { 'RequestVerificationToken': readToken(composeForm) },
                body: formData,
            });
            if (!res.ok) {
                showError(errorBox, `Send failed (${res.status})`);
                return;
            }
            composeModal.close();
        } catch (err) {
            showError(errorBox, (err as Error).message || 'Network error');
        } finally {
            submitBtn.disabled = false;
        }
    });
});
