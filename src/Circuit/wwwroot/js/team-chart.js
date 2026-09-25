(() => {
  const holder = document.querySelector('[data-team-chart]');
  if (!holder || !window.Chart) return;
  const { labels, margins } = JSON.parse(holder.dataset.teamChart);
  new Chart(holder.querySelector('canvas'), {
    type: 'bar',
    data: {
      labels,
      datasets: [{
        data: margins,
        backgroundColor: margins.map(value => value > 0 ? '#8ecbc3' : '#e9aa76'),
        borderRadius: 0,
        barThickness: 18
      }]
    },
    options: {
      indexAxis: 'y',
      responsive: true,
      maintainAspectRatio: false,
      animation: false,
      plugins: {
        legend: { display: false },
        tooltip: { callbacks: { label: context => `${context.raw > 0 ? '+' : ''}${context.raw} карт` } }
      },
      scales: {
        x: { min: -3, max: 3, grid: { color: '#2b434b' }, border: { display: false }, ticks: { color: '#8fa5aa', stepSize: 1, font: { family: 'IBM Plex Mono' } } },
        y: { grid: { display: false }, border: { display: false }, ticks: { color: '#d9e4e3', font: { family: 'IBM Plex Mono' } } }
      }
    }
  });
})();
