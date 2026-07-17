(() => {
    const form = document.querySelector('[data-task-form]');
    if (!form) return;

    const mode = form.dataset.taskForm;
    const submitButton = form.querySelector('[data-task-submit]');
    const dirtyIndicator = document.getElementById('taskDirtyIndicator');
    const titleInput = document.getElementById('Baslik');
    const descriptionInput = document.getElementById('Aciklama');
    const titleCounter = document.getElementById('titleCounter');
    const descriptionCounter = document.getElementById('descriptionCounter');
    const startDate = document.getElementById('BaslangicTarihi');
    const dueDate = document.getElementById('SonTarih');
    const dateWarning = document.getElementById('datePlanWarning');
    const fileInput = document.getElementById('ResimDosyasi');
    const fileInfo = document.getElementById('selectedFileInfo');
    const imagePreview = document.getElementById('imagePreview');
    const removeImageCheckbox = document.getElementById('MevcutResmiKaldir');
    const maxFileSize = 5 * 1024 * 1024;
    const allowedExtensions = ['jpg', 'jpeg', 'png', 'webp'];
    let submitted = false;
    let dirty = false;

    const updateCounter = (input, counter, maximum) => {
        if (!input || !counter) return;
        counter.textContent = `${input.value.length} / ${maximum}`;
        counter.classList.toggle('text-amber-600', input.value.length >= maximum * .9);
    };

    const markDirty = () => {
        dirty = true;
        dirtyIndicator?.classList.remove('hidden');
    };

    const validateDatePlan = () => {
        if (!dateWarning || !startDate || !dueDate) return;
        const invalid = Boolean(startDate.value && dueDate.value && dueDate.value < startDate.value);
        dateWarning.classList.toggle('hidden', !invalid);
        dueDate.setAttribute('aria-invalid', invalid ? 'true' : 'false');
    };

    const resetFilePreview = () => {
        if (fileInfo) {
            fileInfo.textContent = '';
            fileInfo.classList.add('hidden');
            fileInfo.classList.remove('text-red-600');
        }
        if (imagePreview) {
            imagePreview.removeAttribute('src');
            imagePreview.classList.add('hidden');
        }
    };

    titleInput?.addEventListener('input', () => updateCounter(titleInput, titleCounter, 120));
    descriptionInput?.addEventListener('input', () => updateCounter(descriptionInput, descriptionCounter, 2000));
    updateCounter(titleInput, titleCounter, 120);
    updateCounter(descriptionInput, descriptionCounter, 2000);

    startDate?.addEventListener('change', validateDatePlan);
    dueDate?.addEventListener('change', validateDatePlan);
    validateDatePlan();

    form.addEventListener('input', markDirty);
    form.addEventListener('change', markDirty);

    fileInput?.addEventListener('change', () => {
        resetFilePreview();
        const file = fileInput.files?.[0];
        if (!file) return;
        const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
        if (!allowedExtensions.includes(extension) || file.size > maxFileSize) {
            fileInput.value = '';
            if (fileInfo) {
                fileInfo.textContent = file.size > maxFileSize
                    ? 'Dosya 5 MB sınırını aşıyor.'
                    : 'Yalnızca JPG, JPEG, PNG veya WEBP seçebilirsiniz.';
                fileInfo.classList.remove('hidden');
                fileInfo.classList.add('text-red-600');
            }
            return;
        }
        if (removeImageCheckbox) removeImageCheckbox.checked = false;
        if (fileInfo) {
            fileInfo.classList.remove('hidden');
            fileInfo.textContent = `${file.name} · ${(file.size / 1024 / 1024).toFixed(2)} MB`;
        }
        const reader = new FileReader();
        reader.addEventListener('load', event => {
            if (!imagePreview || typeof event.target?.result !== 'string') return;
            imagePreview.src = event.target.result;
            imagePreview.classList.remove('hidden');
        });
        reader.readAsDataURL(file);
    });

    form.addEventListener('submit', event => {
        validateDatePlan();
        if (!form.checkValidity()) {
            event.preventDefault();
            form.querySelector(':invalid')?.focus();
            return;
        }
        submitted = true;
        dirty = false;
        if (submitButton) {
            submitButton.disabled = true;
            const text = submitButton.querySelector('[data-button-text]');
            if (text) text.textContent = mode === 'create' ? 'Görev oluşturuluyor...' : 'Değişiklikler kaydediliyor...';
        }
    });

    window.addEventListener('beforeunload', event => {
        if (!dirty || submitted) return;
        event.preventDefault();
        event.returnValue = '';
    });
})();
