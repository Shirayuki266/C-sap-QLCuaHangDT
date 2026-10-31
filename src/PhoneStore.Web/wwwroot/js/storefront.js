"use strict";
// This is a UX guard; service transactions remain authoritative for repeated requests.
document.addEventListener("submit", event => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || form.method.toLowerCase() !== "post") return;
    if (form.dataset.submitting === "true") { event.preventDefault(); return; }
    if (!form.checkValidity()) return;
    form.dataset.submitting = "true";
    form.setAttribute("aria-busy", "true");
    for (const button of form.querySelectorAll("button[type=submit],input[type=submit]")) {
        // Keep named submit controls in the form payload.
        if (button.name) continue;
        if (button.disabled) continue;
        button.dataset.busyDisabled = "true";
        button.disabled = true;
    }
});
window.addEventListener("pageshow", () => {
    for (const form of document.querySelectorAll("form[data-submitting]")) {
        delete form.dataset.submitting;
        form.removeAttribute("aria-busy");
        for (const button of form.querySelectorAll("[data-busy-disabled]")) {
            button.disabled = false;
            delete button.dataset.busyDisabled;
        }
    }
});
