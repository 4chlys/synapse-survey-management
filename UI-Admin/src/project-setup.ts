import { toast, bindDirtyTracking } from './lib/notify';

function compressImage(file: File, maxWidth: number, quality: number): Promise<Blob> {
    return new Promise((resolve) => {
        const img = new Image();
        const url = URL.createObjectURL(file);
        img.onload = () => {
            URL.revokeObjectURL(url);
            const scale = Math.min(1, maxWidth / img.width);
            const canvas = document.createElement('canvas');
            canvas.width = img.width * scale;
            canvas.height = img.height * scale;
            canvas.getContext('2d')!.drawImage(img, 0, 0, canvas.width, canvas.height);
            canvas.toBlob((blob) => resolve(blob!), 'image/jpeg', quality);
        };
        img.src = url;
    });
}

document.addEventListener('DOMContentLoaded', () => {
    const mainForm = document.querySelector<HTMLFormElement>('form[method="post"]');
    if (mainForm) {
        bindDirtyTracking(mainForm);
        if (new URLSearchParams(window.location.search).get('saved') === '1')
            toast('Opgeslagen', 'success');
    }

    const btnChat = document.getElementById('btn-chat') as HTMLButtonElement | null;
    const btnScroll = document.getElementById('btn-scroll') as HTMLButtonElement | null;
    const interactionModeInput = document.getElementById('InteractionMode') as HTMLInputElement | null;
    const btnOff = document.getElementById('btn-off') as HTMLButtonElement | null;
    const btnOn = document.getElementById('btn-on') as HTMLButtonElement | null;
    const contactOptInInput = document.getElementById('ContactOptIn') as HTMLInputElement | null;
    const addSectionBtn = document.getElementById('add-section-btn') as HTMLButtonElement | null;
    const sectionsList = document.getElementById('sections-list') as HTMLDivElement | null;
    const browseBtn = document.getElementById('browse-btn') as HTMLButtonElement | null;
    const fileInput = document.getElementById('image-file-input') as HTMLInputElement | null;
    const imagePreview = document.getElementById('image-preview') as HTMLImageElement | null;
    const imagePlaceholder = document.getElementById('image-placeholder') as SVGElement | null;
    const imageUrlInput = document.getElementById('ImageUrl') as HTMLInputElement | null;

    // Open file picker when Bladeren is clicked
    browseBtn?.addEventListener('click', () => fileInput?.click());

    // Upload selected file to server
    fileInput?.addEventListener('change', async () => {
        const file = fileInput.files?.[0];
        if (!file) return;

        const compressed = await compressImage(file, 1200, 0.8);

        const formData = new FormData();
        formData.append('file', compressed, 'image.jpg');

        const response = await fetch('/Project/UploadImage', { method: 'POST', body: formData });
        if (!response.ok) return;

        const data = await response.json() as { url: string };
        if (imageUrlInput) imageUrlInput.value = data.url;
        if (imagePreview) {
            imagePreview.src = data.url;
            imagePreview.classList.remove('hidden');
        }
        imagePlaceholder?.classList.add('hidden');
    });

    // Pre-fill image preview on page load if a URL is already stored
    if (imageUrlInput?.value) {
        if (imagePreview) {
            imagePreview.src = imageUrlInput.value;
            imagePreview.classList.remove('hidden');
        }
        imagePlaceholder?.classList.add('hidden');
    }

    // Event delegation for removing pre-rendered section rows
    sectionsList?.addEventListener('click', (e) => {
        const btn = (e.target as HTMLElement).closest('[data-remove-section]');
        if (btn) (btn as HTMLElement).closest('div.flex.items-center')?.remove();
    });

    function setMode(mode: string) {
        if (!interactionModeInput || !btnChat || !btnScroll) return;
        interactionModeInput.value = mode;
        btnChat.classList.remove('bg-white', 'text-black', 'text-white');
        btnScroll.classList.remove('bg-white', 'text-black', 'text-white');
        if (mode === 'Chat') {
            btnChat.classList.add('bg-white', 'text-black');
            btnScroll.classList.add('text-white');
        } else {
            btnScroll.classList.add('bg-white', 'text-black');
            btnChat.classList.add('text-white');
        }
    }

    function setContactOptIn(value: boolean) {
        if (!contactOptInInput || !btnOn || !btnOff) return;
        contactOptInInput.value = String(value);
        btnOn.classList.remove('bg-yellow-400', 'text-black', 'text-white');
        btnOff.classList.remove('bg-white', 'text-black', 'text-white');
        btnOn.classList.add('text-white');
        btnOff.classList.add('text-white');
        if (value) {
            btnOn.classList.add('bg-yellow-400', 'text-black');
            btnOn.classList.remove('text-white');
        } else {
            btnOff.classList.add('bg-white', 'text-black');
            btnOff.classList.remove('text-white');
        }
    }

    btnChat?.addEventListener('click', () => setMode('Chat'));
    btnScroll?.addEventListener('click', () => setMode('Scroll'));
    if (interactionModeInput) setMode(interactionModeInput.dataset.initial ?? 'Scroll');

    btnOff?.addEventListener('click', () => setContactOptIn(false));
    btnOn?.addEventListener('click', () => setContactOptIn(true));
    if (contactOptInInput) setContactOptIn(contactOptInInput.dataset.initial === 'true');

    let sectionIndex = parseInt(sectionsList?.dataset.initialCount ?? '0', 10);
    addSectionBtn?.addEventListener('click', () => {
        if (!sectionsList) return;
        const div = document.createElement('div');
        div.className = 'flex items-center gap-1';
        const idx = sectionIndex++;
        const input = document.createElement('input');
        input.name = `Sections[${idx}]`;
        input.type = 'text';
        input.placeholder = 'Section name';
        input.className = 'flex-1 bg-gray-900 border border-gray-500 text-white text-xs px-3 py-1 focus:outline-none';
        const removeBtn = document.createElement('button');
        removeBtn.type = 'button';
        removeBtn.className = 'text-gray-500 hover:text-white text-xs px-1';
        removeBtn.textContent = '✕';
        removeBtn.addEventListener('click', () => div.remove());
        div.appendChild(input);
        div.appendChild(removeBtn);
        sectionsList.appendChild(div);
    });
});
