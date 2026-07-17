(() => {
    "use strict";

    const sidebar = document.getElementById("sidebar");
    const backdrop = document.getElementById("sidebarBackdrop");
    const appShellMain = document.getElementById("appShellMain");
    const mobileOpenButton = document.getElementById("mobileSidebarOpen");
    const mobileCloseButton = document.getElementById("mobileSidebarClose");
    const desktopToggleButton = document.getElementById("desktopSidebarToggle");
    const desktopToggleIcon = document.getElementById("sidebarToggleIcon");
    const desktopQuery = window.matchMedia("(min-width: 1024px)");
    const storageKey = "yonetim-paneli-sidebar-collapsed";
    const focusableSelector = [
        "a[href]",
        "button:not([disabled])",
        "input:not([disabled])",
        "select:not([disabled])",
        "textarea:not([disabled])",
        "[tabindex]:not([tabindex='-1'])"
    ].join(",");

    if (!sidebar) return;

    let returnFocusElement = null;
    let mobileOpen = false;

    function setDesktopCollapsed(collapsed) {
        sidebar.classList.toggle("is-collapsed", collapsed);
        desktopToggleButton?.setAttribute("aria-expanded", String(!collapsed));
        desktopToggleButton?.setAttribute("aria-label", collapsed ? "Menüyü genişlet" : "Menüyü küçült");
        desktopToggleButton?.setAttribute("title", collapsed ? "Menüyü genişlet" : "Menüyü küçült");
        desktopToggleIcon?.classList.toggle("rotate-180", collapsed);

        try {
            localStorage.setItem(storageKey, String(collapsed));
        } catch {
            // Depolama engeli menünün çalışmasını etkilemez.
        }
    }

    function setMainInert(inert) {
        if (!appShellMain) return;
        appShellMain.inert = inert;
        appShellMain.setAttribute("aria-hidden", String(inert));
        if (!inert) appShellMain.removeAttribute("aria-hidden");
    }

    function openMobileSidebar() {
        if (desktopQuery.matches || mobileOpen) return;

        mobileOpen = true;
        returnFocusElement = document.activeElement instanceof HTMLElement
            ? document.activeElement
            : mobileOpenButton;

        sidebar.classList.remove("-translate-x-full");
        sidebar.classList.add("translate-x-0");
        sidebar.removeAttribute("aria-hidden");
        backdrop?.classList.remove("hidden");
        mobileOpenButton?.setAttribute("aria-expanded", "true");
        document.body.classList.add("overflow-hidden");
        setMainInert(true);
        mobileCloseButton?.focus();
    }

    function closeMobileSidebar({ restoreFocus = true } = {}) {
        if (!desktopQuery.matches) {
            sidebar.classList.add("-translate-x-full");
            sidebar.classList.remove("translate-x-0");
            sidebar.setAttribute("aria-hidden", "true");
        }

        backdrop?.classList.add("hidden");
        mobileOpenButton?.setAttribute("aria-expanded", "false");
        document.body.classList.remove("overflow-hidden");
        setMainInert(false);

        const shouldRestore = mobileOpen && restoreFocus;
        mobileOpen = false;
        if (shouldRestore) returnFocusElement?.focus();
        returnFocusElement = null;
    }

    function syncResponsiveState() {
        if (desktopQuery.matches) {
            mobileOpen = false;
            sidebar.classList.remove("-translate-x-full");
            sidebar.classList.add("translate-x-0");
            sidebar.removeAttribute("aria-hidden");
            backdrop?.classList.add("hidden");
            mobileOpenButton?.setAttribute("aria-expanded", "false");
            document.body.classList.remove("overflow-hidden");
            setMainInert(false);
            returnFocusElement = null;
        } else if (!mobileOpen) {
            closeMobileSidebar({ restoreFocus: false });
        }
    }

    function trapSidebarFocus(event) {
        if (!mobileOpen || event.key !== "Tab") return;

        const focusable = Array.from(sidebar.querySelectorAll(focusableSelector))
            .filter((element) => element instanceof HTMLElement && !element.hidden && element.offsetParent !== null);

        if (focusable.length === 0) {
            event.preventDefault();
            sidebar.focus();
            return;
        }

        const first = focusable[0];
        const last = focusable[focusable.length - 1];

        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    }

    let savedCollapsedState = false;
    try {
        savedCollapsedState = localStorage.getItem(storageKey) === "true";
    } catch {
        savedCollapsedState = false;
    }

    setDesktopCollapsed(savedCollapsedState);
    syncResponsiveState();

    desktopToggleButton?.addEventListener("click", () => {
        setDesktopCollapsed(!sidebar.classList.contains("is-collapsed"));
    });
    mobileOpenButton?.addEventListener("click", openMobileSidebar);
    mobileCloseButton?.addEventListener("click", () => closeMobileSidebar());
    backdrop?.addEventListener("click", () => closeMobileSidebar());

    sidebar.addEventListener("click", (event) => {
        if (!desktopQuery.matches && event.target instanceof Element && event.target.closest("a[href]")) {
            closeMobileSidebar({ restoreFocus: false });
        }
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && mobileOpen) {
            event.preventDefault();
            closeMobileSidebar();
            return;
        }

        trapSidebarFocus(event);
    });

    desktopQuery.addEventListener("change", syncResponsiveState);
})();
