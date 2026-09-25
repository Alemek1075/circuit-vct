const { test, expect } = require('@playwright/test');

test('Paris bracket shows verified results and the selected team route', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'CHAMPIONS ПАРИЖ' })).toBeVisible();
  await expect(page.locator('.bracket-grid--upper .match-card')).toHaveCount(8);
  await expect(page.locator('.bracket-grid--lower .match-card')).toHaveCount(6);
  await expect(page.getByRole('table')).toContainText('NRG');
  await page.getByRole('combobox', { name: 'Шлях команди' }).selectOption({ label: 'NRG' });
  await expect(page.locator('.match-card.is-highlighted')).toHaveCount(4);
  await expect(page.locator('.bracket-lines path.is-highlighted')).toHaveCount(3);
  await page.getByRole('link', { name: 'Відкрити фінал' }).click();
  await expect(page.getByRole('heading', { name: 'Гранд-фінал' })).toBeVisible();
  await expect(page.locator('.detail-score__value')).toHaveText('3:2');
});

test('Shanghai shows scheduled fixtures without fabricated scores', async ({ page }) => {
  await page.goto('/?tournament=shanghai-2026');
  await expect(page.getByRole('heading', { name: 'CHAMPIONS ШАНХАЙ' })).toBeVisible();
  await expect(page.locator('.fixture-day .match-card')).toHaveCount(8);
  await expect(page.locator('.region-list a')).toHaveCount(16);
  await expect(page.locator('.fixture-day .match-team.is-winner')).toHaveCount(0);
  await page.locator('.fixture-day .match-card').first().click();
  await expect(page.getByRole('heading', { name: 'Груповий етап · стартовий матч' })).toBeVisible();
  await expect(page.locator('.detail-score__value')).toHaveText('— : —');
});

test('match filters and page metadata follow the selected tournament', async ({ page }) => {
  await page.goto('/');
  await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', 'http://127.0.0.1:5307/');
  await page.getByLabel('Етап сітки').selectOption('lower');
  await expect(page.locator('.match-list__row:visible')).toHaveCount(6);
  await expect(page.locator('#match-filter-count')).toHaveText('6 матчів');
  await page.goto('/?tournament=shanghai-2026');
  await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', 'http://127.0.0.1:5307/?tournament=shanghai-2026');
  const day = await page.getByLabel('День матчів').locator('option').nth(1).getAttribute('value');
  await page.getByLabel('День матчів').selectOption(day);
  await expect(page.locator('.match-list__row:visible')).toHaveCount(2);
});

test('sitemap includes public details and the studio stays out of search indexes', async ({ page, request }) => {
  const sitemap = await request.get('/sitemap.xml');
  expect(sitemap.status()).toBe(200);
  expect(sitemap.headers()['content-type']).toContain('application/xml');
  expect(await sitemap.text()).toContain('/team/');
  expect(await sitemap.text()).toContain('/match/');
  await page.goto('/studio');
  await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', 'noindex,nofollow');
});

test('editor score changes reach open tournament tabs without navigation', async ({ context, request }) => {
  const first = await context.newPage();
  const second = await context.newPage();
  await Promise.all([first.goto('/?tournament=shanghai-2026'), second.goto('/?tournament=shanghai-2026')]);
  await Promise.all([
    expect(first.locator('[data-live-tournament]')).toHaveAttribute('data-live-connected', 'true'),
    expect(second.locator('[data-live-tournament]')).toHaveAttribute('data-live-connected', 'true')
  ]);
  const id = await first.locator('.match-card[data-match-code="G1"]').getAttribute('data-match-id');
  const original = await (await request.get(`/api/matches/${id}`)).json();
  await first.evaluate(() => { window.circuitNavigationMarker = 'still-here'; });
  try {
    const changed = await request.put(`/api/matches/${id}`, { data: { ...original, scoreA: 2, scoreB: 0 } });
    expect(changed.status()).toBe(204);
    for (const page of [first, second]) {
      const card = page.locator('.match-card[data-match-code="G1"]');
      await expect(card.locator('.match-team__score')).toHaveText(['2', '0']);
      await expect(page.locator('[data-live-status]')).toHaveText('Рахунок оновлено редактором');
    }
    expect(await first.evaluate(() => window.circuitNavigationMarker)).toBe('still-here');
  } finally {
    await request.put(`/api/matches/${id}`, { data: original });
    await first.close();
    await second.close();
  }
});

test('team result chart uses played matches and scheduled teams show an empty state', async ({ page }) => {
  await page.goto('/');
  await page.locator('.table-team').filter({ hasText: 'NRG' }).click();
  await expect(page.getByRole('heading', { name: 'Різниця карт' })).toBeVisible();
  await expect(page.locator('[data-team-chart]')).toBeVisible();
  await expect.poll(() => page.evaluate(() => Boolean(window.Chart?.getChart(document.querySelector('[data-team-chart] canvas'))))).toBe(true);
  await page.goto('/?tournament=shanghai-2026');
  await page.locator('.region-list a').first().click();
  await expect(page.getByText('Результатів для графіка поки немає.')).toBeVisible();
});

