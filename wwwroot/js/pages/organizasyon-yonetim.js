(() => {
    const page = document.getElementById('organizationManagementPage');
    if (!page) return;

    const tabs = [...page.querySelectorAll('[data-management-tab]')];
    const panels = [...page.querySelectorAll('[data-management-panel]')];
    const storageKey = 'yonetim-paneli-organization-tab';

    function normalizeTab(value) {
        return value === 'unvan' ? 'unvan' : 'departman';
    }

    function storedTab() {
        try {
            return normalizeTab(localStorage.getItem(storageKey));
        } catch {
            return 'departman';
        }
    }

    function initialTab() {
        const serverTab = page.dataset.initialTab;
        if (serverTab === 'departman' || serverTab === 'unvan') return serverTab;
        const hashTab = window.location.hash.replace('#', '');
        if (hashTab === 'departman' || hashTab === 'unvan') return hashTab;
        return storedTab();
    }

    function activateTab(tabName, { focus = false, persist = true } = {}) {
        const normalized = normalizeTab(tabName);

        tabs.forEach(tab => {
            const active = tab.dataset.managementTab === normalized;
            tab.setAttribute('aria-selected', String(active));
            tab.setAttribute('tabindex', active ? '0' : '-1');
            tab.classList.toggle('is-active', active);
            if (active && focus) tab.focus();
        });

        panels.forEach(panel => {
            const active = panel.dataset.managementPanel === normalized;
            panel.hidden = !active;
            panel.classList.toggle('is-active', active);
        });

        if (persist) {
            try {
                localStorage.setItem(storageKey, normalized);
            } catch {
                // Depolama engeli sekme kullanımını etkilemez.
            }

            const nextUrl = `${window.location.pathname}${window.location.search}#${normalized}`;
            window.history.replaceState(null, '', nextUrl);
        }
    }

    tabs.forEach((tab, index) => {
        tab.addEventListener('click', () => activateTab(tab.dataset.managementTab));
        tab.addEventListener('keydown', event => {
            if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
            event.preventDefault();

            let nextIndex = index;
            if (event.key === 'ArrowRight') nextIndex = (index + 1) % tabs.length;
            if (event.key === 'ArrowLeft') nextIndex = (index - 1 + tabs.length) % tabs.length;
            if (event.key === 'Home') nextIndex = 0;
            if (event.key === 'End') nextIndex = tabs.length - 1;
            activateTab(tabs[nextIndex].dataset.managementTab, { focus: true });
        });
    });

    activateTab(initialTab(), { persist: false });

    function setCreatePanel(panelId, open, trigger = null) {
        const panel = document.getElementById(panelId);
        if (!panel) return;

        panel.hidden = !open;
        page.querySelectorAll(`[data-create-toggle="${panelId}"]`).forEach(button => {
            button.setAttribute('aria-expanded', String(open));
        });

        if (open) {
            window.setTimeout(() => panel.querySelector('input, select, textarea')?.focus(), 50);
            panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        } else if (trigger instanceof HTMLElement) {
            trigger.focus();
        }
    }

    page.querySelectorAll('[data-create-toggle]').forEach(button => {
        button.addEventListener('click', () => {
            const panelId = button.getAttribute('data-create-toggle');
            const panel = panelId ? document.getElementById(panelId) : null;
            if (!panelId || !panel) return;
            setCreatePanel(panelId, panel.hidden, button);
        });
    });

    page.querySelectorAll('[data-create-close]').forEach(button => {
        button.addEventListener('click', () => {
            const panelId = button.getAttribute('data-create-close');
            if (!panelId) return;
            const trigger = page.querySelector(`[data-create-toggle="${panelId}"]`);
            setCreatePanel(panelId, false, trigger);
        });
    });

    function bindSearch(inputId, listId) {
        const input = document.getElementById(inputId);
        const list = document.getElementById(listId);
        const empty = page.querySelector(`[data-search-empty="${listId}"]`);
        if (!input || !list) return;

        function applySearch() {
            const query = input.value.toLocaleLowerCase('tr-TR').trim();
            let visibleCount = 0;

            list.querySelectorAll('.management-item').forEach(item => {
                const text = (item.dataset.managementSearch || '').toLocaleLowerCase('tr-TR');
                const visible = query.length === 0 || text.includes(query);
                item.hidden = !visible;
                if (visible) visibleCount += 1;
            });

            if (empty) empty.hidden = visibleCount > 0;
        }

        input.addEventListener('input', applySearch);
        input.addEventListener('search', applySearch);
    }

    bindSearch('departmentManagementSearch', 'departmentManagementList');
    bindSearch('titleManagementSearch', 'titleManagementList');

    // Aynı listede yalnızca bir düzenleme alanı açık kalsın.
    page.addEventListener('toggle', event => {
        const opened = event.target;
        if (!(opened instanceof HTMLDetailsElement) || !opened.open || !opened.classList.contains('organization-record')) return;
        const list = opened.closest('.organization-record-list');
        list?.querySelectorAll('.organization-record[open]').forEach(record => {
            if (record !== opened) record.removeAttribute('open');
        });
    }, true);

    page.querySelectorAll('form[data-submit-lock]').forEach(form => {
        form.addEventListener('submit', () => {
            if (window.jQuery && !window.jQuery(form).valid()) return;
            const button = form.querySelector('button[type="submit"]');
            if (!(button instanceof HTMLButtonElement)) return;
            button.disabled = true;
            button.classList.add('opacity-70', 'cursor-not-allowed');
            const text = button.querySelector('[data-submit-text]');
            if (text) text.textContent = 'Kaydediliyor...';
        });
    });

    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape') return;
        const openPanel = [...page.querySelectorAll('.organization-create-panel:not([hidden])')].at(-1);
        if (!openPanel) return;
        const trigger = page.querySelector(`[data-create-toggle="${openPanel.id}"]`);
        setCreatePanel(openPanel.id, false, trigger);
    });
})();
