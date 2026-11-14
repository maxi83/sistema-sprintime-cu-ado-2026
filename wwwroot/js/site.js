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
    const validBib = bib => /^(?:[1-9]\d*)$/.test(String(bib || '').trim()) && Number(bib) <= 200;
    const queue = () => {
      try {
        const entries = JSON.parse(localStorage.getItem(key) || '[]');
        return Array.isArray(entries) ? entries : [];
      } catch { return []; }
    };
    const save = entries => { localStorage.setItem(key, JSON.stringify(entries)); count.textContent = entries.length; };
    // Eliminar únicamente dorsales inválidos de versiones anteriores (p. ej. #0).
    save(queue().filter(entry => validBib(entry.bibNumber)));

    let registering = false;
    async function register() {
      if (registering) return;
      const bib = (input.value || '').trim();
      if (!validBib(bib)) {
        message.textContent = 'Ingresá un dorsal válido entre 1 y 200. No se registró ningún paso.';
        input.focus();
        return;
      }
      const entry = { eventId, checkpointId, bibNumber: bib, timestampUtc: new Date().toISOString(), clientEntryId: crypto.randomUUID ? crypto.randomUUID() : `${Date.now()}-${Math.random()}` };
      registering = true;
      button.disabled = true;
      try {
        if (!navigator.onLine) {
          save([...queue(), entry]);
          showLocal(entry);
          message.textContent = 'Sin conexión. Paso guardado en este dispositivo para sincronizar después.';
          input.value = '';
          return;
        }
        let res;
        try {
          res = await fetch('/RaceDay/RegisterPass', { method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(entry) });
        } catch {
          save([...queue(), entry]);
          showLocal(entry);
          message.textContent = 'Sin conexión con el servidor. Paso guardado para sincronizar después.';
          input.value = '';
          return;
        }
        if (!res.ok) {
          let details;
          try { details = await res.json(); } catch {}
          message.textContent = 'No se registró el paso: ' + (details?.error || 'El servidor rechazó el dorsal.');
          input.focus();
          return;
        }
        const data = await res.json();
        showServer(entry, data);
        message.textContent = 'Registro confirmado en servidor.';
        input.value = '';
        input.focus();
      } finally {
        registering = false;
        button.disabled = false;
      }
    }
    function showLocal(entry) { lastTime.textContent = new Date(entry.timestampUtc).toLocaleTimeString('es-AR', { timeZone: 'America/Argentina/Buenos_Aires', hour12: false, hour: '2-digit', minute: '2-digit', second: '2-digit' }); lastName.textContent = 'Pendiente de sincronizar'; lastBib.textContent = `#${entry.bibNumber}`; }
    function showServer(entry, data) { lastTime.textContent = new Date(data.timestamp || entry.timestampUtc).toLocaleTimeString('es-AR', { timeZone: 'America/Argentina/Buenos_Aires', hour12: false, hour: '2-digit', minute: '2-digit', second: '2-digit' }); lastName.textContent = data.name || 'Corredor registrado'; lastBib.textContent = `#${entry.bibNumber}`; }
    async function syncQueue() {
      if (!navigator.onLine) { message.textContent = 'Todavía no hay conexión.'; return; }
      const items = queue();
      if (!items.length) { message.textContent = 'La cola offline está vacía.'; return; }
      try {
        const res = await fetch('/RaceDay/Sync', { method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(items) });
        if (!res.ok) throw new Error('Error de sincronización');
        const data = await res.json();
        if (data.errors?.length) {
          // Conservar los fallidos: el servidor deduplica los pasos ya aceptados por ClientEntryId.
          const failed = new Set(data.errors.map(error => String(error).split(':')[0].trim()));
          save(items.filter(entry => failed.has(String(entry.bibNumber))));
          message.textContent = `${data.synced} sincronizados; ${data.errors.length} pendientes: ${data.errors.join(' · ')}`;
        } else {
          save([]);
          message.textContent = `${data.synced} registros sincronizados.`;
        }
      } catch {
        message.textContent = 'No se pudo sincronizar. La cola permanece guardada.';
      }
    }
    button?.addEventListener('click', register);
    input?.addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); register(); } });
    syncBtn?.addEventListener('click', syncQueue);
    window.addEventListener('online', syncQueue);
  }
})();
