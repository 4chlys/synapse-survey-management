document.addEventListener('DOMContentLoaded', () => {
    const modal = document.getElementById('delete-modal') as HTMLElement;
    const projectNameEl = document.getElementById('delete-project-name') as HTMLElement;
    const projectIdInput = document.getElementById('delete-project-id') as HTMLInputElement;
    const cancelBtn = document.getElementById('delete-cancel') as HTMLButtonElement;

    if (!modal) return;

    document.querySelectorAll<HTMLButtonElement>('.project-menu-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            const id = btn.dataset.projectId;
            document.querySelectorAll<HTMLElement>('.project-menu').forEach(m => {
                if (m.dataset.projectId !== id) m.classList.add('hidden');
            });
            const menu = document.querySelector<HTMLElement>(`.project-menu[data-project-id="${id}"]`);
            menu?.classList.toggle('hidden');
        });
    });

    document.addEventListener('click', () => {
        document.querySelectorAll<HTMLElement>('.project-menu').forEach(m => m.classList.add('hidden'));
    });

    document.querySelectorAll<HTMLButtonElement>('.delete-project-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            projectNameEl.textContent = btn.dataset.projectName ?? '';
            projectIdInput.value = btn.dataset.projectId ?? '';
            document.querySelectorAll<HTMLElement>('.project-menu').forEach(m => m.classList.add('hidden'));
            modal.classList.remove('hidden');
        });
    });

    cancelBtn.addEventListener('click', () => modal.classList.add('hidden'));
    modal.addEventListener('click', (e) => {
        if (e.target === modal) modal.classList.add('hidden');
    });

    // Double-submit guard: a quick double-click on Delete fires two POSTs.
    // The first commits, the second 500s on a now-missing project. Covers both
    // the modal #delete-form and the inline per-card forms (action=…/DeleteProject).
    document.querySelectorAll<HTMLFormElement>('form[action*="DeleteProject"]').forEach(form => {
        form.addEventListener('submit', (e) => {
            const submitBtn = form.querySelector<HTMLButtonElement>('button[type="submit"]');
            if (submitBtn?.disabled) {
                e.preventDefault();
                return;
            }
            if (submitBtn) submitBtn.disabled = true;
        });
    });
});
