"use strict";
function websiteLoading() {
    const status = document.querySelector("#navigation-progress");
    if (status) status.hidden = false;
    document.querySelector("#main")?.setAttribute("aria-busy", "true");
}
document.addEventListener("click", event => {
    const link = event.target.closest?.("a[href]");
    if (!link || event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || link.target || link.hasAttribute("download")) return;
    const next = new URL(link.href, location.href);
    if (next.origin === location.origin && (next.pathname !== location.pathname || next.search !== location.search || !next.hash)) websiteLoading();
});
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
document.addEventListener("submit", event => {
    const form = event.target;
    if (form instanceof HTMLFormElement && !event.defaultPrevented && form.checkValidity()) websiteLoading();
});
window.addEventListener("pageshow", () => {
    const status = document.querySelector("#navigation-progress");
    if (status) status.hidden = true;
    document.querySelector("#main")?.removeAttribute("aria-busy");
    for (const form of document.querySelectorAll("form[data-submitting]")) {
        delete form.dataset.submitting;
        form.removeAttribute("aria-busy");
        for (const button of form.querySelectorAll("[data-busy-disabled]")) {
            button.disabled = false;
            delete button.dataset.busyDisabled;
        }
    }
});
