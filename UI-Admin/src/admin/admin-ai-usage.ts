interface ProjectCostAggregateDto {
    projectId: number;
    projectName: string;
    platformId: number;
    platformName: string;
    inputTokens: number;
    outputTokens: number;
    costEur: number;
}

const eur = new Intl.NumberFormat('nl-BE', {
    style: 'currency',
    currency: 'EUR',
    minimumFractionDigits: 4,
    maximumFractionDigits: 4,
});

function escapeHtml(value: string): string {
    return value
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function renderSystemWide(tbody: HTMLTableSectionElement, projects: ProjectCostAggregateDto[], emptyLabel: string): void {
    if (projects.length === 0) {
        tbody.innerHTML = `<tr><td colspan="5" class="text-center text-base-content/40 text-sm py-4">${escapeHtml(emptyLabel)}</td></tr>`;
        return;
    }
    tbody.innerHTML = projects.map(p =>
        `<tr>
            <td class="text-base-content/60">${p.platformName}</td>
            <td>${p.projectName}</td>
            <td class="text-right tabular-nums">${p.inputTokens.toLocaleString('nl-BE')}</td>
            <td class="text-right tabular-nums">${p.outputTokens.toLocaleString('nl-BE')}</td>
            <td class="text-right tabular-nums">${eur.format(p.costEur)}</td>
        </tr>`
    ).join('');
}

function renderPlatformFiltered(tbody: HTMLTableSectionElement, projects: ProjectCostAggregateDto[], emptyLabel: string): void {
    if (projects.length === 0) {
        tbody.innerHTML = `<tr><td colspan="4" class="text-center text-base-content/40 text-sm py-4">${escapeHtml(emptyLabel)}</td></tr>`;
        return;
    }
    tbody.innerHTML = projects.map(p =>
        `<tr>
            <td>${p.projectName}</td>
            <td class="text-right tabular-nums">${p.inputTokens.toLocaleString('nl-BE')}</td>
            <td class="text-right tabular-nums">${p.outputTokens.toLocaleString('nl-BE')}</td>
            <td class="text-right tabular-nums">${eur.format(p.costEur)}</td>
        </tr>`
    ).join('');
}

document.addEventListener('DOMContentLoaded', () => {
    const tbody = document.querySelector<HTMLTableSectionElement>('tbody[data-ai-usage]');
    if (!tbody) return;

    const container = tbody.closest<HTMLElement>('[data-ai-cost-container]');
    const platformIdStr = container?.dataset.platformId;
    const platformId = platformIdStr !== undefined ? parseInt(platformIdStr, 10) : null;
    const emptyLabel = container?.dataset.emptyLabel ?? '';

    async function fetchAndRender(): Promise<void> {
        try {
            const res = await fetch('/api/admin/ai-usage/projects');
            if (!res.ok) return;
            const all: ProjectCostAggregateDto[] = await res.json();

            if (platformId !== null) {
                renderPlatformFiltered(tbody!, all.filter(p => p.platformId === platformId), emptyLabel);
            } else {
                renderSystemWide(tbody!, all, emptyLabel);
            }
        } catch {
            // ignore network errors during polling
        }
    }

    fetchAndRender();
    setInterval(fetchAndRender, 15_000);
});
