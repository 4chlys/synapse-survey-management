import { toast } from './lib/notify';
import { wireQuestionMediaPanel, hydrateMediaPanel } from './lib/question-media';
import { loadQuestionsPageI18n } from './lib/questions-i18n';

document.addEventListener('DOMContentLoaded', () => {
    const i18n = loadQuestionsPageI18n();
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
    const textError = document.getElementById('text-error') as HTMLElement | null;
    const answersError = document.getElementById('answers-error') as HTMLElement | null;

    // pending section when picker → drawer; cleared after drawer opens
    let pendingSectionId = '';

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
        wireQuestionMediaPanel('card-', projectId);
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
        modal?.classList.add('modal-open');
    }

    function closeModal(): void {
        modal?.classList.remove('modal-open');
    }

    const createUrl = questionForm?.action ?? '';

    function resetModal(): void {
        const textArea = document.getElementById('QuestionText') as HTMLTextAreaElement | null;
        if (textArea) textArea.value = '';
        textError?.classList.add('hidden');
        answersError?.classList.add('hidden');
        const answerList = document.getElementById('answer-list');
        if (answerList) answerList.innerHTML = '';
        const firstBtn = document.querySelector<HTMLElement>('.type-btn');
        if (firstBtn) selectType(firstBtn.dataset.type ?? 'MultipleChoice', firstBtn);
        const sectionSelect = document.getElementById('SectionId') as HTMLSelectElement | null;
        if (sectionSelect) sectionSelect.value = '';
        const qIdInput = document.getElementById('QuestionId') as HTMLInputElement | null;
        if (qIdInput) qIdInput.value = '';
        const mandatoryCb = document.getElementById('IsMandatory-cb') as HTMLInputElement | null;
        if (mandatoryCb) mandatoryCb.checked = true;
        if (questionForm) questionForm.action = createUrl;
        const title = document.getElementById('modal-title');
        if (title) title.textContent = i18n.modalTitleCreate;
    }

    function openEditModal(questionId: number, text: string, format: string, answers: string[], sectionId: string, isMandatory: boolean, mediaUrl = '', mediaType = 0, mediaIsEmbed = false, mediaText = ''): void {
        resetModal();

        const qIdInput = document.getElementById('QuestionId') as HTMLInputElement | null;
        if (qIdInput) qIdInput.value = String(questionId);

        const textArea = document.getElementById('QuestionText') as HTMLTextAreaElement | null;
        if (textArea) textArea.value = text;

        const formatBtn = document.querySelector<HTMLElement>(`.type-btn[data-type="${format}"]`);
        if (formatBtn) selectType(format, formatBtn);

        answers.forEach(a => {
            addAnswer();
            const inputs = document.querySelectorAll<HTMLInputElement>('.answer-input');
            const last = inputs[inputs.length - 1];
            if (last) last.value = a;
        });

        const sectionSelect = document.getElementById('SectionId') as HTMLSelectElement | null;
        if (sectionSelect) sectionSelect.value = sectionId;

        const mandatoryEditCb = document.getElementById('IsMandatory-cb') as HTMLInputElement | null;
        if (mandatoryEditCb) mandatoryEditCb.checked = isMandatory;

        if (questionForm) questionForm.action = questionForm.dataset.updateUrl ?? '';

        const title = document.getElementById('modal-title');
        if (title) title.textContent = i18n.modalTitleEdit;

        updateDrawerTypeHeader(format);

        hydrateMediaPanel('', mediaUrl, mediaType, mediaIsEmbed, mediaText);

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

        // Clear answers error when switching type
        answersError?.classList.add('hidden');
    }

    function addAnswer(): void {
        const list = document.getElementById('answer-list');
        if (!list) return;
        answersError?.classList.add('hidden');
        const row = document.createElement('div');
        row.className = 'flex items-center gap-2';
        const input = document.createElement('input');
        input.type = 'text';
        input.className = 'answer-input input input-bordered input-xs flex-1';
        input.placeholder = i18n.answerPlaceholder;
        input.maxLength = 500;
        input.addEventListener('input', () => answersError?.classList.add('hidden'));
        const removeBtn = document.createElement('button');
        removeBtn.type = 'button';
        removeBtn.className = 'btn btn-ghost btn-xs text-base-content/50';
        removeBtn.textContent = '×';
        removeBtn.addEventListener('click', () => row.remove());
        row.appendChild(input);
        row.appendChild(removeBtn);
        list.appendChild(row);
    }

    addQuestionBtn?.addEventListener('click', () => openTypePicker());

    // Dashed add button rendered inside #questions-list by questionnaire-tree
    questionsList?.addEventListener('click', (e) => {
        const btn = (e.target as HTMLElement).closest<HTMLElement>('.open-add-modal');
        if (btn) openTypePicker();
    });

    // Section-scoped add button in section headers / empty-state buttons
    questionsList?.addEventListener('click', (e) => {
        const btn = (e.target as HTMLElement).closest<HTMLElement>('.add-in-section-btn');
        if (btn) openTypePicker(btn.dataset.sectionId ?? '');
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

    addAnswerBtn?.addEventListener('click', addAnswer);

    document.getElementById('QuestionText')?.addEventListener('input', () => {
        textError?.classList.add('hidden');
    });

    questionForm?.addEventListener('submit', (e: SubmitEvent) => {
        let valid = true;

        // Validate question text
        const textArea = document.getElementById('QuestionText') as HTMLTextAreaElement | null;
        if (!textArea?.value.trim()) {
            e.preventDefault();
            if (textError) textError.textContent = i18n.fieldRequired;
            textError?.classList.remove('hidden');
            valid = false;
        } else if ((textArea?.value.trim().length ?? 0) > 500) {
            e.preventDefault();
            if (textError) textError.textContent = i18n.maxQuestionText;
            textError?.classList.remove('hidden');
            valid = false;
        } else {
            textError?.classList.add('hidden');
        }

        // Validate answers for MCQ and SCQ — read before the validation block so both branches see it
        const currentType = (document.getElementById('QuestionFormat') as HTMLInputElement | null)?.value;
        if (currentType === 'MultipleChoice' || currentType === 'SingleChoice') {
            const answerInputs = Array.from(document.querySelectorAll<HTMLInputElement>('.answer-input'));

            if (answerInputs.length < 2) {
                e.preventDefault();
                if (answersError) {
                    answersError.textContent = i18n.minTwoAnswers;
                    answersError.classList.remove('hidden');
                }
                valid = false;
            } else if (answerInputs.some(a => !a.value.trim())) {
                e.preventDefault();
                if (answersError) {
                    answersError.textContent = i18n.allAnswersFilled;
                    answersError.classList.remove('hidden');
                }
                valid = false;
            } else {
                answersError?.classList.add('hidden');
            }
        }

        if (!valid) return;

        // Collect answers into hidden field (only for choice types)
        const raw = document.getElementById('AnswerChoicesRaw') as HTMLInputElement | null;
        if (raw) {
            const isChoiceType = currentType === 'MultipleChoice' || currentType === 'SingleChoice';
            if (isChoiceType) {
                const answers = Array.from(document.querySelectorAll<HTMLInputElement>('.answer-input'))
                    .map(i => i.value.trim())
                    .filter(v => v);
                raw.value = answers.join('\n');
            } else {
                raw.value = '';
            }
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

    // Edit question via event delegation
    questionsList?.addEventListener('click', (e) => {
        const btn = (e.target as HTMLElement).closest<HTMLElement>('.question-edit-btn');
        if (!btn) return;
        const id = parseInt(btn.dataset.questionId ?? '0', 10);
        const text = btn.dataset.text ?? '';
        const format = btn.dataset.format ?? 'MultipleChoice';
        const answers = JSON.parse(btn.dataset.answers ?? '[]') as string[];
        const sectionId = btn.dataset.sectionId ?? '';
        const isMandatory = btn.dataset.isMandatory === 'true';
        const mediaUrl = btn.dataset.mediaUrl ?? '';
        const mediaType = parseInt(btn.dataset.mediaType ?? '0', 10);
        const mediaIsEmbed = btn.dataset.mediaIsEmbed === 'true';
        const mediaText = btn.dataset.mediaText ?? '';
        openEditModal(id, text, format, answers, sectionId, isMandatory, mediaUrl, mediaType, mediaIsEmbed, mediaText);
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

    // Card form: show/hide answers section + AJAX submit
    const cardForm = document.getElementById('card-question-form') as HTMLFormElement | null;
    const cardFormat = document.getElementById('card-format') as HTMLSelectElement | null;
    const cardAnswersSection = document.getElementById('card-answers-section') as HTMLElement | null;
    const cardAnswerList = document.getElementById('card-answer-list') as HTMLElement | null;
    const cardAddAnswer = document.getElementById('card-add-answer') as HTMLButtonElement | null;
    const cardAnswersRaw = document.getElementById('card-answers-raw') as HTMLInputElement | null;
    const cardAnswersError = document.getElementById('card-answers-error') as HTMLElement | null;

    function isCardChoice(): boolean {
        return cardFormat?.value === 'MultipleChoice' || cardFormat?.value === 'SingleChoice';
    }

    function updateCardVisibility(): void {
        if (!cardAnswersSection) return;
        const show = isCardChoice();
        cardAnswersSection.classList.toggle('hidden', !show);
        if (show) cardAnswersSection.style.display = 'flex';
        else cardAnswersSection.style.display = '';
    }

    function addCardAnswer(value = ''): void {
        if (!cardAnswerList) return;
        const row = document.createElement('div');
        row.className = 'flex gap-2 items-center';
        const input = document.createElement('input');
        input.type = 'text';
        input.className = 'input input-bordered input-xs flex-1';
        input.placeholder = i18n.answerPlaceholder;
        input.maxLength = 500;
        input.value = value;
        input.addEventListener('input', () => cardAnswersError?.classList.add('hidden'));
        const del = document.createElement('button');
        del.type = 'button';
        del.className = 'btn btn-ghost btn-xs text-error';
        del.textContent = '×';
        del.addEventListener('click', () => row.remove());
        row.appendChild(input);
        row.appendChild(del);
        cardAnswerList.appendChild(row);
    }

    cardFormat?.addEventListener('change', updateCardVisibility);
    cardAddAnswer?.addEventListener('click', () => addCardAnswer());

    cardForm?.addEventListener('submit', async (e: SubmitEvent) => {
        e.preventDefault();

        if (isCardChoice()) {
            const inputs = Array.from(cardAnswerList?.querySelectorAll<HTMLInputElement>('input[type=text]') ?? []);
            const values = inputs.map(i => i.value.trim()).filter(v => v);
            if (values.length < 2) {
                if (cardAnswersError) {
                    cardAnswersError.textContent = i18n.minTwoAnswers;
                    cardAnswersError.classList.remove('hidden');
                }
                return;
            }
            cardAnswersError?.classList.add('hidden');
            if (cardAnswersRaw) cardAnswersRaw.value = values.join('\n');
        } else {
            if (cardAnswersRaw) cardAnswersRaw.value = '';
        }

        try {
            const response = await fetch(cardForm.action, {
                method: 'POST',
                body: new FormData(cardForm),
            });
            if (response.ok) {
                cardForm.reset();
                if (cardAnswerList) cardAnswerList.innerHTML = '';
                cardAnswersError?.classList.add('hidden');
                updateCardVisibility();
                document.dispatchEvent(new CustomEvent('questions-reload'));
                toast(i18n.savedToast, 'success');
            } else {
                toast(i18n.saveFailedToast, 'error');
            }
        } catch {
            toast(i18n.saveFailedToast, 'error');
        }
    });
});
