(() => {
    const imageModal = document.getElementById('taskImageModal');
    const modalImage = document.getElementById('taskImageModalImage');
    const modalTitle = document.getElementById('taskImageModalTitle');
    const imageModalCloseButton = document.getElementById('taskImageModalClose');
    let imageReturnFocusElement = null;

    function openImageModal(trigger) {
        if (!imageModal || !modalImage) return;
        imageReturnFocusElement = trigger;
        const imageSource = trigger.dataset.imageSrc;
        if (!imageSource) return;
        modalImage.src = imageSource;
        modalImage.alt = trigger.dataset.imageAlt || 'Görev görseli';
        if (modalTitle) modalTitle.textContent = modalImage.alt;
        imageModal.classList.remove('hidden');
        imageModal.classList.add('flex');
        imageModal.setAttribute('aria-hidden', 'false');
        document.body.classList.add('overflow-hidden');
        imageModalCloseButton?.focus();
    }

    function closeImageModal() {
        if (!imageModal || imageModal.classList.contains('hidden')) return;
        imageModal.classList.add('hidden');
        imageModal.classList.remove('flex');
        imageModal.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('overflow-hidden');
        if (modalImage) {
            modalImage.removeAttribute('src');
            modalImage.alt = '';
        }
        imageReturnFocusElement?.focus();
    }

    document.querySelectorAll('.task-image-trigger').forEach(trigger => {
        trigger.addEventListener('click', () => openImageModal(trigger));
    });
    imageModalCloseButton?.addEventListener('click', closeImageModal);
    imageModal?.addEventListener('click', event => {
        if (event.target === imageModal) closeImageModal();
    });

    // Gelişmiş filtre paneli: aramayı her zaman görünür tutar, ikincil filtreleri gerektiğinde açar.
    const filterToggle = document.getElementById('taskFilterToggle');
    const filterPanel = document.getElementById('taskAdvancedFilters');

    function setFilterPanel(open, focusFirstField = false) {
        if (!filterPanel || !filterToggle) return;
        filterPanel.classList.toggle('is-open', open);
        filterPanel.setAttribute('aria-hidden', String(!open));
        filterToggle.setAttribute('aria-expanded', String(open));

        if (open && focusFirstField) {
            filterPanel.querySelector('select, input, button')?.focus();
        } else if (!open && focusFirstField) {
            filterToggle.focus();
        }
    }

    filterToggle?.addEventListener('click', () => {
        const willOpen = !filterPanel?.classList.contains('is-open');
        setFilterPanel(Boolean(willOpen), Boolean(willOpen));
    });

    document.querySelectorAll('[data-filter-panel-close]').forEach(button => {
        button.addEventListener('click', () => setFilterPanel(false, true));
    });

    // Aktif filtre rozetlerinden tek filtreyi kaldırır ve sayfayı ilk sonuca döndürür.
    document.querySelectorAll('[data-filter-clear]').forEach(button => {
        button.addEventListener('click', () => {
            const parameter = button.getAttribute('data-filter-clear');
            if (!parameter) return;

            const url = new URL(window.location.href);
            [...url.searchParams.keys()].forEach(key => {
                if (key.toLocaleLowerCase('tr-TR') === parameter.toLocaleLowerCase('tr-TR') ||
                    key.toLocaleLowerCase('tr-TR') === 'sayfa') {
                    url.searchParams.delete(key);
                }
            });
            window.location.assign(url.toString());
        });
    });

    // Kart / tablo görünümü. Tercih tarayıcıda saklanır; ilk kullanımda ekran genişliğine uygun görünüm seçilir.
    const taskResults = document.getElementById('taskResults');
    const viewButtons = [...document.querySelectorAll('[data-task-view]')];
    const viewStorageKey = 'yonetim-paneli-task-view';
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

    function setTaskView(view, persist = false) {
        if (!taskResults || (view !== 'cards' && view !== 'table')) return;
        taskResults.dataset.view = view;
        viewButtons.forEach(button => {
            const active = button.getAttribute('data-task-view') === view;
            button.classList.toggle('is-active', active);
            button.setAttribute('aria-pressed', String(active));
        });

        if (persist) {
            savedView = view;
            try {
                localStorage.setItem(viewStorageKey, view);
            } catch {
                // Depolama engeli görünüm değiştirmeyi etkilemez.
            }
        }
    }

    viewButtons.forEach(button => {
        button.addEventListener('click', () => {
            setTaskView(button.getAttribute('data-task-view'), true);
        });
    });

    setTaskView(preferredView());
    desktopQuery.addEventListener?.('change', () => {
        if (!savedView) setTaskView(preferredView());
    });

    // Aynı anda yalnızca bir satır/kart işlem menüsü açık kalır.
    document.addEventListener('toggle', event => {
        const openedMenu = event.target;
        if (!(openedMenu instanceof HTMLDetailsElement) || !openedMenu.open || !openedMenu.classList.contains('action-menu')) return;
        document.querySelectorAll('.action-menu[open]').forEach(menu => {
            if (menu !== openedMenu) menu.removeAttribute('open');
        });
    }, true);

    document.addEventListener('click', event => {
        if (event.target instanceof Element && event.target.closest('.action-menu')) return;
        document.querySelectorAll('.action-menu[open]').forEach(menu => menu.removeAttribute('open'));
    });

    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape') return;
        closeImageModal();
        if (filterPanel?.classList.contains('is-open') && window.innerWidth < 768) {
            setFilterPanel(false, true);
        }
    });
})();
