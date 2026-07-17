(() => {
    const dialog = document.getElementById('globalSearchDialog');
    const openButton = document.getElementById('globalSearchOpen');
    const closeButton = document.getElementById('globalSearchClose');
    const input = document.getElementById('globalSearchInput');
    const results = document.getElementById('globalSearchResults');
    const commands = document.getElementById('globalSearchCommands');
    const status = document.getElementById('globalSearchStatus');

    if (!(dialog instanceof HTMLDialogElement) ||
        !(input instanceof HTMLInputElement) ||
        !(results instanceof HTMLElement) ||
        !(commands instanceof HTMLElement)) {
        return;
    }

    const endpoint = dialog.dataset.searchUrl;
    const commandItems = Array.from(commands.querySelectorAll('[data-command-item]'));
    let debounceTimer = 0;
    let activeRequest = null;
    let selectedIndex = -1;
    let returnFocusElement = null;
    let navigationSequence = '';
    let navigationTimer = 0;

    const normalize = value => (value || '')
        .toLocaleLowerCase('tr-TR')
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '');

    function openDialog() {
        returnFocusElement = document.activeElement instanceof HTMLElement
            ? document.activeElement
            : openButton;

        if (!dialog.open) {
            dialog.showModal();
        }

        document.body.classList.add('global-search-open');
        window.setTimeout(() => {
            input.focus();
            input.select();
        }, 0);
        resetSelection();
    }

    function closeDialog() {
        activeRequest?.abort();
        activeRequest = null;
        window.clearTimeout(debounceTimer);
        if (dialog.open) dialog.close();
        document.body.classList.remove('global-search-open');
        input.value = '';
        showCommands('');
        clearResults();
        setStatus('');
        returnFocusElement?.focus();
        returnFocusElement = null;
    }

    function setStatus(message, tone = '') {
        if (!status) return;
        status.textContent = message;
        status.dataset.tone = tone;
        status.hidden = !message;
    }

    function clearResults() {
        results.replaceChildren();
        results.hidden = true;
    }

    function showCommands(query) {
        commands.hidden = false;
        clearResults();

        const normalizedQuery = normalize(query);
        let visibleCount = 0;

        commandItems.forEach(item => {
            const text = `${item.textContent || ''} ${item.getAttribute('data-command-keywords') || ''}`;
            const visible = !normalizedQuery || normalize(text).includes(normalizedQuery);
            item.hidden = !visible;
            if (visible) visibleCount += 1;
        });

        setStatus(
            normalizedQuery && visibleCount === 0
                ? 'Bu ifadeyle eşleşen hızlı komut bulunamadı. Arama için en az 2 karakter yazın.'
                : '',
            visibleCount === 0 ? 'empty' : '');
        resetSelection();
    }

    function createIcon(type) {
        const icon = document.createElement('span');
        icon.className = `global-search-result__icon is-${type}`;
        icon.setAttribute('aria-hidden', 'true');

        const svgNamespace = 'http://www.w3.org/2000/svg';
        const svg = document.createElementNS(svgNamespace, 'svg');
        svg.setAttribute('viewBox', '0 0 24 24');
        svg.setAttribute('fill', 'none');
        svg.setAttribute('stroke', 'currentColor');
        svg.setAttribute('stroke-width', '2');
        svg.setAttribute('stroke-linecap', 'round');
        svg.setAttribute('stroke-linejoin', 'round');

        const path = document.createElementNS(svgNamespace, 'path');
        path.setAttribute(
            'd',
            type === 'gorev'
                ? 'M9 5H7a2 2 0 0 0-2 2v12h14V7a2 2 0 0 0-2-2h-2M9 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2m-6 9 2 2 4-4'
                : 'M15 11a3 3 0 1 1-6 0 3 3 0 0 1 6 0ZM5 20a7 7 0 0 1 14 0'
        );
        svg.append(path);
        icon.append(svg);
        return icon;
    }

    function createResultItem(item) {
        const link = document.createElement('a');
        link.href = item.url;
        link.className = 'global-search-result';
        link.setAttribute('role', 'option');
        link.dataset.searchSelectable = 'true';

        link.append(createIcon(item.tur));

        const content = document.createElement('span');
        content.className = 'global-search-result__content';

        const titleRow = document.createElement('span');
        titleRow.className = 'global-search-result__title';
        const title = document.createElement('strong');
        title.textContent = item.baslik;
        titleRow.append(title);

        if (item.rozet) {
            const badge = document.createElement('small');
            badge.textContent = item.rozet;
            titleRow.append(badge);
        }

        const description = document.createElement('span');
        description.className = 'global-search-result__description';
        description.textContent = item.aciklama;

        content.append(titleRow, description);

        const arrow = document.createElement('span');
        arrow.className = 'global-search-result__arrow';
        arrow.setAttribute('aria-hidden', 'true');
        arrow.textContent = '→';

        link.append(content, arrow);
        return link;
    }

    function appendResultGroup(title, items) {
        if (!Array.isArray(items) || items.length === 0) return;

        const section = document.createElement('section');
        section.className = 'global-search-result-group';

        const heading = document.createElement('div');
        heading.className = 'global-search-section-title';
        const headingText = document.createElement('span');
        headingText.textContent = title;
        const count = document.createElement('small');
        count.textContent = `${items.length} sonuç`;
        heading.append(headingText, count);

        const list = document.createElement('div');
        list.className = 'global-search-result-list';
        items.forEach(item => list.append(createResultItem(item)));

        section.append(heading, list);
        results.append(section);
    }

    function renderResults(payload) {
        results.replaceChildren();
        commands.hidden = true;
        results.hidden = false;

        appendResultGroup('Görevler', payload.gorevler);
        appendResultGroup('Çalışanlar', payload.kullanicilar);

        const total = Number(payload.toplamSonuc || 0);
        if (total === 0) {
            const empty = document.createElement('div');
            empty.className = 'global-search-empty';
            const title = document.createElement('strong');
            title.textContent = 'Sonuç bulunamadı';
            const description = document.createElement('span');
            description.textContent = 'Farklı bir başlık, çalışan adı, e-posta veya görev numarası deneyin.';
            empty.append(title, description);
            results.append(empty);
            setStatus(`“${payload.sorgu}” için sonuç bulunamadı.`, 'empty');
        } else {
            setStatus(`${total} sonuç bulundu.`, 'success');
        }

        resetSelection();
    }

    async function search(query) {
        if (!endpoint) return;

        activeRequest?.abort();
        const request = new AbortController();
        activeRequest = request;
        setStatus('Aranıyor…', 'loading');

        try {
            const url = new URL(endpoint, window.location.origin);
            url.searchParams.set('q', query);

            const response = await fetch(url, {
                headers: { 'Accept': 'application/json' },
                signal: request.signal
            });

            let payload = {};
            try {
                payload = await response.json();
            } catch {
                payload = {};
            }

            if (response.status === 401) {
                const girisUrl = typeof payload.girisUrl === "string"
                    ? payload.girisUrl
                    : "/Auth/Login";
                setStatus(payload.mesaj || "Oturumunuz sona erdi. Giriş sayfasına yönlendiriliyorsunuz.", "error");
                window.location.assign(girisUrl);
                return;
            }

            if (!response.ok) {
                throw new Error(payload.mesaj || "Arama isteği tamamlanamadı.");
            }

            if (input.value.trim() !== query) return;
            renderResults(payload);
        } catch (error) {
            if (error.name === 'AbortError') return;
            commands.hidden = true;
            clearResults();
            setStatus(
                error instanceof Error && error.message
                    ? error.message
                    : "Arama şu anda tamamlanamadı. Sayfayı yenileyip tekrar deneyin.",
                "error"
            );
        } finally {
            if (activeRequest === request) {
                activeRequest = null;
            }
        }
    }

    function handleInput() {
        const query = input.value.trim();
        window.clearTimeout(debounceTimer);
        activeRequest?.abort();

        if (query.length < 2) {
            showCommands(query);
            return;
        }

        debounceTimer = window.setTimeout(() => search(query), 240);
    }

    function selectableItems() {
        const resultItems = results.hidden
            ? []
            : Array.from(results.querySelectorAll('[data-search-selectable]'));
        const visibleCommands = commands.hidden
            ? []
            : commandItems.filter(item => !item.hidden);
        return [...resultItems, ...visibleCommands];
    }

    function resetSelection() {
        selectedIndex = -1;
        selectableItems().forEach(item => {
            item.classList.remove('is-selected');
            item.setAttribute('aria-selected', 'false');
        });
        input.removeAttribute('aria-activedescendant');
    }

    function moveSelection(direction) {
        const items = selectableItems();
        if (items.length === 0) return;

        selectedIndex = (selectedIndex + direction + items.length) % items.length;
        items.forEach((item, index) => {
            const selected = index === selectedIndex;
            item.classList.toggle('is-selected', selected);
            item.setAttribute('aria-selected', String(selected));
            if (selected) {
                if (!item.id) item.id = `global-search-option-${index}`;
                input.setAttribute('aria-activedescendant', item.id);
                item.scrollIntoView({ block: 'nearest' });
            }
        });
    }

    function isTypingTarget(target) {
        return target instanceof HTMLInputElement ||
            target instanceof HTMLTextAreaElement ||
            target instanceof HTMLSelectElement ||
            target?.isContentEditable;
    }

    function handleNavigationSequence(event) {
        if (dialog.open || isTypingTarget(event.target) || event.ctrlKey || event.metaKey || event.altKey) {
            return false;
        }

        const key = event.key.toLocaleLowerCase('tr-TR');
        if (key === 'g') {
            navigationSequence = 'g';
            window.clearTimeout(navigationTimer);
            navigationTimer = window.setTimeout(() => { navigationSequence = ''; }, 900);
            return true;
        }

        if (navigationSequence === 'g' && (key === 'h' || key === 't')) {
            const href = key === 'h' ? '/Home/Index' : '/Gorev/Index';
            navigationSequence = '';
            window.location.assign(href);
            return true;
        }

        navigationSequence = '';
        return false;
    }

    openButton?.addEventListener('click', openDialog);
    closeButton?.addEventListener('click', closeDialog);
    input.addEventListener('input', handleInput);

    input.addEventListener('keydown', event => {
        if (event.key === 'ArrowDown') {
            event.preventDefault();
            moveSelection(1);
        } else if (event.key === 'ArrowUp') {
            event.preventDefault();
            moveSelection(-1);
        } else if (event.key === 'Enter' && selectedIndex >= 0) {
            const item = selectableItems()[selectedIndex];
            if (item instanceof HTMLAnchorElement) {
                event.preventDefault();
                item.click();
            }
        }
    });

    document.addEventListener('keydown', event => {
        if ((event.ctrlKey || event.metaKey) && event.key.toLocaleLowerCase('tr-TR') === 'k') {
            event.preventDefault();
            dialog.open ? closeDialog() : openDialog();
            return;
        }

        handleNavigationSequence(event);
    });

    dialog.addEventListener('cancel', event => {
        event.preventDefault();
        closeDialog();
    });

    dialog.addEventListener('click', event => {
        if (event.target === dialog) closeDialog();
    });

    commands.addEventListener('click', event => {
        const link = event.target instanceof Element
            ? event.target.closest('a[data-command-item]')
            : null;
        if (link) {
            dialog.close();
            document.body.classList.remove('global-search-open');
        }
    });
})();
