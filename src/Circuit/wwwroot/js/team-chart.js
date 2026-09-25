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
        backgroundColor: margins.map(value => value > 0 ? '#ff4655' : '#aeb9c5'),
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
        x: { min: -3, max: 3, grid: { color: '#354253' }, border: { display: false }, ticks: { color: '#aeb9c5', stepSize: 1, font: { family: 'IBM Plex Mono' } } },
        y: { grid: { display: false }, border: { display: false }, ticks: { color: '#f4f1ec', font: { family: 'IBM Plex Mono' } } }
      }
    }
  });
})();
