document.addEventListener('DOMContentLoaded', () => {
    const saveBtn = document.getElementById('save-btn') as HTMLButtonElement;
    const nextBtn = document.getElementById('next-btn') as HTMLButtonElement;

    const categoriesError = document.getElementById('categories-error') as HTMLElement | null;

    function validateAndNavigate(btn: HTMLButtonElement) {
        const count = parseInt(btn.dataset.projectCount ?? '0', 10);

        if (count < 1) {
            categoriesError?.classList.remove('hidden');
            return;
        }

        categoriesError?.classList.add('hidden');
        window.location.href = btn.dataset.href ?? '/';
    }

    saveBtn.addEventListener('click', () => validateAndNavigate(saveBtn));
    nextBtn.addEventListener('click', () => validateAndNavigate(nextBtn));
});
