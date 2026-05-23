function initIdeasFilter(form: HTMLFormElement, results: HTMLElement) {
    let debounceTimer: ReturnType<typeof setTimeout>;

    async function loadFiltered(url: string) {
        try {
            const res = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            if (res.ok) {
                results.innerHTML = await res.text();
                return;
            }
        } catch {}
        window.location.href = url;
    }

    function buildUrl(page = 1): string {
        const url = new URL(form.action);
        new FormData(form).forEach((val, key) => {
            if (val !== '') url.searchParams.set(key, val.toString());
            else url.searchParams.delete(key);
        });
        url.searchParams.set('page', String(page));
        return url.toString();
    }

    form.querySelectorAll<HTMLSelectElement>('select[onchange]').forEach(s =>
        s.removeAttribute('onchange')
    );

    form.addEventListener('change', (e) => {
        if ((e.target as HTMLElement).tagName === 'SELECT') loadFiltered(buildUrl());
    });

    form.addEventListener('submit', (e) => {
        e.preventDefault();
        loadFiltered(buildUrl());
    });

    form.querySelector<HTMLInputElement>('input[name="search"]')?.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => loadFiltered(buildUrl()), 300);
    });

    form.querySelector<HTMLAnchorElement>('[data-clear-filter]')?.addEventListener('click', (e) => {
        e.preventDefault();
        const search = form.querySelector<HTMLInputElement>('input[name="search"]');
        const category = form.querySelector<HTMLSelectElement>('select[name="categoryId"]');
        const status = form.querySelector<HTMLSelectElement>('select[name="status"]');
        if (search) search.value = '';
        if (category) category.value = '';
        if (status) status.value = '';
        loadFiltered(buildUrl());
    });

    results.addEventListener('click', (e) => {
        const link = (e.target as HTMLElement).closest<HTMLAnchorElement>('a.join-item');
        if (!link) return;
        e.preventDefault();
        loadFiltered(link.href);
    });
}

document.addEventListener('DOMContentLoaded', () => {
    const form = document.querySelector<HTMLFormElement>('[data-ideas-filter]');
    const results = document.getElementById('ideas-results');
    if (!form || !results) return;
    initIdeasFilter(form, results);
});
