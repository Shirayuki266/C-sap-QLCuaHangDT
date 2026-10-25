(() => {
  document.querySelectorAll('img[data-image-fallback],img.detail-image').forEach(img => {
    img.addEventListener('error', () => { if (!img.dataset.fallbackApplied) { img.dataset.fallbackApplied = 'true'; img.src = '/images/phone.svg'; } });
    if (img.complete && img.naturalWidth === 0) img.dispatchEvent(new Event('error'));
  });
  const input = document.querySelector('#header-query'), list = document.querySelector('#search-suggestions'), status = document.querySelector('#search-status');
  if (!input || !list) return;
  let timer, controller, version = 0, selected = -1;
  function close() { list.hidden = true; input.setAttribute('aria-expanded', 'false'); input.removeAttribute('aria-activedescendant'); selected = -1; }
  function message(text) { list.replaceChildren(); const row = document.createElement('div'); row.className = 'suggestion-message'; row.textContent = text; list.append(row); list.hidden = false; input.setAttribute('aria-expanded', 'true'); status.textContent = text; }
  input.addEventListener('input', () => {
    clearTimeout(timer); controller?.abort(); const current = ++version, query = input.value.trim(); selected = -1;
    if (!query) { close(); list.replaceChildren(); status.textContent = ''; return; }
    message('Đang tìm…');
    timer = setTimeout(async () => {
      controller = new AbortController();
      try {
        const response = await fetch('/api/suggestions?query=' + encodeURIComponent(query), { signal: controller.signal });
        if (!response.ok) throw Error('search'); const items = await response.json(); if (current !== version) return;
        if (!items.length) { message('Không có gợi ý phù hợp. Nhấn Enter để tìm kiếm.'); return; }
        list.replaceChildren(); items.forEach((item, i) => { const link = document.createElement('a'); link.href = item.url; link.id = 'suggestion-' + i; link.setAttribute('role', 'option'); link.setAttribute('aria-selected', 'false'); const title = document.createElement('span'); title.textContent = item.label; const kind = document.createElement('small'); kind.textContent = item.kind; link.append(title, kind); list.append(link); });
        list.hidden = false; input.setAttribute('aria-expanded', 'true'); status.textContent = items.length + ' gợi ý';
      } catch (e) { if (e.name !== 'AbortError' && current === version) message('Chưa tải được gợi ý. Bạn vẫn có thể nhấn Enter để tìm kiếm.'); }
    }, 300);
  });
  input.addEventListener('keydown', e => {
    if (e.key === 'Escape') { clearTimeout(timer); controller?.abort(); ++version; close(); return; }
    const links = [...list.querySelectorAll('a')];
    if ((e.key === 'ArrowDown' || e.key === 'ArrowUp') && links.length && !list.hidden) { e.preventDefault(); selected = selected < 0 ? (e.key === 'ArrowDown' ? 0 : links.length - 1) : (selected + (e.key === 'ArrowDown' ? 1 : -1) + links.length) % links.length; links.forEach((a, i) => a.setAttribute('aria-selected', String(i === selected))); input.setAttribute('aria-activedescendant', links[selected].id); links[selected].scrollIntoView({ block: 'nearest' }); }
    if (e.key === 'Enter' && selected >= 0 && !list.hidden) { e.preventDefault(); location.assign(links[selected].href); }
  });
  document.addEventListener('click', e => { if (!e.target.closest('.header-search')) { ++version; clearTimeout(timer); controller?.abort(); close(); } });
})();
document.querySelectorAll('.gallery-thumbnail').forEach(button=>button.addEventListener('click',()=>{const main=document.querySelector('.detail-image');delete main.dataset.fallbackApplied;main.src=button.dataset.image;}));
