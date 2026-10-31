"use strict";
// Only present the public prices/availability embedded by the server. Commands remain service-authoritative.
for (const image of document.querySelectorAll("img[data-product-image]")) {
    const fallback = () => { image.hidden = true; const placeholder = image.parentElement.querySelector(".image-placeholder"); if (placeholder) placeholder.hidden = false; };
    image.addEventListener("error", fallback);
    if (image.complete && image.naturalWidth === 0) fallback();
}
const filterPanel = document.querySelector(".catalog-filter");
if (filterPanel) {
    const wide = matchMedia("(min-width:900px)");
    const showFilters = () => { filterPanel.open = wide.matches || !!filterPanel.querySelector(".validation-summary-errors"); };
    showFilters(); wide.addEventListener("change", showFilters);
    const errors = filterPanel.querySelector(".validation-summary-errors"); if (errors) errors.focus();
}
const detail = document.querySelector("[data-product-detail]");
if (detail) {
    const choice = detail.querySelector("#variant-choice");
    const variants = [...choice.options].map(option => ({ id: option.value, storage: option.dataset.storage, color: option.dataset.color,
        price: option.dataset.price, available: Number(option.dataset.available), sku: option.dataset.sku }));
    const storageInputs = [...detail.querySelectorAll("input[name=storageChoice]")];
    const colorInputs = [...detail.querySelectorAll("input[name=colorChoice]")];
    const quantity = detail.querySelector("#product-quantity"), submit = detail.querySelector("#add-to-cart");
    function selectVariant(preferColor) {
        const storage = storageInputs.find(input => input.checked)?.value;
        const requestedColor = colorInputs.find(input => input.checked)?.value;
        const matching = variants.filter(variant => variant.storage === storage);
        const selected = matching.find(variant => variant.color === requestedColor) || (preferColor ? null : matching[0]);
        if (!selected) return;
        for (const input of colorInputs) { input.disabled = !matching.some(variant => variant.color === input.value); input.checked = input.value === selected.color; }
        choice.value = selected.id;
        detail.querySelector("#selected-variant").value = selected.id;
        detail.querySelector("#selected-price").textContent = selected.price;
        detail.querySelector("#selected-availability").textContent = selected.available > 0 ? `Còn ${selected.available} máy có thể đặt` : "Hết hàng";
        detail.querySelector("#selected-storage").textContent = `${selected.storage} GB`;
        detail.querySelector("#selected-color").textContent = selected.color;
        detail.querySelector("#selected-sku").textContent = selected.sku;
        quantity.max = String(selected.available); quantity.disabled = selected.available === 0;
        quantity.value = String(Math.max(1, Math.min(Number(quantity.value) || 1, selected.available)));
        submit.disabled = selected.available === 0; submit.textContent = selected.available === 0 ? "Hết hàng" : "Thêm vào giỏ";
    }
    for (const input of storageInputs) input.addEventListener("change", () => selectVariant(false));
    for (const input of colorInputs) input.addEventListener("change", () => selectVariant(true));
    detail.querySelector(".variant-controls").hidden = false;
    detail.querySelector(".variant-fallback").hidden = true;
    selectVariant(false);
    for (const thumbnail of detail.querySelectorAll("button[data-image-url]")) thumbnail.addEventListener("click", () => {
        const image = detail.querySelector("#main-product-image"); image.hidden = false; image.parentElement.querySelector(".image-placeholder").hidden = true; image.src = thumbnail.dataset.imageUrl;
    });
}
