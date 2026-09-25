const { chromium } = require('@playwright/test');
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const temporary = path.join(__dirname, '.tmp');
const address = 'http://127.0.0.1:5321';
fs.mkdirSync(temporary, { recursive: true });
const database = path.join(temporary, `transport-${process.pid}.db`);
const modes = ['WebSockets', 'ServerSentEvents', 'LongPolling'];

async function waitForServer(server) {
  for (let attempt = 0; attempt < 300; attempt++) {
    if (server.exitCode !== null) throw new Error(`Server stopped with code ${server.exitCode}`);
    try {
      const response = await fetch(`${address}/health`);
      if (response.ok) return;
    } catch {}
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  throw new Error('Server did not start within 30 seconds');
}

async function main() {
  const server = spawn('dotnet', [
    'run', '--project', 'src/Circuit/Circuit.csproj', '--no-build', '-c', 'Release',
    '--no-launch-profile', '--urls', address
  ], {
    cwd: root,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Development',
      ConnectionStrings__Circuit: `Data Source=${database}`,
      Logging__LogLevel__Microsoft: 'Warning'
    },
    stdio: 'ignore'
  });
  let browser;
  try {
    await waitForServer(server);
    browser = await chromium.launch(process.platform === 'win32' ? { channel: 'msedge' } : {});
    const page = await browser.newPage();
    await page.goto(`${address}/studio`);
    await page.addScriptTag({ url: `${address}/js/signalr.min.js` });

    const matches = await (await fetch(`${address}/api/matches?limit=100`)).json();
    const match = matches.items.find(item => item.code === 'G1' && item.scoreA === null);
    if (!match) throw new Error('Expected unplayed G1 match is missing');

    const results = [];
    for (const mode of modes) {
      let requests = 0;
      let wsFrames = 0;
      let wsBytes = 0;
      const onRequest = request => {
        if (request.url().includes('/score-hub')) requests++;
      };
      const onWebSocket = socket => {
        if (!socket.url().includes('/score-hub')) return;
        socket.on('framereceived', frame => {
          wsFrames++;
          wsBytes += Buffer.byteLength(String(frame.payload));
        });
      };
      page.on('request', onRequest);
      page.on('websocket', onWebSocket);
      const connectStarted = Date.now();
      await page.evaluate(async name => {
        const connection = new signalR.HubConnectionBuilder()
          .withUrl('/score-hub?tournamentId=2', {
            transport: signalR.HttpTransportType[name],
            skipNegotiation: name === 'WebSockets'
          }).build();
        window.benchmark = { connection, times: [] };
        connection.on('matchChanged', () => window.benchmark.times.push(Date.now()));
        await connection.start();
      }, mode);
      const connectMs = Date.now() - connectStarted;
      const latencies = [];
      for (let index = 0; index < 6; index++) {
        const score = index % 2 === 0 ? { scoreA: 2, scoreB: 0 } : { scoreA: null, scoreB: null };
        const started = Date.now();
        const response = await fetch(`${address}/api/matches/${match.id}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ ...match, ...score })
        });
        if (response.status !== 204) throw new Error(`${mode}: update returned ${response.status}`);
        await page.waitForFunction(count => window.benchmark.times.length >= count, index + 1);
        const received = await page.evaluate(count => window.benchmark.times[count - 1], index + 1);
        latencies.push(received - started);
      }
      await page.evaluate(() => window.benchmark.connection.stop());
      page.off('request', onRequest);
      page.off('websocket', onWebSocket);
      results.push({ mode, connectMs, requests, wsFrames, wsBytes, latencyMs: latencies,
        medianMs: [...latencies].sort((a, b) => a - b)[2] });
    }
    await page.close();

    const date = new Date().toISOString().slice(0, 10);
    const rows = results.map(item =>
      `| ${item.mode} | ${item.connectMs} | ${item.requests} | ${item.medianMs} | ${item.wsFrames || '—'} | ${item.wsFrames ? item.wsBytes : '—'} |`);
    const report = `# Порівняння способів оновлення рахунку\n\n` +
      `Виміряно ${date} на локальному ASP.NET Core 8 сервері та ${process.platform === 'win32' ? 'Microsoft Edge' : 'Chromium'}. ` +
      `Для кожного способу відкрито окреме з'єднання з тим самим SignalR hub, внесено шість змін одного матчу через API та отримано шість подій. ` +
      `База SQLite окрема від робочої. Команда: \`npm run bench:transport\`.\n\n` +
      `| Транспорт | З'єднання, мс | HTTP запити до hub без upgrade | Медіана доставки, мс | WebSocket кадри | Отримані байти WebSocket |\n` +
      `| --- | ---: | ---: | ---: | ---: | ---: |\n${rows.join('\n')}\n\n` +
      `HTTP запити враховують видимі браузеру узгодження та читання подій, але не WebSocket upgrade і не запити API, які однакові для всіх способів. ` +
      `Для SSE та Long Polling браузер не надає тут порівнянного лічильника байтів, тому його не вигадано. ` +
      `WebSocket зберігає одне з'єднання; SSE також тримає відкритий потік, а Long Polling повторює HTTP запит після відповіді. ` +
      `Ці локальні числа залежать від машини й не прогнозують поведінку мобільної мережі чи багатьох клієнтів. ` +
      `SignalR у звичайному режимі сам обирає доступний транспорт і перепідключається після втрати зв'язку.\n`;
    fs.writeFileSync(path.join(root, 'TRANSPORT_REPORT.md'), report);
    console.log(report);
  } finally {
    await browser?.close();
    server.kill();
  }
}

main().catch(error => { console.error(error); process.exitCode = 1; });
