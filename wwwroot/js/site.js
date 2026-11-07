(() => {
  if ('serviceWorker' in navigator) window.addEventListener('load', () => navigator.serviceWorker.register('/sw.js').catch(() => {}));
  const qs = (s, r = document) => r.querySelector(s);
  const qsa = (s, r = document) => [...r.querySelectorAll(s)];

  const toggle = qs('[data-nav-toggle]');
  const nav = qs('[data-nav]');
  toggle?.addEventListener('click', () => nav?.classList.toggle('open'));

  const observer = 'IntersectionObserver' in window ? new IntersectionObserver(entries => {
    entries.forEach(entry => { if (entry.isIntersecting) { entry.target.classList.add('in'); observer.unobserve(entry.target); } });
  }, { threshold: .12 }) : null;
  qsa('.reveal').forEach(el => observer ? observer.observe(el) : el.classList.add('in'));

  qsa('[data-auto-dismiss]').forEach(toast => {
    qs('button', toast)?.addEventListener('click', () => toast.remove());
    setTimeout(() => {
  if ('serviceWorker' in navigator) window.addEventListener('load', () => navigator.serviceWorker.register('/sw.js').catch(() => {})); toast.style.opacity = '0'; toast.style.transform = 'translateY(-12px)'; setTimeout(() => toast.remove(), 250); }, 6500);
  });

  const regForm = qs('[data-registration-form]');
  if (regForm) {
    const distances = qsa('[data-distance]', regForm);
    const select = qs('[data-category-select]', regForm);
    const price = qs('[data-summary-price]');
    const summaryDistance = qs('[data-summary-distance]');
    const sync = () => {
      const checked = distances.find(x => x.checked) || distances[0];
      if (!checked) return;
      qsa('option[data-distance-id]', select).forEach(o => o.hidden = o.dataset.distanceId !== checked.value);
      if (select.selectedOptions[0]?.hidden) select.value = '';
      const card = checked.closest('.choice-card');
      summaryDistance && (summaryDistance.textContent = card?.querySelector('b')?.textContent || 'Distancia');
      price && (price.textContent = Number(checked.dataset.price || 0).toLocaleString('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 }));
    };
    distances.forEach(x => x.addEventListener('change', sync));
    sync();
  }

  qsa('[data-network]').forEach(renderNetwork);
  window.addEventListener('online', () => qsa('[data-network]').forEach(renderNetwork));
  window.addEventListener('offline', () => qsa('[data-network]').forEach(renderNetwork));

  const timing = qs('[data-timing-screen]');
  if (timing) initTiming(timing);

  function renderNetwork(el) {
    el.classList.toggle('offline', !navigator.onLine);
    const span = qs('span', el); if (span) span.textContent = navigator.onLine ? 'Conectado' : 'Sin señal';
  }

  function initTiming(root) {
    const eventId = Number(root.dataset.eventId), checkpointId = Number(root.dataset.checkpointId);
    const input = qs('[data-bib-input]', root), button = qs('[data-register-pass]', root), message = qs('[data-timing-message]', root);
    const count = qs('[data-queue-count]', root), syncBtn = qs('[data-sync-queue]', root);
    const lastTime = qs('[data-last-time]', root), lastName = qs('[data-last-name]', root), lastBib = qs('[data-last-bib]', root);
    const key = `sprintime_queue_${eventId}_${checkpointId}`;
    const queue = () => JSON.parse(localStorage.getItem(key) || '[]');
    const save = entries => { localStorage.setItem(key, JSON.stringify(entries)); count.textContent = entries.length; };
    save(queue());

    async function register() {
      const bib = (input.value || '').trim(); if (!bib) return;
      const entry = { eventId, checkpointId, bibNumber: bib, timestampUtc: new Date().toISOString(), clientEntryId: crypto.randomUUID ? crypto.randomUUID() : `${Date.now()}-${Math.random()}` };
      input.value = ''; input.focus();
      if (!navigator.onLine) { const items = queue(); items.push(entry); save(items); showLocal(entry); message.textContent = 'Guardado offline. Se sincronizará cuando vuelva la conexión.'; return; }
      try {
        const res = await fetch('/RaceDay/RegisterPass', { method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(entry) });
        const data = await res.json();
        if (!res.ok) throw new Error(data.error || 'No se pudo registrar.');
        showServer(entry, data); message.textContent = 'Registro confirmado en servidor.';
      } catch (err) {
        const items = queue(); items.push(entry); save(items); showLocal(entry); message.textContent = `Guardado en cola: ${err.message}`;
      }
    }
    function showLocal(entry) { lastTime.textContent = new Date(entry.timestampUtc).toLocaleTimeString('es-AR'); lastName.textContent = 'Pendiente de sincronizar'; lastBib.textContent = `#${entry.bibNumber}`; }
    function showServer(entry, data) { lastTime.textContent = new Date(data.timestamp || entry.timestampUtc).toLocaleTimeString('es-AR'); lastName.textContent = data.name || 'Corredor registrado'; lastBib.textContent = `#${entry.bibNumber}`; }
    async function syncQueue() {
      if (!navigator.onLine) { message.textContent = 'Todavía no hay conexión.'; return; }
      const items = queue(); if (!items.length) { message.textContent = 'La cola offline está vacía.'; return; }
      try {
        const res = await fetch('/RaceDay/Sync', { method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(items) });
        const data = await res.json(); if (!res.ok) throw new Error('Error de sincronización');
        save([]); message.textContent = `${data.synced} registros sincronizados.`;
        if (data.errors?.length) message.textContent += ` Errores: ${data.errors.join(' · ')}`;
      } catch { message.textContent = 'No se pudo sincronizar. La cola permanece guardada.'; }
    }
    button?.addEventListener('click', register);
    input?.addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); register(); } });
    syncBtn?.addEventListener('click', syncQueue);
    window.addEventListener('online', syncQueue);
  }
})();
