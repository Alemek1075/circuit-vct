(() => {
  const boards = [...document.querySelectorAll('[data-bracket]')];
  const picker = document.querySelector('#team-select');

  function drawBoard(board) {
    const svg = board.querySelector('.bracket-lines');
    if (!svg) return;
    const boardBox = board.getBoundingClientRect();
    svg.setAttribute('viewBox', `0 0 ${boardBox.width} ${boardBox.height}`);
    svg.replaceChildren();

    for (const card of board.querySelectorAll('.match-card[data-next-win]')) {
      const code = card.dataset.nextWin;
      if (!code) continue;
      const target = [...board.querySelectorAll('.match-card')]
        .find(item => item.dataset.matchCode === code);
      if (!target) continue;

      const from = card.getBoundingClientRect();
      const to = target.getBoundingClientRect();
      const x1 = from.right - boardBox.left;
      const y1 = from.top + from.height / 2 - boardBox.top;
      const x2 = to.left - boardBox.left;
      const y2 = to.top + to.height / 2 - boardBox.top;
      const middle = x1 + (x2 - x1) / 2;
      const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
      path.setAttribute('d', `M ${x1} ${y1} H ${middle} V ${y2} H ${x2}`);
      path.dataset.from = card.dataset.matchCode;
      path.dataset.to = code;
      svg.append(path);
    }
  }

  function containsTeam(card, teamId) {
    return card.dataset.teamA === teamId || card.dataset.teamB === teamId;
  }

  function highlight(teamId) {
    document.querySelectorAll('.match-card,.match-list__row').forEach(card => {
      card.classList.toggle('is-highlighted', Boolean(teamId) && containsTeam(card, teamId));
      card.classList.toggle('is-muted', Boolean(teamId) && !containsTeam(card, teamId));
    });
    document.querySelectorAll('[data-team-row]').forEach(row => {
      row.classList.toggle('is-muted', Boolean(teamId) && row.dataset.teamRow !== teamId);
    });
    for (const board of boards) {
      for (const path of board.querySelectorAll('.bracket-lines path')) {
        const from = [...board.querySelectorAll('.match-card')]
          .find(card => card.dataset.matchCode === path.dataset.from);
        const to = [...board.querySelectorAll('.match-card')]
          .find(card => card.dataset.matchCode === path.dataset.to);
        const active = Boolean(teamId && from && to && containsTeam(from, teamId) && containsTeam(to, teamId));
        path.classList.toggle('is-highlighted', active);
        path.classList.toggle('is-muted', Boolean(teamId) && !active);
      }
    }
  }

  if (boards.length) {
    const redraw = () => { boards.forEach(drawBoard); highlight(picker?.value ?? ''); };
    if (document.fonts?.ready) document.fonts.ready.then(redraw);
    else requestAnimationFrame(redraw);
    new ResizeObserver(redraw).observe(document.querySelector('.content-shell'));
    picker?.addEventListener('change', () => {
      highlight(picker.value);
      const url = new URL(location.href);
      if (picker.value) url.searchParams.set('team', picker.value);
      else url.searchParams.delete('team');
      history.replaceState(null, '', url);
    });
    const initialTeam = new URL(location.href).searchParams.get('team');
    if (picker && initialTeam && [...picker.options].some(option => option.value === initialTeam)) {
      picker.value = initialTeam;
      highlight(initialTeam);
    }
  }
})();
