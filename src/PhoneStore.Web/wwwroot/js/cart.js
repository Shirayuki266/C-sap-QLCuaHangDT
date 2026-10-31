"use strict";
(() => {
    const key = "phonestore.guest-cart.v1";
    const customer = document.body.dataset.customer === "true";
    const page = document.querySelector("#cart-page");
    const badge = document.querySelector("#cart-badge");
    const feedback = document.querySelector("#cart-feedback") || document.querySelector("#cart-sync");
    const money = value => new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 2 }).format(value) + " đ";
    let busy = false;
    function element(tag, text, className) {
        const node = document.createElement(tag); if (text !== undefined) node.textContent = text;
        if (className) node.className = className; return node;
    }
    function normalize(lines) {
        if (!Array.isArray(lines) || lines.length > 100) throw new Error("Giỏ trên trình duyệt không hợp lệ. Vui lòng kiểm tra hoặc xóa giỏ.");
        const result = new Map();
        for (const line of lines) {
            if (!line || !Number.isSafeInteger(line.variantId) || line.variantId < 1 || !Number.isInteger(line.quantity) || line.quantity < 1 || line.quantity > 2147483647)
                throw new Error("Số lượng trong giỏ phải là số nguyên dương.");
            result.set(line.variantId, Math.max(result.get(line.variantId) || 0, line.quantity));
        }
        return Array.from(result, ([variantId, quantity]) => ({ variantId, quantity }));
    }
    function read() { return normalize(JSON.parse(localStorage.getItem(key) || "[]")); }
    function notice(text, retry) {
        feedback.replaceChildren(element("p", text));
        if (retry) { const button = element("button", "Thử lại", "button button-light"); button.type = "button"; button.addEventListener("click", retry); feedback.append(button); }
    }
    function count(cart) { if (badge) badge.textContent = `Giỏ hàng (${cart.lines.reduce((sum, line) => sum + line.quantity, 0)})`; }
    async function request(action, lines) {
        const response = await fetch(`/Cart/${action}`, {
            method: "POST", credentials: "same-origin", redirect: "error",
            headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": document.querySelector('meta[name="csrf-token"]').content },
            body: JSON.stringify({ lines })
        });
        const json = response.headers.get("content-type")?.includes("application/json") ? await response.json() : null;
        if (!response.ok || !json) throw new Error(json?.message || "Chưa kết nối được với cửa hàng. Giỏ trên trình duyệt vẫn được giữ. Vui lòng thử lại.");
        return json;
    }
    function issue(line) {
        return line.issue === "OUT_OF_STOCK" ? "Số lượng vượt khả dụng. Vui lòng giảm số lượng." : line.issue ? "Sản phẩm không còn bán. Vui lòng xóa dòng này." : null;
    }
    function render(cart) {
        count(cart); if (!page || customer) return;
        document.querySelector("#guest-loading")?.remove();
        const content = document.querySelector("#cart-content"); content.replaceChildren();
        if (!cart.lines.length) {
            const empty = element("div", undefined, "cart-empty"); empty.append(element("h2", "Giỏ hàng đang trống"), element("p", "Chọn chiếc điện thoại phù hợp để bắt đầu."));
            const link = element("a", "Xem điện thoại", "button button-primary"); link.href = "/Catalog/Index"; empty.append(link); content.append(empty); return;
        }
        const lines = element("div", undefined, "cart-lines");
        for (const line of cart.lines) {
            const card = element("article", undefined, "cart-line"); card.dataset.variantId = line.variantId;
            const heading = element("div", undefined, "cart-line-heading"); heading.append(element("h2", line.name || "Sản phẩm không còn tồn tại"), element("strong", line.price === null ? "Chưa có giá" : money(line.price)));
            card.append(heading, element("p", `${line.storageGb || "—"} GB · ${line.color || "—"}`), element("p", `Khả dụng: ${line.available}`));
            if (issue(line)) card.append(element("p", issue(line), "cart-line-issue"));
            const actions = element("div", undefined, "cart-line-actions");
            const form = element("form"); const label = element("label", "Số lượng");
            const input = element("input"); input.type = "number"; input.min = "1"; input.max = Math.max(1, line.available); input.step = "1"; input.required = true; input.value = line.quantity; input.id = `quantity-${line.variantId}`; label.htmlFor = input.id;
            const update = element("button", "Cập nhật", "button button-light"); update.type = "submit";
            form.append(label, input, update);
            form.addEventListener("submit", event => { event.preventDefault(); edit(line.variantId, Number(input.value)); });
            const remove = element("button", "Xóa", "text-button"); remove.type = "button"; remove.setAttribute("aria-label", `Xóa ${line.name || "sản phẩm"} khỏi giỏ`); remove.addEventListener("click", () => edit(line.variantId, 0));
            actions.append(form, remove); card.append(actions, element("p", `Thành tiền: ${money((line.price || 0) * line.quantity)}`, "cart-line-total")); lines.append(card);
        }
        const summary = element("aside", undefined, "cart-summary"); summary.setAttribute("aria-label", "Tóm tắt giỏ hàng");
        const total = element("div", undefined, "cart-total"); const amount = element("strong", money(cart.total)); amount.id = "cart-total"; total.append(element("span", "Tổng tiền hàng"), amount);
        const login = element("a", "Đăng nhập để đặt hàng", "button button-primary"); login.href = "/Account/Login?returnUrl=%2FCart%2FIndex";
        summary.append(element("h2", "Tóm tắt giỏ hàng"), element("p", "Thanh toán khi nhận hàng (COD)"), total, element("p", "Giá được cập nhật từ cửa hàng. Hàng chưa được giữ khi ở trong giỏ."), login);
        content.append(lines, summary);
    }
    async function refresh() {
        try { const cart = await request("Quote", read()); render(cart); }
        catch (error) { notice(error.message, refresh); }
    }
    async function edit(variantId, quantity) {
        if (busy) return; busy = true;
        try {
            const lines = read().filter(line => line.variantId !== variantId); if (quantity) lines.push({ variantId, quantity });
            const normalized = normalize(lines); const cart = await request("Quote", normalized);
            const changed = cart.lines.find(line => line.variantId === variantId);
            if (changed?.issue) throw new Error(issue(changed));
            localStorage.setItem(key, JSON.stringify(normalized)); render(cart); notice("Đã cập nhật giỏ hàng theo giá hiện tại.");
        } catch (error) { notice(error.message); } finally { busy = false; }
    }
    async function merge() {
        if (busy) return; busy = true;
        try {
            const snapshot = localStorage.getItem(key); const lines = read(); if (!lines.length) return;
            const result = await request("Merge", lines);
            // Another tab/edit may have changed the guest cart while this request was in flight.
            if (localStorage.getItem(key) !== snapshot) { notice("Giỏ trên trình duyệt vừa thay đổi. Vui lòng hợp nhất lại.", merge); return; }
            localStorage.removeItem(key); count(result.cart);
            notice(result.adjusted.length ? "Đã hợp nhất giỏ hàng. Một số dòng đã được điều chỉnh:" : "Đã hợp nhất giỏ hàng từ trình duyệt.");
            if (result.adjusted.length) {
                const list = element("ul");
                for (const adjusted of result.adjusted) {
                    const line = result.cart.lines.find(row => row.variantId === adjusted.variantId);
                    list.append(element("li", `${line?.name || `Phiên bản ${adjusted.variantId}`}: ${adjusted.requestedQuantity} → ${adjusted.quantity}. ${adjusted.quantity ? "Đã cắt theo khả dụng." : "Đã bỏ dòng không còn bán hoặc hết hàng."}`));
                }
                feedback.append(list);
            }
            if (page) {
                // Reload server-rendered lines without losing the adjustment feedback.
                const response = await fetch("/Cart/Index", { cache: "no-store", credentials: "same-origin", redirect: "error" });
                if (!response.ok) throw new Error("Giỏ đã hợp nhất. Vui lòng tải lại trang để xem giỏ mới.");
                const html = new DOMParser().parseFromString(await response.text(), "text/html");
                document.querySelector("#cart-content").replaceWith(html.querySelector("#cart-content"));
            }
        } catch (error) { notice(error.message, merge); } finally { busy = false; }
    }
    if (!customer) {
        document.addEventListener("submit", async event => {
            const form = event.target;
            if (!(form instanceof HTMLFormElement) || new URL(form.action).pathname !== "/Cart/Add") return;
            event.preventDefault(); event.stopImmediatePropagation();
            if (busy || !form.checkValidity()) return; busy = true;
            try {
                const fields = new FormData(form), variantId = Number(fields.get("variantId")), quantity = Number(fields.get("quantity"));
                const lines = read(); const existing = lines.find(line => line.variantId === variantId);
                if (existing) existing.quantity += quantity; else lines.push({ variantId, quantity });
                const normalized = normalize(lines), cart = await request("Quote", normalized);
                const selected = cart.lines.find(line => line.variantId === variantId);
                if (!selected || selected.issue) throw new Error(selected ? issue(selected) : "Không tìm thấy lựa chọn sản phẩm.");
                localStorage.setItem(key, JSON.stringify(normalized)); window.location.assign("/Cart/Index");
            } catch (error) { notice(error.message); } finally { busy = false; }
        }, true);
        try { const lines = read(); if (badge) badge.textContent = `Giỏ hàng (${lines.reduce((sum, line) => sum + line.quantity, 0)})`; }
        catch (error) { notice(error.message); }
        if (page) refresh();
    } else merge();
    window.addEventListener("storage", event => { if (event.key === key) { if (customer) merge(); else if (page) refresh(); else { try { count({ lines: read() }); } catch (error) { notice(error.message); } } } });
})();
