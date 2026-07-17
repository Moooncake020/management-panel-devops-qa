(() => {
    "use strict";


    document.querySelectorAll("[data-calendar-more]").forEach((button) => {
        button.addEventListener("click", () => {
            const events = button.closest("[data-calendar-events]");
            if (!events) {
                return;
            }

            const expanded = events.classList.toggle("is-expanded");
            const count = button.dataset.count || "0";
            button.textContent = expanded ? "Daha az göster" : `+${count} daha`;
            button.setAttribute("aria-expanded", String(expanded));
        });
    });
})();
