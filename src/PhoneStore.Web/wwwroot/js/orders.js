"use strict";
(() => {
    const error = document.querySelector(".cancel-form .validation-summary-errors, .orders-filter ~ .validation-summary-errors");
    if (error) error.focus();
    document.addEventListener("submit", event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.matches(".cancel-form") || form.dataset.submitting === "true" || !form.checkValidity()) return;
        if (!window.confirm("Bạn có chắc muốn hủy đơn hàng này?")) { event.preventDefault(); event.stopImmediatePropagation(); }
    }, true);
    document.addEventListener("submit", event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.matches(".cancel-form") || event.defaultPrevented || form.dataset.submitting !== "true") return;
        const button = form.querySelector("button[type=submit]");button.dataset.idleLabel = button.textContent;button.textContent = "Đang hủy đơn…";
    });
    window.addEventListener("pageshow", () => {
        for (const button of document.querySelectorAll(".cancel-form button[data-idle-label]")) { button.textContent = button.dataset.idleLabel;delete button.dataset.idleLabel; }
    });
})();
