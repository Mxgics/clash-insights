import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
test('local dashboard navigation, history and accessibility', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  await page.goto('http://127.0.0.1:5188/?e2e=launch-20260919');
  await expect(page.getByText(/DEMO DATA|LIVE MODE/, { exact: true })).toBeVisible();
  await expect(page.locator('.progression h2')).toContainText('/ trophies');
  await page.getByRole('button', { name: /View .* progression/ }).first().click();
  await expect(
    page.getByRole('heading', { name: 'Player progression', exact: true }),
  ).toBeVisible();
  await page.getByRole('button', { name: '7D', exact: true }).click();
  await expect(page.getByRole('button', { name: '7D', exact: true })).toHaveAttribute(
    'aria-pressed',
    'true',
  );
  await page.getByText('View exact history', { exact: true }).click();
  await expect(page.getByRole('columnheader', { name: 'Donation counter' })).toBeVisible();
  await page.getByRole('button', { name: 'Wars', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'The war room', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Collection', exact: true }).click();
  await expect(page.locator('section.panel h2').first()).toBeVisible();
  await page.getByRole('button', { name: 'Overview', exact: true }).click();
  expect(
    (await new AxeBuilder({ page }).analyze()).violations.map((v) => ({
      id: v.id,
      nodes: v.nodes.map((n) => ({ target: n.target, summary: n.failureSummary })),
    })),
  ).toEqual([]);
  await page.screenshot({ path: 'test-results/dashboard-desktop.png', fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(
    true,
  );
  expect(
    (await new AxeBuilder({ page }).analyze()).violations.map((v) => ({
      id: v.id,
      nodes: v.nodes.map((n) => ({ target: n.target, summary: n.failureSummary })),
    })),
  ).toEqual([]);
  await page.screenshot({ path: 'test-results/dashboard-mobile.png', fullPage: true });
  expect(errors).toEqual([]);
});

test('empty, failed and stale observations remain explicit', async ({ page }) => {
  const empty = {
    mode: 'Live',
    collectorStatus: 'API token not configured',
    intervalMinutes: 60,
    retentionDays: 90,
    players: [],
    clans: [],
    wars: [],
    attempts: [],
  };
  await page.route('**/api/public/dashboard', (route) => route.fulfill({ json: empty }));
  await page.goto('http://127.0.0.1:5188/?e2e=launch-20260919');
  await expect(page.getByText('No player observations yet.', { exact: false })).toBeVisible();
  await expect(page.getByText('DEMO DATA', { exact: true })).toHaveCount(0);
  await page.unroute('**/api/public/dashboard');
  await page.route('**/api/public/dashboard', (route) => route.fulfill({ status: 503, json: {} }));
  await page.reload();
  await expect(page.getByRole('alert')).toContainText('Unable to load');
  await page.unroute('**/api/public/dashboard');
  await page.route('**/api/public/dashboard', (route) =>
    route.fulfill({
      json: {
        ...empty,
        players: [
          {
            tag: '#P0Y28',
            name: 'Stale player',
            townHall: null,
            trophies: null,
            donations: null,
            received: null,
            trophyChange: null,
            observedAt: '2020-01-01T00:00:00Z',
            history: [],
          },
        ],
      },
    }),
  );
  await page.route('**/history?*', (route) => route.fulfill({ json: [] }));
  await page.reload();
  await expect(page.locator('.observation').filter({ hasText: 'Stale observation' })).toBeVisible();
  await expect(page.getByText('No observations in this range.', { exact: true })).toHaveCount(1);
  expect(
    (await new AxeBuilder({ page }).analyze()).violations.map((v) => ({
      id: v.id,
      nodes: v.nodes.map((n) => ({ target: n.target, summary: n.failureSummary })),
    })),
  ).toEqual([]);
});
test('keyboard navigation and every section', async ({ page }) => {
  await page.goto('http://127.0.0.1:5188/?e2e=launch-20260919');
  await page.locator('body').focus();
  await page.keyboard.press('Tab');
  await expect(page.getByRole('link', { name: 'Skip to dashboard' })).toBeFocused();
  await page.keyboard.press('Enter');
  for (const section of ['Players', 'Clan', 'Wars', 'Collection', 'Overview']) {
    const button = page.getByRole('button', { name: section, exact: true });
    await button.focus();
    await page.keyboard.press('Enter');
    await expect(button).toHaveAttribute('aria-current', 'page');
    expect(
      (await new AxeBuilder({ page }).analyze()).violations.map((v) => ({
        id: v.id,
        nodes: v.nodes.map((n) => ({ target: n.target, summary: n.failureSummary })),
      })),
    ).toEqual([]);
  }
});

test('history API validates ranges and preserves stored reads', async ({ request }) => {
  expect(
    (await request.get('http://127.0.0.1:5188/api/public/players/P0Y28/history?days=5')).status(),
  ).toBe(400);
  expect(
    (await request.get('http://127.0.0.1:5188/api/public/players/INVALID/history?days=7')).status(),
  ).toBe(400);
  const dashboard = await request.get('http://127.0.0.1:5188/api/public/dashboard');
  const firstPlayer = (await dashboard.json()).players[0];
  expect(firstPlayer).toBeTruthy();
  const history = await request.get(
    `http://127.0.0.1:5188/api/public/players/${encodeURIComponent(firstPlayer.tag)}/history?days=90`,
  );
  expect(history.status()).toBe(200);
  expect(Array.isArray(await history.json())).toBe(true);
  expect((await request.get('http://127.0.0.1:5188/api/missing')).status()).toBe(404);
});
