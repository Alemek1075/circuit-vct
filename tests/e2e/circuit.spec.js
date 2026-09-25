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

test('mobile layout keeps the page within the viewport', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/');
  await expect(page.getByRole('navigation', { name: 'Вибір турніру' })).toBeVisible();
  const widths = await page.evaluate(() => ({ client: document.documentElement.clientWidth, scroll: document.documentElement.scrollWidth }));
  expect(widths.scroll).toBeLessThanOrEqual(widths.client);
  await page.getByRole('navigation', { name: 'Вибір турніру' }).getByRole('link', { name: 'Шанхай 2026' }).click();
  await expect(page.getByRole('heading', { name: 'Стартові матчі' })).toBeVisible();
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
