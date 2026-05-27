import type { WidgetOption } from './project-types';

let _opts: WidgetOption[] = [];
let _container: HTMLElement;
let _previewGrid: HTMLElement;

function getMeta(widgetType: string): { cat: string; span: number } {
    const opt = _opts.find(o => o.value === widgetType);
    return opt ?? { cat: 'kpi', span: 1 };
}

function getLabel(widgetType: string): string {
    return _opts.find(o => o.value === widgetType)?.label ?? widgetType;
}

function getRows(): HTMLElement[] {
    return Array.from(_container.querySelectorAll<HTMLElement>('[data-slot-row]'));
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
    _previewGrid.innerHTML = '';
    getRows().forEach(row => {
        const wt = row.querySelector<HTMLSelectElement>('select')?.value ?? 'ViewCount';
        const m = getMeta(wt);
        const tile = document.createElement('div');
        tile.className = 'cfg-tile';
        tile.dataset.cat = m.cat;
        if (m.span > 1) tile.dataset.span = String(m.span);
        tile.textContent = getLabel(wt);
        _previewGrid.appendChild(tile);
    });
}

function updateTile(index: number, widgetType: string): void {
    const tiles = _previewGrid.querySelectorAll<HTMLElement>('.cfg-tile');
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
    return _opts.map(o => `<option value="${o.value}">${o.label}</option>`).join('');
}

export function initConfigureDashboard(): void {
    const root = document.getElementById('configure-dashboard');
    if (!root) return;

    const optsEl = document.getElementById('cfg-widget-opts');
    _opts = optsEl ? JSON.parse(optsEl.textContent ?? '[]') : [];

    const container = document.getElementById('slots-container') as HTMLElement | null;
    const previewGrid = document.getElementById('cfg-preview-grid') as HTMLElement | null;
    const addBtn = document.getElementById('cfg-add-slot');

    if (!container || !previewGrid) return;
    _container = container;
    _previewGrid = previewGrid;

    _container.addEventListener('change', (e) => {
        const target = e.target as HTMLElement;
        const sel = target.closest('select') as HTMLSelectElement | null;
        if (!sel) return;
        const row = sel.closest<HTMLElement>('[data-slot-row]');
        if (!row) return;
        syncDot(row);
        const idx = getRows().indexOf(row);
        updateTile(idx, sel.value);
    });

    _container.addEventListener('click', (e) => {
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
        _container.appendChild(row);
        requestAnimationFrame(() => {
            row.style.transition = 'opacity 0.18s, transform 0.18s';
            row.style.opacity = '1';
            row.style.transform = 'translateY(0)';
        });
        rebuildPreview();
    });

    getRows().forEach(r => syncDot(r));
    rebuildPreview();
}
