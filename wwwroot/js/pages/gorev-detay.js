(() => {
    const copyButton = document.getElementById('copyTaskLink');
    const copyStatus = document.getElementById('copyTaskLinkStatus');
    copyButton?.addEventListener('click', async () => {
        try {
            await navigator.clipboard.writeText(window.location.href);
            copyStatus?.classList.remove('hidden');
            window.setTimeout(() => copyStatus?.classList.add('hidden'), 2200);
        } catch {
            window.prompt('Görev bağlantısını kopyalayın:', window.location.href);
        }
    });

    const form = document.getElementById('commentForm');
    const textarea = document.getElementById('commentContent');
    const counter = document.getElementById('commentCounter');
    const submitButton = document.getElementById('commentSubmitButton');
    const updateCounter = () => {
        if (!textarea || !counter) return;
        counter.textContent = `${textarea.value.length} / 2000`;
        counter.classList.toggle('text-amber-600', textarea.value.length >= 1800);
    };
    textarea?.addEventListener('input', updateCounter);
    updateCounter();
    form?.addEventListener('submit', event => {
        if (!form.checkValidity()) {
            event.preventDefault();
            form.querySelector(':invalid')?.focus();
            return;
        }
        if (!submitButton) return;
        submitButton.disabled = true;
        const text = submitButton.querySelector('[data-button-text]');
        if (text) text.textContent = 'Ekleniyor...';
    });
})();
