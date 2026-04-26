function normalizeToShortForm(value: string): string {
    return value
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '')
        .slice(0, 30)
        .replace(/-+$/, '');
}

document.addEventListener('DOMContentLoaded', () => {
    const nameInput = document.getElementById('project-name-input') as HTMLInputElement | null;
    const shortFormInput = document.getElementById('project-shortform-input') as HTMLInputElement | null;

    if (!nameInput || !shortFormInput) return;

    if (shortFormInput.hasAttribute('readonly')) return;

    let dirty = shortFormInput.value.length > 0;

    shortFormInput.addEventListener('input', () => {
        dirty = true;
    });

    nameInput.addEventListener('input', () => {
        if (dirty) return;
        const normalized = normalizeToShortForm(nameInput.value);
        shortFormInput.value = normalized;
    });
});
