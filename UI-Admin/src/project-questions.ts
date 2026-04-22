document.addEventListener('DOMContentLoaded', () => {
    const modal = document.getElementById('modal') as HTMLDivElement | null;
    if (!modal) return;

    const questionForm = document.getElementById('question-form') as HTMLFormElement | null;
    const questionsList = document.getElementById('questions-list') as HTMLDivElement | null;
    const addQuestionBtn = document.getElementById('add-question-btn') as HTMLButtonElement | null;
    const modalClose = document.getElementById('modal-close') as HTMLButtonElement | null;
    const modalCancel = document.getElementById('modal-cancel') as HTMLButtonElement | null;
    const addAnswerBtn = document.getElementById('add-answer-btn') as HTMLButtonElement | null;
    const askFirstJa = document.getElementById('ask-first-ja') as HTMLButtonElement | null;
    const textError = document.getElementById('text-error') as HTMLElement | null;
    const answersError = document.getElementById('answers-error') as HTMLElement | null;

    function openModal(): void {
        resetModal();
        modal.classList.remove('hidden');
    }

    function closeModal(): void {
        modal.classList.add('hidden');
    }

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
    }

    function selectType(type: string, activeBtn: HTMLElement): void {
        const formatInput = document.getElementById('QuestionFormat') as HTMLInputElement | null;
        if (formatInput) formatInput.value = type;

        document.querySelectorAll<HTMLElement>('.type-btn').forEach(b => b.classList.remove('bg-gray-600'));
        activeBtn.classList.add('bg-gray-600');

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
        input.className = 'answer-input flex-1 bg-gray-800 border border-gray-500 text-white text-xs px-3 py-2 focus:outline-none';
        input.placeholder = 'Antwoord...';
        input.addEventListener('input', () => answersError?.classList.add('hidden'));
        const removeBtn = document.createElement('button');
        removeBtn.type = 'button';
        removeBtn.className = 'text-gray-400 hover:text-white text-base px-1';
        removeBtn.textContent = '×';
        removeBtn.addEventListener('click', () => row.remove());
        row.appendChild(input);
        row.appendChild(removeBtn);
        list.appendChild(row);
    }

    addQuestionBtn?.addEventListener('click', openModal);
    modalClose?.addEventListener('click', closeModal);
    modalCancel?.addEventListener('click', closeModal);
    modal.addEventListener('click', (e) => { if (e.target === modal) closeModal(); });

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
            textError?.classList.remove('hidden');
            valid = false;
        } else {
            textError?.classList.add('hidden');
        }

        // Validate answers for MCQ and SCQ
        const currentType = (document.getElementById('QuestionFormat') as HTMLInputElement | null)?.value;
        if (currentType === 'MultipleChoice' || currentType === 'SingleChoice') {
            const answerInputs = Array.from(document.querySelectorAll<HTMLInputElement>('.answer-input'));

            if (answerInputs.length < 2) {
                e.preventDefault();
                if (answersError) {
                    answersError.textContent = 'Voeg minimaal 2 antwoorden toe.';
                    answersError.classList.remove('hidden');
                }
                valid = false;
            } else if (answerInputs.some(a => !a.value.trim())) {
                e.preventDefault();
                if (answersError) {
                    answersError.textContent = 'Alle antwoorden moeten ingevuld zijn.';
                    answersError.classList.remove('hidden');
                }
                valid = false;
            } else {
                answersError?.classList.add('hidden');
            }
        }

        if (!valid) return;

        // Collect answers into hidden field
        const answers = Array.from(document.querySelectorAll<HTMLInputElement>('.answer-input'))
            .map(i => i.value.trim())
            .filter(v => v);
        const raw = document.getElementById('AnswerChoicesRaw') as HTMLInputElement | null;
        if (raw) raw.value = answers.join('\n');
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
});
