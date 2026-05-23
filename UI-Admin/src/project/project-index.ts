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
});
