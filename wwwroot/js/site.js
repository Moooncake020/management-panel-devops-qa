(() => {
    // Küçük ortak davranışlar sayfa içi inline JavaScript yerine burada tutulur.
    document.querySelectorAll('[data-auto-submit]').forEach(element => {
        element.addEventListener('change', () => element.form?.requestSubmit());
    });

    document.querySelectorAll('[data-history-back]').forEach(element => {
        element.addEventListener('click', () => window.history.back());
    });

    document.querySelectorAll('[data-dismiss-flash]').forEach(button => {
        button.addEventListener('click', () => {
            const message = button.closest('[data-flash-message]');
            if (!(message instanceof HTMLElement)) return;

            message.classList.add('is-closing');
            window.setTimeout(() => message.remove(), 160);
        });
    });

    const dialog = document.getElementById('appConfirmDialog');
    const dialogTitle = document.getElementById('appConfirmTitle');
    const dialogMessage = document.getElementById('appConfirmMessage');
    const cancelButton = document.getElementById('appConfirmCancel');
    const acceptButton = document.getElementById('appConfirmAccept');

    let pendingForm = null;
    let pendingElement = null;
    let returnFocusElement = null;

    function resetPendingAction() {
        pendingForm = null;
        pendingElement = null;
    }

    function closeConfirmDialog() {
        if (dialog?.open) dialog.close();
        resetPendingAction();
        returnFocusElement?.focus();
        returnFocusElement = null;
    }

    function openConfirmDialog(source, form = null) {
        const message = source.getAttribute('data-confirm');
        if (!message) return false;

        // <dialog> desteği olmayan çok eski tarayıcılarda güvenli geri dönüş.
        if (!dialog || typeof dialog.showModal !== 'function') {
            return window.confirm(message);
        }

        pendingForm = form;
        pendingElement = form ? null : source;
        returnFocusElement = document.activeElement instanceof HTMLElement ? document.activeElement : null;

        const title = source.getAttribute('data-confirm-title') || 'İşlemi onaylayın';
        const action = source.getAttribute('data-confirm-action') || 'Onayla';
        const tone = source.getAttribute('data-confirm-tone') || 'danger';

        if (dialogTitle) dialogTitle.textContent = title;
        if (dialogMessage) dialogMessage.textContent = message;
        if (acceptButton) {
            acceptButton.textContent = action;
            acceptButton.classList.toggle('app-button--danger', tone === 'danger');
            acceptButton.classList.toggle('app-button--primary', tone !== 'danger');
        }
        dialog.dataset.tone = tone;
        dialog.showModal();
        acceptButton?.focus();
        return false;
    }

    document.addEventListener('submit', event => {
        const form = event.target instanceof HTMLFormElement ? event.target : null;
        if (!form || !form.hasAttribute('data-confirm') || form.dataset.confirmed === 'true') return;

        event.preventDefault();
        openConfirmDialog(form, form);
    });

    document.addEventListener('click', event => {
        const target = event.target instanceof Element ? event.target.closest('[data-confirm]') : null;
        if (!target || target instanceof HTMLFormElement || target.closest('form[data-confirm]')) return;

        event.preventDefault();
        openConfirmDialog(target);
    });

    cancelButton?.addEventListener('click', closeConfirmDialog);

    acceptButton?.addEventListener('click', () => {
        if (pendingForm) {
            const form = pendingForm;
            form.dataset.confirmed = 'true';
            dialog?.close();
            form.requestSubmit();
            return;
        }

        if (pendingElement instanceof HTMLAnchorElement && pendingElement.href) {
            window.location.assign(pendingElement.href);
            return;
        }

        pendingElement?.click();
        closeConfirmDialog();
    });

    dialog?.addEventListener('cancel', event => {
        event.preventDefault();
        closeConfirmDialog();
    });

    dialog?.addEventListener('click', event => {
        if (event.target === dialog) closeConfirmDialog();
    });


    // Açılır işlem menülerinde yalnızca bir panel açık kalır. Escape ve dışarı
    // tıklama davranışları klavye kullanıcıları için de tutarlı hale gelir.
    const actionMenus = Array.from(document.querySelectorAll("details.action-menu"));

    actionMenus.forEach((menu) => {
        const summary = menu.querySelector(":scope > summary");
        summary?.setAttribute("aria-expanded", String(menu.open));

        menu.addEventListener("toggle", () => {
            summary?.setAttribute("aria-expanded", String(menu.open));
            if (!menu.open) return;
            actionMenus.forEach((other) => {
                if (other !== menu) other.open = false;
            });
        });

        menu.addEventListener("keydown", (event) => {
            if (event.key !== "Escape" || !menu.open) return;
            event.preventDefault();
            menu.open = false;
            summary?.focus();
        });
    });

    document.addEventListener("click", (event) => {
        if (!(event.target instanceof Node)) return;
        actionMenus.forEach((menu) => {
            if (menu.open && !menu.contains(event.target)) menu.open = false;
        });
    });

    // Yatay taşan tablo, takvim ve organizasyon alanları klavyeyle odaklanıp
    // ok tuşlarıyla kaydırılabilir. Taşma yoksa gereksiz tab durağı oluşturmaz.
    const scrollRegions = Array.from(document.querySelectorAll(
        ".soft-scroll, .workload-table-wrap, .report-table-wrap, .calendar-desktop, .org-tree, [data-scroll-region]"
    ));

    const updateScrollRegion = (region) => {
        const overflows = region.scrollWidth > region.clientWidth + 2;
        region.classList.toggle("keyboard-scroll-region", overflows);

        if (overflows) {
            region.tabIndex = 0;
            region.setAttribute("role", "region");
            if (!region.hasAttribute("aria-label")) {
                region.setAttribute("aria-label", "Yatay kaydırılabilir içerik");
            }
        } else if (region.getAttribute("tabindex") === "0") {
            region.removeAttribute("tabindex");
        }
    };

    scrollRegions.forEach(updateScrollRegion);

    if ("ResizeObserver" in window) {
        const observer = new ResizeObserver((entries) => {
            entries.forEach((entry) => updateScrollRegion(entry.target));
        });
        scrollRegions.forEach((region) => observer.observe(region));
    }
})();
