"use strict";
(() => {
    const error = document.querySelector(".checkout-form .validation-summary-errors");
    if (error) error.focus();
    document.addEventListener("submit", event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.matches(".checkout-form") || event.defaultPrevented || form.dataset.submitting !== "true") return;
        const button = form.querySelector("button[type=submit]");
        button.dataset.idleLabel = button.textContent;
        button.textContent = "Đang đặt hàng…";
    });
    window.addEventListener("pageshow", () => {
        for (const button of document.querySelectorAll(".checkout-form button[data-idle-label]")) {
            button.textContent = button.dataset.idleLabel; delete button.dataset.idleLabel;
        }
    });
})();
