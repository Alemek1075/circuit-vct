import { spawn } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const address = 'http://127.0.0.1:5322';
const publicBase = 'https://alemek1075.github.io/circuit-vct/';
const prefix = '/circuit-vct/';
const output = path.join(root, 'tests', '.tmp', 'circuit-vct');
const database = path.join(root, 'tests', '.tmp', `export-${process.pid}.db`);
fs.mkdirSync(path.dirname(output), { recursive: true });

function pagePath(raw) {
  const parsed = new URL(raw.replaceAll('&amp;', '&'), address);
  let file;
  if (parsed.pathname === '/') {
    const tournament = parsed.searchParams.get('tournament');
    file = tournament && tournament !== 'paris-2025' ? `${tournament}.html` : 'index.html';
    const team = parsed.searchParams.get('team');
    return `${file}${team ? `?team=${encodeURIComponent(team)}` : ''}${parsed.hash}`;
  }
  if (parsed.pathname.startsWith('/team/') || parsed.pathname.startsWith('/match/'))
    return `${parsed.pathname.slice(1)}.html${parsed.hash}`;
  return `${parsed.pathname.slice(1)}${parsed.search}${parsed.hash}`;
}

function convert(html) {
  return html
    .replace(/<script[^>]+src="\/js\/(?:signalr\.min|live-scores)[^"]*"[^>]*><\/script>/g, '')
    .replace(/Оновлення рахунків власного редактора/g, 'Статичний знімок турніру')
    .replace(/\sdata-live-tournament="\d+"/g, '')
    .replace(/(href|src)="(\/[^\"]*)"/g, (_, attribute, value) =>
      `${attribute}="${prefix}${pagePath(value).replaceAll('&', '&amp;')}"`)
    .replace(/href="http:\/\/127\.0\.0\.1:5322([^\"]*)"/g, (_, value) =>
      `href="${publicBase}${pagePath(value).replaceAll('&', '&amp;')}"`);
}

async function waitForServer(server) {
  for (let attempt = 0; attempt < 300; attempt++) {
    if (server.exitCode !== null) throw new Error(`Server stopped with code ${server.exitCode}`);
    try {
      if ((await fetch(`${address}/health`)).ok) return;
    } catch {}
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  throw new Error('Server did not start within 30 seconds');
}

const server = spawn('dotnet', [
  'run', '--project', 'src/Circuit/Circuit.csproj', '--no-build', '-c', 'Release',
  '--no-launch-profile', '--urls', address
], {
  cwd: root,
  env: {
    ...process.env,
    ASPNETCORE_ENVIRONMENT: 'Production',
    ConnectionStrings__Circuit: `Data Source=${database}`,
    Logging__LogLevel__Microsoft: 'Warning'
  },
  stdio: 'ignore'
});

try {
  await waitForServer(server);
  const tournaments = await (await fetch(`${address}/api/tournaments`)).json();
  const teams = await (await fetch(`${address}/api/teams`)).json();
  const matches = await (await fetch(`${address}/api/matches?limit=100`)).json();
  if (matches.total > 100) throw new Error('Static export needs pagination for more than 100 matches');
  const pages = [
    ...tournaments.map(item => [item.slug === 'paris-2025' ? 'index.html' : `${item.slug}.html`,
      `/?tournament=${encodeURIComponent(item.slug)}`]),
    ...teams.map(item => [`team/${item.id}.html`, `/team/${item.id}`]),
    ...matches.items.map(item => [`match/${item.id}.html`, `/match/${item.id}`])
  ];
  if (output !== path.resolve(root, 'tests', '.tmp', 'circuit-vct'))
    throw new Error('Unexpected export directory');
  fs.rmSync(output, { recursive: true, force: true });
  fs.mkdirSync(output, { recursive: true });
  fs.cpSync(path.join(root, 'src', 'Circuit', 'wwwroot'), output, { recursive: true });
  const stylesheet = path.join(output, 'css', 'site.css');
  fs.writeFileSync(stylesheet, fs.readFileSync(stylesheet, 'utf8')
    .replaceAll('url("/fonts/', `url("${prefix}fonts/`));
  for (const [name, route] of pages) {
    const response = await fetch(address + route);
    if (!response.ok) throw new Error(`${route} returned ${response.status}`);
    const target = path.join(output, name);
    fs.mkdirSync(path.dirname(target), { recursive: true });
    fs.writeFileSync(target, convert(await response.text()));
  }
  fs.writeFileSync(path.join(output, '.nojekyll'), '');
  fs.writeFileSync(path.join(output, 'robots.txt'),
    `User-agent: *\nAllow: /\nSitemap: ${publicBase}sitemap.xml\n`);
  const sitemap = `<?xml version="1.0" encoding="utf-8"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">` +
    pages.map(([name]) => `<url><loc>${publicBase}${name}</loc></url>`).join('') + '</urlset>';
  fs.writeFileSync(path.join(output, 'sitemap.xml'), sitemap);
  console.log(`Exported ${pages.length} public pages to ${output}`);
} finally {
  server.kill();
}
