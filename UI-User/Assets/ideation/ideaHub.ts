export function initIdeation(): void {
    // Make functions globally available for onclick handlers
    (window as any).showCreateForm = showCreateForm;
    (window as any).hideCreateForm = hideCreateForm;
    (window as any).showEmojiPicker = showEmojiPicker;
    (window as any).addEmoji = addEmoji;
}

function showCreateForm(): void {
    const form = document.getElementById('createForm');
    if (form) {
        form.classList.remove('hidden');
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }
}

function hideCreateForm(): void {
    const form = document.getElementById('createForm');
    if (form) {
        form.classList.add('hidden');
    }
}

function showEmojiPicker(ideaId: number): void {
    const picker = document.getElementById(`picker-${ideaId}`);
    if (picker) {
        picker.classList.toggle('hidden');
    }
}

function addEmoji(ideaId: number, emoji: string): void {
    const emojisContainer = document.getElementById(`emojis-${ideaId}`);
    if (!emojisContainer) return;

    // Check if emoji already exists
    const existingEmojis = emojisContainer.querySelectorAll('.emoji-item');
    let found = false;

    existingEmojis.forEach((item) => {
        const emojiSymbol = item.querySelector('.emoji-symbol');
        if (emojiSymbol && emojiSymbol.textContent === emoji) {
            // Increase count
            const countSpan = item.querySelector('.emoji-count');
            if (countSpan && countSpan.textContent) {
                const currentCount = parseInt(countSpan.textContent);
                countSpan.textContent = (currentCount + 1).toString();
                found = true;
            }
        }
    });

    // If emoji doesn't exist yet, add it
    if (!found) {
        const emojiElement = document.createElement('button');
        emojiElement.className = 'emoji-item inline-flex items-center justify-center gap-1.5 px-3 py-1.5 bg-gray-100 border border-gray-200 rounded-full text-xs min-w-[3rem] leading-none';
        emojiElement.innerHTML = `
            <span class="emoji-symbol text-lg leading-none">${emoji}</span>
            <span class="emoji-count font-semibold text-gray-700 text-sm leading-none">1</span>
        `;
        emojisContainer.appendChild(emojiElement);
    }

    // Close picker
    const picker = document.getElementById(`picker-${ideaId}`);
    if (picker) {
        picker.classList.add('hidden');
    }
}