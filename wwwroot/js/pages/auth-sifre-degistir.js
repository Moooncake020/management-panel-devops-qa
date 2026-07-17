(() => {
    "use strict";

    document.querySelectorAll("[data-password-toggle]").forEach((button) => {
        button.addEventListener("click", () => {
            const input = document.getElementById(button.dataset.passwordToggle || "");
            if (!(input instanceof HTMLInputElement)) return;

            const show = input.type === "password";
            input.type = show ? "text" : "password";
            button.setAttribute("aria-pressed", String(show));
            button.setAttribute("aria-label", show ? "Şifreyi gizle" : "Şifreyi göster");
            input.focus();
        });
    });

    const form = document.getElementById("changePasswordForm");
    const submitButton = document.getElementById("changePasswordSubmit");

    form?.addEventListener("submit", () => {
        if (window.jQuery && !window.jQuery(form).valid()) return;
        if (!submitButton) return;

        submitButton.disabled = true;
        submitButton.setAttribute("aria-busy", "true");
        const text = submitButton.querySelector("span");
        if (text) text.textContent = "Değiştiriliyor...";
    });
})();
