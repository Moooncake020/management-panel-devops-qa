(() => {
    "use strict";

    const passwordInput = document.getElementById("loginPassword");
    const toggleButton = document.getElementById("toggleLoginPassword");
    const capsLockWarning = document.getElementById("capsLockWarning");
    const form = document.getElementById("loginForm");
    const submitButton = document.getElementById("loginSubmitButton");
    const buttonText = document.getElementById("loginButtonText");
    const buttonIcon = document.getElementById("loginButtonIcon");
    const spinner = document.getElementById("loginSpinner");

    toggleButton?.addEventListener("click", () => {
        if (!passwordInput) return;

        const show = passwordInput.type === "password";
        passwordInput.type = show ? "text" : "password";
        toggleButton.setAttribute("aria-pressed", String(show));
        toggleButton.setAttribute("aria-label", show ? "Şifreyi gizle" : "Şifreyi göster");
        passwordInput.focus();
    });

    const updateCapsLockWarning = (event) => {
        const capsLockOpen = event.getModifierState?.("CapsLock") ?? false;
        capsLockWarning?.classList.toggle("hidden", !capsLockOpen);
    };

    passwordInput?.addEventListener("keydown", updateCapsLockWarning);
    passwordInput?.addEventListener("keyup", updateCapsLockWarning);
    passwordInput?.addEventListener("blur", () => capsLockWarning?.classList.add("hidden"));

    form?.addEventListener("submit", () => {
        if (window.jQuery && !window.jQuery(form).valid()) return;
        if (!submitButton) return;

        submitButton.disabled = true;
        submitButton.setAttribute("aria-busy", "true");
        if (buttonText) buttonText.textContent = "Giriş yapılıyor...";
        buttonIcon?.classList.add("hidden");
        spinner?.classList.remove("hidden");
    });
})();
