(() => {
    const root = document.getElementById('userEditPage');
    const form = root?.querySelector('[data-user-edit-form]');
    if (!(form instanceof HTMLFormElement)) return;

    const saveState = root.querySelector('[data-save-state]');
    const submitButton = form.querySelector('button[type="submit"]');
    const submitText = form.querySelector('[data-submit-text]');
    const sectionLinks = Array.from(root.querySelectorAll('[data-section-link]'));
    const sections = Array.from(root.querySelectorAll('[data-edit-section]'));
    let dirty = false;
    let submitting = false;

    const setDirty = () => {
        if (dirty) return;
        dirty = true;
        if (saveState) saveState.textContent = 'Kaydedilmemiş değişiklikler var';
        saveState?.parentElement?.classList.add('is-dirty');
    };

    form.addEventListener('input', setDirty);
    form.addEventListener('change', setDirty);

    root.querySelectorAll('[data-password-toggle]').forEach((button) => {
        button.addEventListener('click', () => {
            const id = button.dataset.passwordToggle;
            const input = id ? document.getElementById(id) : null;
            if (!(input instanceof HTMLInputElement)) return;
            const show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            button.setAttribute('aria-label', show ? 'Şifreyi gizle' : 'Şifreyi göster');
        });
    });

    form.addEventListener('submit', (event) => {
        if (window.jQuery && !window.jQuery(form).valid()) {
            event.preventDefault();
            const firstError = form.querySelector('.input-validation-error');
            if (firstError instanceof HTMLElement) firstError.focus();
            return;
        }
        submitting = true;
        dirty = false;
        if (submitButton instanceof HTMLButtonElement) submitButton.disabled = true;
        if (submitText) submitText.textContent = 'Kaydediliyor...';
        if (saveState) saveState.textContent = 'Değişiklikler kaydediliyor';
    });

    window.addEventListener('beforeunload', (event) => {
        if (!dirty || submitting) return;
        event.preventDefault();
        event.returnValue = '';
    });

    sectionLinks.forEach((link) => {
        link.addEventListener('click', () => {
            sectionLinks.forEach((item) => item.classList.toggle('is-active', item === link));
        });
    });

    if ('IntersectionObserver' in window && sections.length) {
        const observer = new IntersectionObserver((entries) => {
            const visible = entries
                .filter((entry) => entry.isIntersecting)
                .sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];
            if (!visible) return;
            sectionLinks.forEach((link) => {
                link.classList.toggle('is-active', link.dataset.sectionLink === visible.target.id);
            });
        }, { rootMargin: '-20% 0px -65% 0px', threshold: [0.05, 0.25, 0.5] });
        sections.forEach((section) => observer.observe(section));
    }
})();
