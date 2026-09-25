(() => {
  const root = document.querySelector('[data-live-tournament]');
  if (!root || !window.signalR) return;

  const status = document.querySelector('[data-live-status]');
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`/score-hub?tournamentId=${encodeURIComponent(root.dataset.liveTournament)}`)
    .withAutomaticReconnect()
    .build();

  let refreshing = false;
  let queued = false;

  async function refreshScores() {
    if (refreshing) { queued = true; return; }
    refreshing = true;
    do {
      queued = false;
      try {
        const response = await fetch(location.href, { cache: 'no-store' });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const fresh = new DOMParser().parseFromString(await response.text(), 'text/html');
        for (const current of document.querySelectorAll('[data-match-id]')) {
          const kind = current.classList.contains('match-card') ? 'match-card' : 'match-list__row';
          const replacement = fresh.querySelector(`.${kind}[data-match-id="${current.dataset.matchId}"]`);
          if (replacement) current.replaceWith(replacement);
        }
        for (const selector of ['.headline-result', '.standings-table tbody', '.detail-score']) {
          const current = document.querySelector(selector);
          const replacement = fresh.querySelector(selector);
          if (current && replacement) current.replaceWith(replacement);
        }
        if (status) status.textContent = 'Рахунок оновлено редактором';
        document.dispatchEvent(new Event('circuit:score-update'));
      } catch {
        if (status) status.textContent = 'Не вдалося оновити рахунок. Оновіть сторінку.';
      }
    } while (queued);
    refreshing = false;
  }

  connection.on('matchChanged', refreshScores);
  connection.onreconnecting(() => {
    root.dataset.liveConnected = 'false';
    if (status) status.textContent = 'Відновлюємо зв’язок для оновлень';
  });
  connection.onreconnected(() => {
    root.dataset.liveConnected = 'true';
    if (status) status.textContent = 'Оновлення рахунків власного редактора';
    refreshScores();
  });

  async function connect() {
    try {
      await connection.start();
      root.dataset.liveConnected = 'true';
    } catch {
      if (status) status.textContent = 'Оновлення тимчасово недоступні';
      window.setTimeout(connect, 5000);
    }
  }
  connect();
})();
