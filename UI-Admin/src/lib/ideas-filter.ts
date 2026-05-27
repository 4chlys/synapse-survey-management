let _form: HTMLFormElement | null = null;
let _results: HTMLElement | null = null;
let _debounceTimer: ReturnType<typeof setTimeout>;

async function loadFiltered(url: string): Promise<void> {
    try {
        const res = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (res.ok) {
            _results!.innerHTML = await res.text();
            return;
        }
    } catch {}
    window.location.href = url;
}

function buildUrl(page = 1): string {
    const url = new URL(_form!.action);
    new FormData(_form!).forEach((val, key) => {
        if (val !== '') url.searchParams.set(key, val.toString());
        else url.searchParams.delete(key);
    });
    url.searchParams.set('page', String(page));
    return url.toString();
}

export function initIdeasFilter(): void {
    _form = document.querySelector<HTMLFormElement>('[data-ideas-filter]');
    _results = document.getElementById('ideas-results');
    if (!_form || !_results) return;

    _form.addEventListener('change', (e) => {
        if ((e.target as HTMLElement).tagName === 'SELECT') void loadFiltered(buildUrl());
    });

    _form.addEventListener('submit', (e) => {
        e.preventDefault();
        void loadFiltered(buildUrl());
    });

    _form.querySelector<HTMLInputElement>('input[name="search"]')?.addEventListener('input', () => {
        clearTimeout(_debounceTimer);
        _debounceTimer = setTimeout(() => void loadFiltered(buildUrl()), 300);
    });

    _form.querySelector<HTMLAnchorElement>('[data-clear-filter]')?.addEventListener('click', (e) => {
        e.preventDefault();
        const search = _form!.querySelector<HTMLInputElement>('input[name="search"]');
        const category = _form!.querySelector<HTMLSelectElement>('select[name="categoryId"]');
        const status = _form!.querySelector<HTMLSelectElement>('select[name="status"]');
        if (search) search.value = '';
        if (category) category.value = '';
        if (status) status.value = '';
        void loadFiltered(buildUrl());
    });

    _results.addEventListener('click', (e) => {
        const link = (e.target as HTMLElement).closest<HTMLAnchorElement>('a.join-item');
        if (!link) return;
        e.preventDefault();
        void loadFiltered(link.href);
    });
}
