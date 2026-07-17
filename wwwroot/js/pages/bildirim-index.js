(() => {
    const page = document.querySelector('.notification-page');
    if (!page) return;

    const searchInput = page.querySelector('#notificationSearch');

    searchInput?.addEventListener('keydown', event => {
        if (event.key === 'Escape' && searchInput.value) {
            searchInput.value = '';
            searchInput.form?.requestSubmit();
        }
    });

    page.addEventListener('toggle', event => {
        const openedMenu = event.target instanceof HTMLDetailsElement &&
            event.target.matches('.notification-item__menu') &&
            event.target.open
            ? event.target
            : null;

        if (!openedMenu) return;

        page.querySelectorAll('.notification-item__menu[open]').forEach(menu => {
            if (menu !== openedMenu) menu.removeAttribute('open');
        });
    }, true);
})();
