(() => {
    const root = document.getElementById('profilePage');
    if (!root) return;

    const tabs = Array.from(root.querySelectorAll('[data-profile-tab]'));
    const panels = Array.from(root.querySelectorAll('[data-profile-panel]'));
    if (!tabs.length || !panels.length) return;

    const activate = (tab) => {
        const target = tab.dataset.profileTab;
        tabs.forEach((item) => {
            const selected = item === tab;
            item.classList.toggle('is-active', selected);
            item.setAttribute('aria-selected', String(selected));
            item.tabIndex = selected ? 0 : -1;
        });
        panels.forEach((panel) => {
            panel.hidden = panel.dataset.profilePanel !== target;
        });
    };

    tabs.forEach((tab, index) => {
        tab.addEventListener('click', () => activate(tab));
        tab.addEventListener('keydown', (event) => {
            let nextIndex = index;
            if (event.key === 'ArrowRight') nextIndex = (index + 1) % tabs.length;
            else if (event.key === 'ArrowLeft') nextIndex = (index - 1 + tabs.length) % tabs.length;
            else if (event.key === 'Home') nextIndex = 0;
            else if (event.key === 'End') nextIndex = tabs.length - 1;
            else return;

            event.preventDefault();
            activate(tabs[nextIndex]);
            tabs[nextIndex].focus();
        });
    });
})();
