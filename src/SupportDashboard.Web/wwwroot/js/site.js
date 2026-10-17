(() => {
  'use strict';

  const THEME_COOKIE = 'sd-theme';
  const SOUND_KEY = 'sd-sound';
  const html = document.documentElement;

  // ---- Theme (cookie-backed so the server renders the right theme with no flash) ----
  const themeButton = document.getElementById('theme-toggle');

  function setTheme(theme) {
    html.dataset.theme = theme;
    html.setAttribute('data-bs-theme', theme);
    document.cookie = `${THEME_COOKIE}=${theme}; path=/; max-age=31536000; SameSite=Lax`;
    themeButton?.setAttribute('aria-pressed', String(theme === 'dark'));
  }

  themeButton?.addEventListener('click', () => {
    setTheme(html.dataset.theme === 'dark' ? 'light' : 'dark');
  });

  // ---- New-ticket sound preference ----
  const soundButton = document.getElementById('sound-toggle');

  function readSoundPreference() {
    try {
      return localStorage.getItem(SOUND_KEY) !== 'off';
    } catch {
      return true;
    }
  }

  let soundEnabled = readSoundPreference();
  soundButton?.setAttribute('aria-pressed', String(soundEnabled));

  soundButton?.addEventListener('click', () => {
    soundEnabled = !soundEnabled;
    soundButton.setAttribute('aria-pressed', String(soundEnabled));
    try {
      localStorage.setItem(SOUND_KEY, soundEnabled ? 'on' : 'off');
    } catch {
      // Storage unavailable (private mode); the preference just won't persist.
    }
  });

  let audioContext;

  function playChime() {
    try {
      const Ctx = window.AudioContext || window.webkitAudioContext;
      if (!Ctx) return;
      audioContext ??= new Ctx();
      if (audioContext.state === 'suspended') audioContext.resume();

      const start = audioContext.currentTime;
      [[880, 0], [1318.5, 0.12]].forEach(([frequency, offset]) => {
        const osc = audioContext.createOscillator();
        const gain = audioContext.createGain();
        osc.type = 'sine';
        osc.frequency.setValueAtTime(frequency, start + offset);
        gain.gain.setValueAtTime(0.0001, start + offset);
        gain.gain.exponentialRampToValueAtTime(0.22, start + offset + 0.015);
        gain.gain.exponentialRampToValueAtTime(0.0001, start + offset + 0.45);
        osc.connect(gain).connect(audioContext.destination);
        osc.start(start + offset);
        osc.stop(start + offset + 0.5);
      });
    } catch {
      // Audio can be blocked by the browser; notifications still show visually.
    }
  }

  // ---- Toasts ----
  const toastRegion = document.getElementById('toast-region');
  const SVG_NS = 'http://www.w3.org/2000/svg';

  function bellIcon() {
    const svg = document.createElementNS(SVG_NS, 'svg');
    svg.setAttribute('width', '16');
    svg.setAttribute('height', '16');
    svg.setAttribute('viewBox', '0 0 24 24');
    svg.setAttribute('fill', 'none');
    svg.setAttribute('stroke', 'currentColor');
    svg.setAttribute('stroke-width', '2');
    svg.setAttribute('stroke-linecap', 'round');
    svg.setAttribute('aria-hidden', 'true');
    ['M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9', 'M13.73 21a2 2 0 0 1-3.46 0'].forEach(d => {
      const path = document.createElementNS(SVG_NS, 'path');
      path.setAttribute('d', d);
      svg.appendChild(path);
    });
    return svg;
  }

  function showToast({ title, message, tone = 'info', timeoutMs = 6000 }) {
    if (!toastRegion) return;

    const card = document.createElement('div');
    card.className = `toast-card tone-${tone}`;

    const icon = document.createElement('span');
    icon.className = 'toast-card__icon';
    icon.appendChild(bellIcon());

    const body = document.createElement('div');
    body.className = 'toast-card__body';
    const titleEl = document.createElement('div');
    titleEl.className = 'toast-card__title';
    titleEl.textContent = title;
    const messageEl = document.createElement('div');
    messageEl.className = 'toast-card__message';
    messageEl.textContent = message;
    body.append(titleEl, messageEl);

    const close = document.createElement('button');
    close.type = 'button';
    close.className = 'toast-card__close';
    close.setAttribute('aria-label', 'Dismiss notification');
    close.textContent = '✕';

    const dismiss = () => card.remove();
    close.addEventListener('click', dismiss);
    setTimeout(dismiss, timeoutMs);

    card.append(icon, body, close);
    toastRegion.prepend(card);
  }

  window.SupportDashboard = {
    sound: {
      isEnabled: () => soundEnabled,
      play: playChime,
    },
    toast: { show: showToast },
  };
})();
