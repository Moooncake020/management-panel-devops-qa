(() => {
    const page = document.getElementById('userPage');
    const drawer = document.getElementById('newUserDrawer');
    const openButtons = [
        document.getElementById('openNewUserDrawer'),
        ...document.querySelectorAll('[data-open-user-drawer]')
    ].filter(Boolean);
    const closeButtons = drawer?.querySelectorAll('[data-drawer-close]') || [];
    let returnFocusElement = null;

    function focusableElements() {
        if (!drawer) return [];
        return Array.from(drawer.querySelectorAll(
            'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
        ));
    }

    function openDrawer(event) {
        if (!drawer) return;
        returnFocusElement = event?.currentTarget instanceof HTMLElement
            ? event.currentTarget
            : document.activeElement;
        drawer.classList.add('is-open');
        drawer.setAttribute('aria-hidden', 'false');
        document.getElementById('openNewUserDrawer')?.setAttribute('aria-expanded', 'true');
        document.body.classList.add('drawer-open');
        window.setTimeout(() => document.getElementById('newUserFirstName')?.focus(), 180);
    }

    function closeDrawer() {
        if (!drawer) return;
        drawer.classList.remove('is-open');
        drawer.setAttribute('aria-hidden', 'true');
        document.getElementById('openNewUserDrawer')?.setAttribute('aria-expanded', 'false');
        document.body.classList.remove('drawer-open');
        if (returnFocusElement instanceof HTMLElement) returnFocusElement.focus();
    }

    openButtons.forEach(button => button.addEventListener('click', openDrawer));
    closeButtons.forEach(button => button.addEventListener('click', closeDrawer));

    if (page?.dataset.openDrawer === 'true') {
        window.setTimeout(() => openDrawer(), 50);
    }

    // Arama her zaman görünür, ikincil filtreler ihtiyaç halinde açılır.
    const filterToggle = document.getElementById('userFilterToggle');
    const filterPanel = document.getElementById('userAdvancedFilters');

    function setFilterPanel(open, moveFocus = false) {
        if (!filterPanel || !filterToggle) return;
        filterPanel.classList.toggle('is-open', open);
        filterPanel.setAttribute('aria-hidden', String(!open));
        filterToggle.setAttribute('aria-expanded', String(open));

        if (moveFocus) {
            if (open) filterPanel.querySelector('select, input, button, a')?.focus();
            else filterToggle.focus();
        }
    }

    filterToggle?.addEventListener('click', () => {
        const willOpen = !filterPanel?.classList.contains('is-open');
        setFilterPanel(Boolean(willOpen), Boolean(willOpen));
    });

    document.querySelectorAll('[data-user-filter-panel-close]').forEach(button => {
        button.addEventListener('click', () => setFilterPanel(false, true));
    });

    // Aktif filtre rozetinden yalnız ilgili query parametresini ve sayfa bilgisini kaldırır.
    document.querySelectorAll('[data-user-filter-clear]').forEach(button => {
        button.addEventListener('click', () => {
            const parameter = button.getAttribute('data-user-filter-clear');
            if (!parameter) return;

            const url = new URL(window.location.href);
            [...url.searchParams.keys()].forEach(key => {
                const normalizedKey = key.toLocaleLowerCase('tr-TR');
                if (normalizedKey === parameter.toLocaleLowerCase('tr-TR') || normalizedKey === 'sayfa') {
                    url.searchParams.delete(key);
                }
            });
            window.location.assign(url.toString());
        });
    });

    // Kart / tablo görünümü ekran genişliğine göre başlar ve kullanıcının tercihini saklar.
    const userResults = document.getElementById('userResults');
    const viewButtons = [...document.querySelectorAll('[data-user-view]')];
    const viewStorageKey = 'yonetim-paneli-user-view';
    const desktopQuery = window.matchMedia('(min-width: 1280px)');
    let savedView = null;

    try {
        const stored = localStorage.getItem(viewStorageKey);
        if (stored === 'cards' || stored === 'table') savedView = stored;
    } catch {
        savedView = null;
    }

    function preferredView() {
        return savedView || (desktopQuery.matches ? 'table' : 'cards');
    }

    function setUserView(view, persist = false) {
        if (!userResults || (view !== 'cards' && view !== 'table')) return;
        userResults.dataset.view = view;
        viewButtons.forEach(button => {
            const active = button.getAttribute('data-user-view') === view;
            button.classList.toggle('is-active', active);
            button.setAttribute('aria-pressed', String(active));
        });

        if (persist) {
            savedView = view;
            try {
                localStorage.setItem(viewStorageKey, view);
            } catch {
                // Depolama kapalıysa görünüm değiştirme yine çalışır.
            }
        }
    }

    viewButtons.forEach(button => {
        button.addEventListener('click', () => setUserView(button.getAttribute('data-user-view'), true));
    });

    setUserView(preferredView());
    desktopQuery.addEventListener?.('change', () => {
        if (!savedView) setUserView(preferredView());
    });

    // Drawer ve işlem menülerinin klavye davranışları.
    document.addEventListener('keydown', event => {
        if (drawer?.classList.contains('is-open')) {
            if (event.key === 'Escape') {
                closeDrawer();
                return;
            }

            if (event.key === 'Tab') {
                const elements = focusableElements();
                const first = elements[0];
                const last = elements[elements.length - 1];
                if (!first || !last) return;

                if (event.shiftKey && document.activeElement === first) {
                    event.preventDefault();
                    last.focus();
                } else if (!event.shiftKey && document.activeElement === last) {
                    event.preventDefault();
                    first.focus();
                }
            }
        } else if (event.key === 'Escape' && filterPanel?.classList.contains('is-open') && window.innerWidth < 768) {
            setFilterPanel(false, true);
        }
    });

    document.querySelectorAll('[data-password-toggle]').forEach(button => {
        button.addEventListener('click', () => {
            const input = document.getElementById(button.dataset.passwordToggle || '');
            if (!(input instanceof HTMLInputElement)) return;
            const show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            button.setAttribute('aria-label', show ? 'Şifreyi gizle' : 'Şifreyi göster');
        });
    });

    document.querySelectorAll('form[data-submit-lock]').forEach(form => {
        form.addEventListener('submit', () => {
            if (window.jQuery && !window.jQuery(form).valid()) return;
            const button = form.querySelector('button[type="submit"]');
            if (!(button instanceof HTMLButtonElement)) return;
            button.disabled = true;
            button.classList.add('cursor-not-allowed', 'opacity-70');
            const text = button.querySelector('[data-submit-text]');
            if (text) text.textContent = 'Kaydediliyor...';
        });
    });

    document.addEventListener('toggle', event => {
        const opened = event.target;
        if (!(opened instanceof HTMLDetailsElement) || !opened.open || !opened.classList.contains('action-menu')) return;
        document.querySelectorAll('.action-menu[open]').forEach(menu => {
            if (menu !== opened) menu.removeAttribute('open');
        });
    }, true);

    document.addEventListener('click', event => {
        if (event.target instanceof Element && event.target.closest('.action-menu')) return;
        document.querySelectorAll('.action-menu[open]').forEach(menu => menu.removeAttribute('open'));
    });
})();
