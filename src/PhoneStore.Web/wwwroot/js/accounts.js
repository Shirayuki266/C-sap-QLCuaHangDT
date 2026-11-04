"use strict";
// Presentation only: prices, authentication and duplicate safety stay on the server.
const accountError = document.querySelector(".account-form .validation-summary-errors, .account-form .input-validation-error");
if (accountError) accountError.focus();
document.addEventListener("submit", event => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.matches(".account-form") || event.defaultPrevented || form.dataset.submitting !== "true") return;
    const button = form.querySelector("button[type=submit]");
    if (!button || button.dataset.idleLabel) return;
    button.dataset.idleLabel = button.textContent;
    button.textContent = "Đang xử lý…";
});
window.addEventListener("pageshow", () => {
    for (const button of document.querySelectorAll("button[data-idle-label]")) {
        button.textContent = button.dataset.idleLabel;
        delete button.dataset.idleLabel;
    }
});
