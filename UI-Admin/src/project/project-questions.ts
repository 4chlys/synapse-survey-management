import { toast } from '../lib/notify';
import { wireQuestionMediaPanel, hydrateMediaPanel } from '../lib/question-media';
import { readQuestionsI18n, readFormI18n } from '../lib/questions-i18n';

document.addEventListener('DOMContentLoaded', () => {
    const modal = document.getElementById('modal') as HTMLDivElement | null;
    const typePicker = document.getElementById('type-picker') as HTMLDivElement | null;
    const typePickerClose = document.getElementById('type-picker-close') as HTMLButtonElement | null;
    const typePickerBackdrop = document.getElementById('type-picker-backdrop') as HTMLButtonElement | null;
    const drawerTypeIcon = document.getElementById('drawer-type-icon') as HTMLElement | null;
    const drawerTypeLabel = document.getElementById('drawer-type-label') as HTMLElement | null;

    const questionForm = document.getElementById('question-form') as HTMLFormElement | null;
    const questionsList = document.getElementById('questions-list') as HTMLDivElement | null;
    const addQuestionBtn = document.getElementById('add-question-btn') as HTMLButtonElement | null;
    const modalClose = document.getElementById('modal-close') as HTMLButtonElement | null;
    const modalCancel = document.getElementById('modal-cancel') as HTMLButtonElement | null;
    const addAnswerBtn = document.getElementById('add-answer-btn') as HTMLButtonElement | null;
    const askFirstJa = document.getElementById('ask-first-ja') as HTMLButtonElement | null;
    const answersError = document.getElementById('answers-error') as HTMLElement | null;

    // This module only drives the Questions page. Like the other feature modules
    // (e.g. project-shortform-autofill), bail when our page isn't present —
    // otherwise the document-wide `panes()` query below toggles the `hidden`
    // state of unrelated [data-translation-pane] elements (the project language
    // tabs on Create), fighting project-setup.ts over the same panes.
    if (!questionForm) return;

    const i18n = { ...readQuestionsI18n(questionsList), ...readFormI18n(questionForm) };

    // Multi-language setup: derived from #questions-list dataset (populated by
    // ProjectController.Questions). Supported languages drive the tab strip,
    // status dots, slot-row inputs, and per-language submit validation.
    const supportedLanguages = (questionsList?.dataset['supportedLanguages'] ?? 'nl')
        .split(',').map(s => s.trim()).filter(Boolean);
    const defaultLanguage = questionsList?.dataset['defaultLanguageCode'] ?? supportedLanguages[0] ?? 'nl';

    interface QuestionTranslationData {
        languageCode: string;
        text: string;
        answerChoices: string[];
        mediaText: string | null;
    }
    interface EndingAnswerTranslationData { languageCode: string; value: string; }
    interface EndingAnswerSlotData {
        endingAnswerId: number;
        translations: EndingAnswerTranslationData[];
    }

    function panes(): HTMLElement[] {
        return Array.from(document.querySelectorAll<HTMLElement>('[data-translation-pane]'));
    }
    function tabs(): HTMLButtonElement[] {
        return Array.from(document.querySelectorAll<HTMLButtonElement>('.question-translation-tab'));
    }
    function activateTab(code: string): void {
        // The active language is read back from [aria-selected="true"] by
        // questionsupportchat.resolveQuestionLanguage at send time, so no
        // module-level variable is needed here.
        panes().forEach(p => { p.hidden = p.dataset.translationPane !== code; });
        tabs().forEach(t => {
            const active = t.dataset.translationTab === code;
            t.classList.toggle('bg-base-100', active);
            t.classList.toggle('text-primary', active);
            t.classList.toggle('shadow-sm', active);
            t.classList.toggle('ring-1', active);
            t.classList.toggle('ring-base-300', active);
            t.setAttribute('aria-selected', active ? 'true' : 'false');
        });
    }

    // Status dot per tab: green when this language's Text is filled AND every
    // answer-slot row has a non-empty value for this language (parallel-array
    // invariant — if a slot has text in one language, it must have text in all).
    function updateStatusDot(code: string): void {
        const pane = document.querySelector<HTMLElement>(`[data-translation-pane="${code}"]`);
        const tab = document.querySelector<HTMLElement>(`.question-translation-tab[data-translation-tab="${code}"]`);
        if (!pane || !tab) return;
        const text = pane.querySelector<HTMLTextAreaElement>('.question-text-input')?.value.trim() ?? '';
        const isChoice = isChoiceType();
        const slots = Array.from(document.querySelectorAll<HTMLInputElement>(`.answer-input[data-language="${code}"]`));
        const allSlotsFilled = !isChoice || (slots.length > 0 && slots.every(s => s.value.trim().length > 0));
        const complete = text.length > 0 && allSlotsFilled;
        const dot = tab.querySelector<HTMLElement>('[data-translation-status]');
        if (dot) {
            dot.classList.toggle('bg-success', complete);
            dot.classList.toggle('bg-warning', !complete);
        }
    }
    function updateAllStatusDots(): void { supportedLanguages.forEach(updateStatusDot); }

    function isChoiceType(): boolean {
        const v = (document.getElementById('QuestionFormat') as HTMLInputElement | null)?.value;
        return v === 'MultipleChoice' || v === 'SingleChoice';
    }

    // pending context when picker → drawer; cleared after drawer opens
    let pendingSectionId = '';
    let pendingParentId = '';
    let pendingChoiceIndex = '';

    const TYPE_META: Record<string, { label: string; iconBg: string; iconText: string; svg: string }> = {
        MultipleChoice: { label: i18n.formatMCQ,    iconBg: 'bg-primary/15',   iconText: 'text-primary',   svg: 'M9 12l2 2 4-4M5 8h14M5 16h10' },
        SingleChoice:   { label: i18n.formatSCQ,    iconBg: 'bg-secondary/15', iconText: 'text-secondary', svg: 'M9 12l2 2 4-4M3 12a9 9 0 1018 0 9 9 0 00-18 0z' },
        Open:           { label: i18n.formatOpen,   iconBg: 'bg-info/15',      iconText: 'text-info',      svg: 'M4 6h16M4 10h16M4 14h10' },
        Likert:         { label: i18n.formatLikert, iconBg: 'bg-warning/15',   iconText: 'text-warning',   svg: 'M7 20V10M12 20V4M17 20V14' },
    };

    function updateDrawerTypeHeader(type: string): void {
        const m = TYPE_META[type] ?? TYPE_META.MultipleChoice;
        if (drawerTypeLabel) drawerTypeLabel.textContent = m.label;
        if (drawerTypeIcon) {
            drawerTypeIcon.className = `w-10 h-10 rounded-xl ${m.iconBg} ${m.iconText} flex items-center justify-center flex-shrink-0`;
            drawerTypeIcon.innerHTML = `<svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="${m.svg}"/></svg>`;
        }
    }

    function openTypePicker(sectionId = ''): void {
        pendingSectionId = sectionId;
        typePicker?.classList.add('modal-open');
    }
    function closeTypePicker(): void {
        typePicker?.classList.remove('modal-open');
    }

    if (new URLSearchParams(window.location.search).get('saved') === '1')
        toast(i18n.savedToast, 'success');

    const projectId = parseInt(questionForm?.dataset.projectId ?? questionsList?.dataset.projectId ?? '0', 10);

    // Wire media panel for the modal form (prefix = '' so IDs match: MediaType, media-upload-block, etc.)
    if (projectId) {
        wireQuestionMediaPanel('', projectId);
    }

    function openModal(sectionId = '', type = 'MultipleChoice'): void {
        resetModal();
        const formatBtn = document.querySelector<HTMLElement>(`.type-btn[data-type="${type}"]`);
        if (formatBtn) selectType(type, formatBtn);
        updateDrawerTypeHeader(type);
        if (sectionId) {
            const sectionSelect = document.getElementById('SectionId') as HTMLSelectElement | null;
            if (sectionSelect) sectionSelect.value = sectionId;
        }
        const parentInput = document.getElementById('ParentQuestionId') as HTMLInputElement | null;
        const choiceInput = document.getElementById('ChoiceIndex') as HTMLInputElement | null;
        if (parentInput) parentInput.value = pendingParentId;
        if (choiceInput) choiceInput.value = pendingChoiceIndex;
        modal?.classList.add('modal-open');
    }

    function closeModal(): void {
        modal?.classList.remove('modal-open');
    }

    const createUrl = questionForm?.action ?? '';

    function resetModal(): void {
        // Clear every per-language pane (Text + MediaText + the hidden
        // AnswerChoicesRaw bridge field that gets populated at submit).
        panes().forEach(p => {
            p.querySelectorAll<HTMLTextAreaElement>('.question-text-input').forEach(t => t.value = '');
            p.querySelectorAll<HTMLTextAreaElement>('.question-media-text-input').forEach(t => t.value = '');
            p.querySelectorAll<HTMLInputElement>('[data-translation-field="answer-choices-raw"]').forEach(h => h.value = '');
            const counter = p.querySelector<HTMLElement>('[data-media-text-counter]');
            if (counter) counter.textContent = '0 / 2000';
        });
        // Clear per-language text errors
        document.querySelectorAll<HTMLElement>('.question-text-error').forEach(e => e.classList.add('hidden'));
        answersError?.classList.add('hidden');
        const answerList = document.getElementById('answer-list');
        if (answerList) answerList.innerHTML = '';
        const firstBtn = document.querySelector<HTMLElement>('.type-btn');
        if (firstBtn) selectType(firstBtn.dataset.type ?? 'MultipleChoice', firstBtn);
        const sectionSelect = document.getElementById('SectionId') as HTMLSelectElement | null;
        if (sectionSelect) sectionSelect.value = '';
        const qIdInput = document.getElementById('QuestionId') as HTMLInputElement | null;
        if (qIdInput) qIdInput.value = '';
        const parentInput = document.getElementById('ParentQuestionId') as HTMLInputElement | null;
        if (parentInput) parentInput.value = '';
        const choiceInput = document.getElementById('ChoiceIndex') as HTMLInputElement | null;
        if (choiceInput) choiceInput.value = '';
        const mandatoryCb = document.getElementById('IsMandatory-cb') as HTMLInputElement | null;
        if (mandatoryCb) mandatoryCb.checked = true;
        if (questionForm) questionForm.action = createUrl;
        const title = document.getElementById('modal-title');
        if (title) title.textContent = i18n.modalTitleCreate;
        activateTab(defaultLanguage);
        updateAllStatusDots();
    }

    function openEditModal(
        questionId: number,
        format: string,
        sectionId: string,
        isMandatory: boolean,
        mediaUrl: string,
        mediaType: number,
        mediaIsEmbed: boolean,
        translations: QuestionTranslationData[],
        _endingSlots: EndingAnswerSlotData[],
    ): void {
        resetModal();

        const qIdInput = document.getElementById('QuestionId') as HTMLInputElement | null;
        if (qIdInput) qIdInput.value = String(questionId);

        const formatBtn = document.querySelector<HTMLElement>(`.type-btn[data-type="${format}"]`);
        if (formatBtn) selectType(format, formatBtn);

        // Pad missing-language translations so every supported language has a
        // pane value; empty strings are fine — the user re-authors them.
        const byLang = new Map(translations.map(t => [t.languageCode, t]));
        supportedLanguages.forEach(code => {
            const t = byLang.get(code);
            const pane = document.querySelector<HTMLElement>(`[data-translation-pane="${code}"]`);
            if (!pane) return;
            const txt = pane.querySelector<HTMLTextAreaElement>('.question-text-input');
            if (txt) txt.value = t?.text ?? '';
            const mt = pane.querySelector<HTMLTextAreaElement>('.question-media-text-input');
            if (mt) {
                mt.value = t?.mediaText ?? '';
                const c = pane.querySelector<HTMLElement>('[data-media-text-counter]');
                if (c) c.textContent = `${mt.value.length} / 2000`;
            }
        });

        // Slot rows: derive slot count from the longest translation. Each row
        // gets one input per supported language, prefilled from translations.
        const slotCount = translations.reduce((m, t) => Math.max(m, t.answerChoices.length), 0);
        for (let i = 0; i < slotCount; i++) {
            addAnswerSlot();
            supportedLanguages.forEach(code => {
                const t = byLang.get(code);
                const input = document.querySelector<HTMLInputElement>(
                    `#answer-list .answer-row:last-child .answer-input[data-language="${code}"]`,
                );
                if (input) input.value = t?.answerChoices[i] ?? '';
            });
        }

        const sectionSelect = document.getElementById('SectionId') as HTMLSelectElement | null;
        if (sectionSelect) sectionSelect.value = sectionId;

        const mandatoryEditCb = document.getElementById('IsMandatory-cb') as HTMLInputElement | null;
        if (mandatoryEditCb) mandatoryEditCb.checked = isMandatory;

        if (questionForm) questionForm.action = questionForm.dataset.updateUrl ?? '';

        const title = document.getElementById('modal-title');
        if (title) title.textContent = i18n.modalTitleEdit;

        updateDrawerTypeHeader(format);

        // Media URL/type/embed are language-agnostic; MediaText is set per-pane above.
        hydrateMediaPanel('', mediaUrl, mediaType, mediaIsEmbed, '');

        updateAllStatusDots();
        modal?.classList.add('modal-open');
    }

    function selectType(type: string, activeBtn: HTMLElement): void {
        const formatInput = document.getElementById('QuestionFormat') as HTMLInputElement | null;
        if (formatInput) formatInput.value = type;

        document.querySelectorAll<HTMLElement>('.type-btn').forEach(b => {
            b.classList.remove('btn-primary');
            b.classList.add('btn-ghost');
        });
        activeBtn.classList.remove('btn-ghost');
        activeBtn.classList.add('btn-primary');

        document.querySelectorAll<HTMLElement>('.type-selected-label').forEach(l => l.classList.add('hidden'));
        activeBtn.parentElement?.querySelector<HTMLElement>('.type-selected-label')?.classList.remove('hidden');

        const showAnswers = type === 'MultipleChoice' || type === 'SingleChoice';
        const answersSection = document.getElementById('answers-section');
        if (answersSection) answersSection.style.display = showAnswers ? 'block' : 'none';

        // Clear answers error when switching type. Status dots depend on
        // whether slots are required, so refresh them.
        answersError?.classList.add('hidden');
        updateAllStatusDots();
    }

    // A slot row holds one input per supported language plus a remove button.
    // Removing the row drops the slot from every language at once, preserving
    // the parallel-array invariant; the slot count grows/shrinks atomically.
    function addAnswerSlot(): void {
        const list = document.getElementById('answer-list');
        if (!list) return;
        answersError?.classList.add('hidden');
        const row = document.createElement('div');
        row.className = 'answer-row flex items-center gap-2';
        supportedLanguages.forEach(code => {
            const input = document.createElement('input');
            input.type = 'text';
            input.className = 'answer-input input input-bordered input-xs flex-1';
            input.placeholder = supportedLanguages.length > 1
                ? `${i18n.answerPlaceholder} (${code.toUpperCase()})`
                : i18n.answerPlaceholder;
            input.maxLength = 500;
            input.dataset['language'] = code;
            input.addEventListener('input', () => {
                answersError?.classList.add('hidden');
                updateStatusDot(code);
            });
            row.appendChild(input);
        });
        const removeBtn = document.createElement('button');
        removeBtn.type = 'button';
        removeBtn.className = 'btn btn-ghost btn-xs text-base-content/50';
        removeBtn.textContent = '×';
        removeBtn.addEventListener('click', () => {
            row.remove();
            updateAllStatusDots();
        });
        row.appendChild(removeBtn);
        list.appendChild(row);
    }

    addQuestionBtn?.addEventListener('click', () => {
        pendingParentId = '';
        pendingChoiceIndex = '';
        openTypePicker();
    });

    // Dashed add button rendered inside #questions-list by questionnaire-tree
    questionsList?.addEventListener('click', (e) => {
        const btn = (e.target as HTMLElement).closest<HTMLElement>('.open-add-modal');
        if (btn) {
            pendingParentId = '';
            pendingChoiceIndex = '';
            openTypePicker();
        }
    });

    // Section-scoped add button in section headers / empty-state buttons
    questionsList?.addEventListener('click', (e) => {
        const btn = (e.target as HTMLElement).closest<HTMLElement>('.add-in-section-btn');
        if (btn) {
            pendingParentId = '';
            pendingChoiceIndex = '';
            openTypePicker(btn.dataset.sectionId ?? '');
        }
    });

    // Sub-question add button (inside answer-group drop zones)
    questionsList?.addEventListener('click', (e) => {
        const btn = (e.target as HTMLElement).closest<HTMLElement>('.add-sub-question-btn');
        if (!btn) return;
        pendingParentId = btn.dataset.parentId ?? '';
        pendingChoiceIndex = btn.dataset.parentChoiceIndex ?? '';
        openTypePicker();
    });

    // Type picker tile → opens drawer with chosen type + pending section
    document.querySelectorAll<HTMLElement>('.picker-tile').forEach(tile => {
        tile.addEventListener('click', () => {
            const type = tile.dataset.type ?? 'MultipleChoice';
            const sec = pendingSectionId;
            closeTypePicker();
            openModal(sec, type);
        });
    });
    typePickerClose?.addEventListener('click', closeTypePicker);
    typePickerBackdrop?.addEventListener('click', closeTypePicker);
    typePicker?.addEventListener('click', (e) => { if (e.target === typePicker) closeTypePicker(); });

    modalClose?.addEventListener('click', closeModal);
    modalCancel?.addEventListener('click', closeModal);
    modal?.addEventListener('click', (e) => { if (e.target === modal) closeModal(); });

    document.querySelectorAll<HTMLElement>('.type-btn').forEach(btn => {
        btn.addEventListener('click', () => selectType(btn.dataset.type ?? '', btn));
    });

    addAnswerBtn?.addEventListener('click', () => {
        addAnswerSlot();
        updateAllStatusDots();
    });

    // Tab switching + per-language text-input wiring (status dots + clear error).
    tabs().forEach(t => t.addEventListener('click', () => activateTab(t.dataset.translationTab ?? defaultLanguage)));
    document.querySelectorAll<HTMLTextAreaElement>('.question-text-input').forEach(ta => {
        ta.addEventListener('input', () => {
            const code = ta.dataset['language'] ?? defaultLanguage;
            const pane = ta.closest<HTMLElement>('[data-translation-pane]');
            pane?.querySelector<HTMLElement>(`.question-text-error[data-language="${code}"]`)?.classList.add('hidden');
            updateStatusDot(code);
        });
    });
    document.querySelectorAll<HTMLTextAreaElement>('.question-media-text-input').forEach(ta => {
        const code = ta.dataset['language'] ?? defaultLanguage;
        const counter = document.querySelector<HTMLElement>(`[data-media-text-counter="${code}"]`);
        ta.addEventListener('input', () => {
            if (counter) counter.textContent = `${ta.value.length} / 2000`;
        });
    });
    activateTab(defaultLanguage);
    updateAllStatusDots();

    questionForm?.addEventListener('submit', async (e: SubmitEvent) => {
        e.preventDefault();
        let valid = true;
        let firstBadLang: string | null = null;

        // Per-language Text required (≤ 500 chars). Switch to the first bad
        // tab so the inline error is visible.
        supportedLanguages.forEach(code => {
            const pane = document.querySelector<HTMLElement>(`[data-translation-pane="${code}"]`);
            if (!pane) return;
            const ta = pane.querySelector<HTMLTextAreaElement>('.question-text-input');
            const err = pane.querySelector<HTMLElement>(`.question-text-error[data-language="${code}"]`);
            const val = ta?.value.trim() ?? '';
            if (val.length === 0) {
                if (err) { err.textContent = i18n.fieldRequired; err.classList.remove('hidden'); }
                valid = false; firstBadLang ??= code;
            } else if (val.length > 500) {
                if (err) { err.textContent = i18n.maxQuestionText; err.classList.remove('hidden'); }
                valid = false; firstBadLang ??= code;
            } else {
                err?.classList.add('hidden');
            }
        });

        // Slot rows: for MCQ/SCQ, at least 2 slots; every slot must be filled
        // for every supported language (parallel-array invariant — partial
        // translations are rejected, not silently dropped, so authors notice).
        const currentType = (document.getElementById('QuestionFormat') as HTMLInputElement | null)?.value;
        const isChoice = currentType === 'MultipleChoice' || currentType === 'SingleChoice';
        const rows = Array.from(document.querySelectorAll<HTMLElement>('#answer-list .answer-row'));
        if (isChoice) {
            if (rows.length < 2) {
                if (answersError) {
                    answersError.textContent = i18n.minTwoAnswers;
                    answersError.classList.remove('hidden');
                }
                valid = false;
            } else {
                const anyBlank = rows.some(row =>
                    Array.from(row.querySelectorAll<HTMLInputElement>('.answer-input'))
                        .some(i => i.value.trim().length === 0));
                if (anyBlank) {
                    if (answersError) {
                        answersError.textContent = i18n.allAnswersFilled;
                        answersError.classList.remove('hidden');
                    }
                    valid = false;
                } else {
                    answersError?.classList.add('hidden');
                }
            }
        }

        if (!valid) {
            if (firstBadLang) activateTab(firstBadLang);
            return;
        }

        // Serialize answer choices: per language, walk slot rows in order and
        // collect that language's input value (trimmed), join with '\n' into
        // the matching Translations[i].AnswerChoicesRaw hidden bridge field.
        // Open/Likert clear AnswerChoicesRaw for every language so the slot
        // count goes to zero on the server side.
        supportedLanguages.forEach(code => {
            const hidden = document.querySelector<HTMLInputElement>(
                `[data-translation-field="answer-choices-raw"][data-language="${code}"]`);
            if (!hidden) return;
            if (!isChoice) { hidden.value = ''; return; }
            const values = rows.map(row =>
                row.querySelector<HTMLInputElement>(`.answer-input[data-language="${code}"]`)?.value.trim() ?? '');
            hidden.value = values.join('\n');
        });

        try {
            const response = await fetch(questionForm.action, {
                method: 'POST',
                body: new FormData(questionForm),
            });
            if (response.ok) {
                closeModal();
                document.dispatchEvent(new CustomEvent('questions-reload'));
                toast(i18n.savedToast, 'success');
            } else {
                toast(i18n.saveFailedToast, 'error');
            }
        } catch {
            toast(i18n.saveFailedToast, 'error');
        }
    });

    // Collapse/expand questions via event delegation
    questionsList?.addEventListener('click', (e) => {
        const btn = (e.target as HTMLElement).closest<HTMLElement>('.question-toggle');
        if (!btn) return;
        const id = btn.dataset.questionId;
        if (!id) return;
        const answersDiv = document.getElementById('answers-' + id);
        const arrow = document.getElementById('arrow-' + id);
        if (!answersDiv || !arrow) return;
        const isHidden = answersDiv.classList.toggle('hidden');
        arrow.textContent = isHidden ? '>' : 'v';
    });

    // Edit question via event delegation. The card's data-translations /
    // data-ending-answer-slots are JSON blobs serialized by questionnaire-tree,
    // carrying every supported language so the modal can rehydrate any tab.
    questionsList?.addEventListener('click', (e) => {
        const btn = (e.target as HTMLElement).closest<HTMLElement>('.question-edit-btn');
        if (!btn) return;
        const id = parseInt(btn.dataset.questionId ?? '0', 10);
        const format = btn.dataset.format ?? 'MultipleChoice';
        const sectionId = btn.dataset.sectionId ?? '';
        const isMandatory = btn.dataset.isMandatory === 'true';
        const mediaUrl = btn.dataset.mediaUrl ?? '';
        const mediaType = parseInt(btn.dataset.mediaType ?? '0', 10);
        const mediaIsEmbed = btn.dataset.mediaIsEmbed === 'true';
        let translations: QuestionTranslationData[] = [];
        let endingSlots: EndingAnswerSlotData[] = [];
        try { translations = JSON.parse(btn.dataset.translations ?? '[]') as QuestionTranslationData[]; }
        catch { translations = []; }
        try { endingSlots = JSON.parse(btn.dataset.endingAnswerSlots ?? '[]') as EndingAnswerSlotData[]; }
        catch { endingSlots = []; }
        openEditModal(id, format, sectionId, isMandatory, mediaUrl, mediaType, mediaIsEmbed, translations, endingSlots);
    });

    // Validate at least 1 question before proceeding to next step
    const nextBtn = document.getElementById('questions-next-btn') as HTMLButtonElement | null;
    const nextError = document.getElementById('questions-next-error') as HTMLElement | null;

    nextBtn?.addEventListener('click', () => {
        const count = parseInt(nextBtn.dataset.questionCount ?? '0', 10);
        if (count < 1) {
            nextError?.classList.remove('hidden');
            return;
        }
        nextError?.classList.add('hidden');
        window.location.href = nextBtn.dataset.href ?? '/';
    });

    // AskFirst: reveal AI panel on click
    askFirstJa?.addEventListener('click', () => {
        document.getElementById('ask-first-prompt')?.classList.add('hidden');
        const panel = document.getElementById('ai-content');
        panel?.classList.remove('hidden');
        panel?.classList.add('flex');
    });

    // (Dead "card form" block removed: #card-question-form had no Razor view
    // binding it; only the drawer-style modal authors questions now.)
});
