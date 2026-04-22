document.addEventListener('DOMContentLoaded', () => {
    const saveBtn = document.getElementById('save-btn') as HTMLButtonElement | null;
    if (!saveBtn) return;

    const categoriesError = document.getElementById('categories-error') as HTMLElement | null;

    saveBtn.addEventListener('click', () => {
        const count = parseInt(saveBtn.dataset.projectCount ?? '0', 10);

        if (count < 1) {
            categoriesError?.classList.remove('hidden');
            return;
        }

        categoriesError?.classList.add('hidden');
        window.location.href = saveBtn.dataset.href ?? '/';
    });
});
