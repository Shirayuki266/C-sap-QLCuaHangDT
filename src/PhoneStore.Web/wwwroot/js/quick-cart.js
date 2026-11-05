"use strict";
// Choose a real server-provided variant before using the existing cart command.
for (const button of document.querySelectorAll("[data-quick-cart]")) {
    const panel = button.closest(".product-card").querySelector(".quick-cart-panel");
    button.addEventListener("click", async () => {
        if (panel.childElementCount) {
            panel.hidden = !panel.hidden;
            button.setAttribute("aria-expanded", String(!panel.hidden));
            return;
        }
        button.disabled = true;
        panel.hidden = false;
        panel.textContent = "Đang tải phiên bản…";
        button.setAttribute("aria-expanded", "true");
        try {
            const response = await fetch(`/Catalog/Details/${button.dataset.quickCart}`, { credentials: "same-origin", cache: "no-store" });
            if (!response.ok) throw new Error();
            const html = new DOMParser().parseFromString(await response.text(), "text/html");
            const source = html.querySelector("#variant-choice"), form = html.querySelector(".product-add-form");
            if (!source || !form) throw new Error();
            const select = source.cloneNode(true);
            select.id = `quick-variant-${button.dataset.quickCart}`;
            select.name = "variantId";
            const label = document.createElement("label");
            label.htmlFor = select.id; label.textContent = "Chọn phiên bản";
            form.querySelector('[name="variantId"]').remove();
            form.querySelector('[name="quantity"]').closest("div").remove();
            const quantity = document.createElement("input");
            quantity.type = "hidden"; quantity.name = "quantity"; quantity.value = "1";
            const submit = form.querySelector('[type="submit"]'); submit.removeAttribute("id");
            const availability = document.createElement("p"); availability.setAttribute("role", "status");
            function update() {
                const available = Number(select.selectedOptions[0].dataset.available);
                submit.disabled = available < 1;
                submit.textContent = available > 0 ? "Thêm vào giỏ" : "Hết hàng";
                availability.textContent = available > 0 ? `Còn ${available} máy · Thêm 1 máy vào giỏ` : "Phiên bản này đã hết hàng. Vui lòng chọn phiên bản khác.";
            }
            select.addEventListener("change", update);
            const availableOption = [...select.options].find(option => Number(option.dataset.available) > 0);
            if (availableOption) select.value = availableOption.value;
            form.prepend(label, select, quantity); form.append(availability); update();
            panel.replaceChildren(form); select.focus();
        } catch {
            panel.textContent = "Chưa tải được phiên bản. Bấm giỏ hàng để thử lại hoặc xem chi tiết.";
        } finally { button.disabled = false; }
    });
}
