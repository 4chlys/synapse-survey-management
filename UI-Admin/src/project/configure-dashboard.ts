interface WidgetOption {
    value: string;
    label: string;
}

interface WidgetMeta {
    cat: string;
    span: number;
}

const META: Record<string, WidgetMeta> = {
    ViewCount:             { cat: 'kpi',      span: 1 },
    CompletionCount:       { cat: 'kpi',      span: 1 },
    CompletionRate:        { cat: 'kpi',      span: 1 },
    ParticipationTimeline: { cat: 'timeline', span: 3 },
    TopCategories:         { cat: 'list',     span: 1 },
    TopIdeas:              { cat: 'list',     span: 1 },
    SurveyDistribution:    { cat: 'distrib',  span: 1 },
    IdeaFeed:              { cat: 'feed',     span: 1 },
    ModerationQueue:       { cat: 'moderate', span: 1 },
    QuestionSinglePie:     { cat: 'chart',    span: 1 },
    QuestionMultiBar:      { cat: 'chart',    span: 1 },
    QuestionLikertBar:     { cat: 'chart',    span: 1 },
    SocialMediaPost:       { cat: 'social',   span: 1 },
};

document.addEventListener('DOMContentLoaded', () => {
    const root = document.getElementById('configure-dashboard');
    if (!root) return;

    const optsEl = document.getElementById('cfg-widget-opts');
    const opts: WidgetOption[] = optsEl ? JSON.parse(optsEl.textContent ?? '[]') : [];

    const container = document.getElementById('slots-container') as HTMLElement;
    const previewGrid = document.getElementById('cfg-preview-grid') as HTMLElement;
    const addBtn = document.getElementById('cfg-add-slot');

    function getMeta(widgetType: string): WidgetMeta {
        return META[widgetType] ?? { cat: 'kpi', span: 1 };
    }

    function getLabel(widgetType: string): string {
        return opts.find(o => o.value === widgetType)?.label ?? widgetType;
    }

    function getRows(): HTMLElement[] {
        return Array.from(container.querySelectorAll<HTMLElement>('[data-slot-row]'));
    }

    function syncDot(row: HTMLElement): void {
        const sel = row.querySelector<HTMLSelectElement>('select');
        const dot = row.querySelector<HTMLElement>('.cfg-dot');
        if (!sel || !dot) return;
        dot.dataset.cat = getMeta(sel.value).cat;
    }

    function syncIndices(): void {
        getRows().forEach((row, i) => {
            const num = row.querySelector<HTMLElement>('.cfg-slot-num');
            if (num) num.textContent = String(i + 1).padStart(2, '0');
            const sel = row.querySelector<HTMLSelectElement>('select');
            if (sel) sel.name = `Slots[${i}].WidgetType`;
            const hid = row.querySelector<HTMLInputElement>('input[type=hidden]');
            if (hid) hid.name = `Slots[${i}].QuestionId`;
        });
    }

    function rebuildPreview(): void {
        previewGrid.innerHTML = '';
        getRows().forEach(row => {
            const wt = row.querySelector<HTMLSelectElement>('select')?.value ?? 'ViewCount';
            const m = getMeta(wt);
            const tile = document.createElement('div');
            tile.className = 'cfg-tile';
            tile.dataset.cat = m.cat;
            if (m.span > 1) tile.dataset.span = String(m.span);
            tile.textContent = getLabel(wt);
            previewGrid.appendChild(tile);
        });
    }

    function updateTile(index: number, widgetType: string): void {
        const tiles = previewGrid.querySelectorAll<HTMLElement>('.cfg-tile');
        if (tiles.length !== getRows().length) {
            rebuildPreview();
            return;
        }
        const tile = tiles[index];
        if (!tile) return;
        const m = getMeta(widgetType);
        tile.dataset.cat = m.cat;
        if (m.span > 1) tile.dataset.span = String(m.span);
        else delete tile.dataset.span;
        tile.textContent = getLabel(widgetType);
        tile.classList.add('cfg-tile--flash');
        setTimeout(() => tile.classList.remove('cfg-tile--flash'), 220);
    }

    function buildOptionHTML(): string {
        return opts.map(o =>
            `<option value="${o.value}">${o.label}</option>`
        ).join('');
    }

    // Change delegation for selects
    container.addEventListener('change', (e) => {
        const target = e.target as HTMLElement;
        const sel = target.closest('select') as HTMLSelectElement | null;
        if (!sel) return;
        const row = sel.closest<HTMLElement>('[data-slot-row]');
        if (!row) return;
        syncDot(row);
        const idx = getRows().indexOf(row);
        updateTile(idx, sel.value);
    });

    // Click delegation for remove buttons
    container.addEventListener('click', (e) => {
        const btn = (e.target as HTMLElement).closest<HTMLButtonElement>('[data-remove-slot]');
        if (!btn) return;
        const rows = getRows();
        if (rows.length <= 1) return;
        const row = btn.closest<HTMLElement>('[data-slot-row]')!;
        row.style.transition = 'opacity 0.15s, transform 0.15s';
        row.style.opacity = '0';
        row.style.transform = 'translateX(-6px)';
        setTimeout(() => {
            row.remove();
            syncIndices();
            rebuildPreview();
        }, 150);
    });

    // Add slot
    addBtn?.addEventListener('click', () => {
        const n = getRows().length;
        const row = document.createElement('div');
        row.className = 'cfg-slot';
        row.dataset.slotRow = '';
        row.innerHTML = `
            <span class="cfg-slot-num">${String(n + 1).padStart(2, '0')}</span>
            <span class="cfg-dot" data-cat="kpi"></span>
            <select name="Slots[${n}].WidgetType" class="select select-bordered select-sm flex-1 min-w-0">
                ${buildOptionHTML()}
            </select>
            <input type="hidden" name="Slots[${n}].QuestionId" value="" />
            <button type="button" class="cfg-remove btn btn-ghost btn-xs text-error px-1.5" data-remove-slot>
                <svg width="13" height="13" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
                    <path d="M18 6L6 18M6 6l12 12"/>
                </svg>
            </button>
        `;
        row.style.opacity = '0';
        row.style.transform = 'translateY(5px)';
        container.appendChild(row);
        requestAnimationFrame(() => {
            row.style.transition = 'opacity 0.18s, transform 0.18s';
            row.style.opacity = '1';
            row.style.transform = 'translateY(0)';
        });
        rebuildPreview();
    });

    // Init
    getRows().forEach(r => syncDot(r));
    rebuildPreview();
});
