(() => {
  'use strict';

  const page = document.getElementById('incidents-page');
  if (!page) return;

  const apiUrl = page.dataset.apiUrl;
  const PRIORITY_TONES = ['danger', 'warning', 'caution', 'indigo', 'neutral'];
  const STATE_TONES = { 'open': 'info', 'in progress': 'caution', 'resolved': 'success' };

  const els = {
    rows: document.getElementById('incident-rows'),
    count: document.getElementById('incident-count'),
    from: document.getElementById('filter-from'),
    to: document.getElementById('filter-to'),
    sla: document.getElementById('filter-sla'),
    search: document.getElementById('filter-search'),
    clear: document.getElementById('filter-clear'),
    simulate: document.getElementById('simulate-btn'),
    chips: Array.from(page.querySelectorAll('.chip[data-priority]')),
    sortHeaders: Array.from(page.querySelectorAll('th[data-sort-col]')),
  };

  const state = { priority: 'all', search: '', from: '', to: '', sla: '', sort: 'priority', dir: 'asc' };
  const pendingComments = new Map();
  const PRIORITY_VALUES = ['P1', 'P2', 'P3', 'P4', 'P5'];
  const SLA_VALUES = ['response', 'resolution'];
  const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;
  let requestSeq = 0;
  let searchTimer;

  const dateFormat = new Intl.DateTimeFormat('en-US', {
    timeZone: 'America/Chicago', month: 'short', day: 'numeric', year: 'numeric',
  });
  const timeFormat = new Intl.DateTimeFormat('en-US', {
    timeZone: 'America/Chicago', hour: 'numeric', minute: '2-digit', timeZoneName: 'short',
  });

  // ---------- Data ----------
  function buildQuery() {
    const params = new URLSearchParams();
    if (state.priority !== 'all') params.set('priority', state.priority);
    if (state.search.trim()) params.set('search', state.search);
    if (state.from) params.set('from', state.from);
    if (state.to) params.set('to', state.to);
    if (state.sla) params.set('sla', state.sla);
    params.set('sort', state.sort);
    params.set('dir', state.dir);
    return params.toString();
  }

  // Filters live in the page URL too, so links like /Incidents?sla=response open pre-filtered.
  function readStateFromUrl() {
    const params = new URLSearchParams(window.location.search);
    const priority = (params.get('priority') ?? '').toUpperCase();
    if (PRIORITY_VALUES.includes(priority)) state.priority = priority;
    const sla = (params.get('sla') ?? '').toLowerCase();
    if (SLA_VALUES.includes(sla)) state.sla = sla;
    state.search = params.get('search') ?? '';
    if (DATE_PATTERN.test(params.get('from') ?? '')) state.from = params.get('from');
    if (DATE_PATTERN.test(params.get('to') ?? '')) state.to = params.get('to');

    els.search.value = state.search;
    els.sla.value = state.sla;
    els.from.value = state.from;
    els.to.value = state.to;
  }

  function writeStateToUrl() {
    const params = new URLSearchParams();
    if (state.priority !== 'all') params.set('priority', state.priority);
    if (state.search.trim()) params.set('search', state.search);
    if (state.from) params.set('from', state.from);
    if (state.to) params.set('to', state.to);
    if (state.sla) params.set('sla', state.sla);
    const query = params.toString();
    window.history.replaceState(null, '', query ? `?${query}` : window.location.pathname);
  }

  async function load() {
    const seq = ++requestSeq;
    writeStateToUrl();
    page.setAttribute('aria-busy', 'true');
    try {
      const response = await fetch(`${apiUrl}?${buildQuery()}`, { headers: { Accept: 'application/json' } });
      if (!response.ok) throw new Error(`Request failed: ${response.status}`);
      const data = await response.json();
      if (seq !== requestSeq) return;
      render(data);
    } catch (error) {
      if (seq !== requestSeq) return;
      console.error(error);
      renderError();
    } finally {
      if (seq === requestSeq) page.removeAttribute('aria-busy');
    }
  }

  // ---------- Rendering ----------
  function render(data) {
    for (const [key, value] of Object.entries(data.totals)) {
      page.querySelectorAll(`[data-total="${key}"]`).forEach(el => { el.textContent = value; });
    }
    for (const [key, value] of Object.entries(data.facets)) {
      page.querySelectorAll(`[data-facet="${key}"]`).forEach(el => { el.textContent = value; });
    }

    els.chips.forEach(chip => {
      const active = chip.dataset.priority === state.priority;
      chip.classList.toggle('is-active', active);
      chip.setAttribute('aria-pressed', String(active));
    });

    els.sortHeaders.forEach(th => {
      const active = th.dataset.sortCol === state.sort;
      th.setAttribute('aria-sort', active ? (state.dir === 'asc' ? 'ascending' : 'descending') : 'none');
    });

    if (data.items.length === 0) {
      els.rows.replaceChildren(messageRow('No incidents match these filters.'));
    } else {
      els.rows.replaceChildren(...data.items.map(createRow));
    }

    const noun = data.totals.total === 1 ? 'incident' : 'incidents';
    els.count.textContent = `Showing ${data.items.length} of ${data.totals.total} ${noun}`;
  }

  function renderError() {
    const row = messageRow("Couldn't load incidents.");
    const retry = document.createElement('button');
    retry.type = 'button';
    retry.className = 'app-btn app-btn--ghost';
    retry.textContent = 'Try again';
    retry.addEventListener('click', load);
    row.firstChild.appendChild(retry);
    els.rows.replaceChildren(row);
    els.count.textContent = '';
  }

  function messageRow(text) {
    const tr = document.createElement('tr');
    tr.className = 'table-message';
    const td = document.createElement('td');
    td.colSpan = 10;
    td.textContent = text;
    tr.appendChild(td);
    return tr;
  }

  function cell(text, className, title) {
    const td = document.createElement('td');
    if (className) td.className = className;
    if (title) td.title = title;
    if (text !== undefined) td.textContent = text;
    return td;
  }

  function badge(text, tone) {
    const span = document.createElement('span');
    span.className = `badge-pill tone-${tone}`;
    span.textContent = text;
    return span;
  }

  function formatCentral(isoUtc) {
    const date = new Date(isoUtc);
    return `${dateFormat.format(date)}, ${timeFormat.format(date)}`;
  }

  function slaFlags(item) {
    if (!item.responseBreached && !item.resolutionBreached) return null;
    const wrap = document.createElement('div');
    wrap.className = 'sla-flags';
    const add = (label, dueUtc) => {
      const flag = document.createElement('span');
      flag.className = 'sla-flag';
      flag.textContent = label;
      flag.title = `${label.split(' ')[0]} was due ${formatCentral(dueUtc)}`;
      wrap.appendChild(flag);
    };
    if (item.responseBreached) add('Response SLA', item.responseDueUtc);
    if (item.resolutionBreached) add('Resolution SLA', item.resolutionDueUtc);
    return wrap;
  }

  function createRow(item) {
    const tr = document.createElement('tr');
    if (item.isNew) tr.classList.add('is-new');

    const numberCell = cell(item.number, 'mono truncate', item.number);
    if (item.isNew) {
      const tag = document.createElement('span');
      tag.className = 'new-badge';
      tag.textContent = 'NEW';
      numberCell.appendChild(tag);
    }

    const priorityCell = cell();
    priorityCell.appendChild(badge(item.priority, PRIORITY_TONES[item.priorityRank - 1] ?? 'neutral'));

    const stateCell = cell();
    stateCell.appendChild(badge(item.state, STATE_TONES[item.state.toLowerCase()] ?? 'neutral'));
    const flags = slaFlags(item);
    if (flags) stateCell.appendChild(flags);

    const opened = new Date(item.openedUtc);
    const openedCell = cell();
    const dateLine = document.createElement('div');
    dateLine.textContent = dateFormat.format(opened);
    const timeLine = document.createElement('div');
    timeLine.className = 'cell-sub';
    timeLine.textContent = timeFormat.format(opened);
    openedCell.append(dateLine, timeLine);

    const commentCell = cell();
    const input = document.createElement('input');
    input.type = 'text';
    input.className = 'comment-input';
    input.maxLength = 2000;
    input.placeholder = 'Add a comment…';
    input.dataset.number = item.number;
    input.setAttribute('aria-label', `Comment for ${item.number}`);
    input.value = pendingComments.get(item.number) ?? item.comment ?? '';
    commentCell.appendChild(input);

    tr.append(
      numberCell,
      cell(item.shortDescription, 'truncate', item.shortDescription),
      priorityCell,
      stateCell,
      cell(item.assignmentGroup, 'truncate', item.assignmentGroup),
      cell(item.assignedBy, 'truncate', item.assignedBy),
      cell(item.assignedTo, 'truncate', item.assignedTo),
      openedCell,
      cell(item.age),
      commentCell,
    );
    return tr;
  }

  // ---------- Comments ----------
  async function saveComment(input) {
    const number = input.dataset.number;
    const comment = input.value;
    input.classList.remove('is-saved', 'is-error');
    try {
      const response = await fetch(`${apiUrl}/${encodeURIComponent(number)}/comment`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ comment }),
      });
      if (!response.ok) throw new Error(`Request failed: ${response.status}`);
      if (pendingComments.get(number) === comment) pendingComments.delete(number);
      input.classList.add('is-saved');
      input.title = 'Saved';
      setTimeout(() => input.classList.remove('is-saved'), 1500);
    } catch (error) {
      console.error(error);
      input.classList.add('is-error');
      input.title = "Couldn't save this comment";
    }
  }

  els.rows.addEventListener('input', event => {
    if (event.target.matches('.comment-input')) {
      pendingComments.set(event.target.dataset.number, event.target.value);
    }
  });
  els.rows.addEventListener('change', event => {
    if (event.target.matches('.comment-input')) saveComment(event.target);
  });
  els.rows.addEventListener('keydown', event => {
    if (event.key === 'Enter' && event.target.matches('.comment-input')) event.target.blur();
  });

  // ---------- Filters & sorting ----------
  els.search.addEventListener('input', () => {
    state.search = els.search.value;
    // A leftover priority chip would silently hide matches of other priorities, so searching resets it.
    if (state.search.trim()) state.priority = 'all';
    clearTimeout(searchTimer);
    searchTimer = setTimeout(load, 250);
  });

  els.from.addEventListener('change', () => { state.from = els.from.value; load(); });
  els.to.addEventListener('change', () => { state.to = els.to.value; load(); });
  els.sla.addEventListener('change', () => { state.sla = els.sla.value; load(); });

  els.clear.addEventListener('click', () => {
    Object.assign(state, { priority: 'all', search: '', from: '', to: '', sla: '' });
    els.search.value = '';
    els.from.value = '';
    els.to.value = '';
    els.sla.value = '';
    load();
  });

  els.chips.forEach(chip => chip.addEventListener('click', () => {
    state.priority = chip.dataset.priority;
    load();
  }));

  page.querySelectorAll('.sort-btn').forEach(button => button.addEventListener('click', () => {
    const key = button.dataset.sort;
    if (state.sort === key) {
      state.dir = state.dir === 'asc' ? 'desc' : 'asc';
    } else {
      state.sort = key;
      state.dir = 'asc';
    }
    load();
  }));

  // ---------- New-ticket simulation ----------
  els.simulate.addEventListener('click', async () => {
    els.simulate.disabled = true;
    try {
      const response = await fetch(`${apiUrl}/simulate`, { method: 'POST' });
      if (!response.ok) throw new Error(`Request failed: ${response.status}`);
      const incident = await response.json();

      const app = window.SupportDashboard;
      if (app?.sound.isEnabled()) app.sound.play();
      app?.toast.show({
        title: `New ${incident.priority} incident · ${incident.number}`,
        message: incident.shortDescription,
        tone: PRIORITY_TONES[incident.priorityRank - 1] ?? 'info',
      });
      await load();
    } catch (error) {
      console.error(error);
      window.SupportDashboard?.toast.show({
        title: "Couldn't create a sample ticket",
        message: 'Please try again.',
        tone: 'danger',
      });
    } finally {
      els.simulate.disabled = false;
    }
  });

  readStateFromUrl();
  load();
})();