test('mobile layout keeps the page within the viewport', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/');
  await expect(page.getByRole('navigation', { name: 'Вибір турніру' })).toBeVisible();
  const widths = await page.evaluate(() => ({ client: document.documentElement.clientWidth, scroll: document.documentElement.scrollWidth }));
  expect(widths.scroll).toBeLessThanOrEqual(widths.client);
  await page.getByRole('navigation', { name: 'Вибір турніру' }).getByRole('link', { name: 'Шанхай 2026' }).click();
  await expect(page.getByRole('heading', { name: 'Стартові матчі' })).toBeVisible();
  await page.goto('/');
  await page.locator('.table-team').filter({ hasText: 'NRG' }).click();
  const teamWidths = await page.evaluate(() => ({ client: document.documentElement.clientWidth, scroll: document.documentElement.scrollWidth }));
  expect(teamWidths.scroll).toBeLessThanOrEqual(teamWidths.client);
});

test('local studio creates edits and deletes a team', async ({ page }) => {
  await page.goto('/studio/teams/new');
  await page.getByLabel('Турнір').selectOption({ label: 'Champions Paris 2025' });
  await page.getByLabel('Назва').fill('QA Team');
  await page.getByLabel('Скорочення').fill('QAT');
  await page.getByLabel('Регіон').fill('EMEA');
  await page.getByRole('button', { name: 'Зберегти команду' }).click();
  await expect(page.getByRole('status')).toHaveText('Команду створено.');

  const row = page.locator('.studio-row').filter({ hasText: 'QA Team' });
  await row.getByRole('link', { name: 'Змінити' }).click();
  await page.getByLabel('Назва').fill('QA Team Updated');
  await page.getByRole('button', { name: 'Зберегти команду' }).click();
  await expect(page.getByRole('status')).toHaveText('Команду оновлено.');
  await page.locator('.studio-row').filter({ hasText: 'QA Team Updated' }).getByRole('button', { name: 'Видалити команду QA Team Updated' }).click();
  await expect(page.getByRole('status')).toHaveText('Команду видалено.');
  await expect(page.locator('.studio-row').filter({ hasText: 'QA Team Updated' })).toHaveCount(0);
});

test('match editor searches teams after three characters and supports keyboard choice', async ({ page }) => {
  await page.goto('/studio/matches/new');
  await page.getByLabel('Турнір').selectOption({ label: 'Champions Paris 2025' });
  const teamA = page.getByRole('combobox', { name: 'Команда A' });
  await teamA.fill('Pa');
  await expect(page.locator('#team-a-options [role="option"]')).toHaveCount(0);
  await teamA.fill('Paper');
  await expect(page.locator('#team-a-options').getByRole('option', { name: 'Paper Rex · PRX' })).toBeVisible();
  await teamA.press('ArrowDown');
  await teamA.press('Enter');
  await expect(teamA).toHaveValue('Paper Rex');
  await expect(page.locator('#TeamAId')).not.toHaveValue('');
  await page.getByLabel('Турнір').selectOption({ label: 'Champions Shanghai 2026' });
  await expect(page.locator('#TeamAId')).toHaveValue('');
});

test('API validates results, paginates, and protects linked records', async ({ request }) => {
  const firstPage = await request.get('/api/matches?skip=0&limit=1');
  expect(firstPage.status()).toBe(200);
  const page = await firstPage.json();
  expect(page.items).toHaveLength(1);
  expect(page.nextLink).toContain('/api/matches?skip=1&limit=1');
  expect((await request.get('/api/matches?limit=101')).status()).toBe(400);

  let tournamentId;
  let matchId;
  const teamIds = [];
  try {
    const tournament = await request.post('/api/tournaments', { data: {
      slug: `qa-${Date.now()}`, name: 'QA Tournament', season: 2030, city: 'Test',
      startsOn: '2030-01-01', endsOn: '2030-01-31', isComplete: false,
      sourceUrl: 'https://example.org/tournament', snapshotOn: '2030-01-01'
    }});
    expect(tournament.status()).toBe(201);
    tournamentId = (await tournament.json()).id;

    for (const shortName of ['QAA', 'QAB']) {
      const team = await request.post('/api/teams', { data: {
        tournamentId, name: `QA ${shortName}`, shortName, region: 'Test', accentColor: '#76B8BA'
      }});
      expect(team.status()).toBe(201);
      teamIds.push((await team.json()).id);
    }

    const data = {
      tournamentId, code: 'QA1', lane: 'upper', round: 1, slot: 1, label: 'QA match',
      teamAId: teamIds[0], teamBId: teamIds[1], scoreA: 2, scoreB: 1, bestOf: 3
    };
    expect((await request.post('/api/matches', { data: { ...data, scoreA: 1 } })).status()).toBe(400);
    const match = await request.post('/api/matches', { data });
    expect(match.status()).toBe(201);
    matchId = (await match.json()).id;
    expect((await request.delete(`/api/teams/${teamIds[0]}`)).status()).toBe(409);
    expect((await request.put(`/api/matches/${matchId}`, { data: { ...data, scoreA: 0, scoreB: 2 } })).status()).toBe(204);
    expect((await (await request.get(`/api/matches/${matchId}`)).json()).scoreB).toBe(2);
  } finally {
    if (matchId) await request.delete(`/api/matches/${matchId}`);
    for (const id of teamIds) await request.delete(`/api/teams/${id}`);
    if (tournamentId) await request.delete(`/api/tournaments/${tournamentId}`);
  }
});
