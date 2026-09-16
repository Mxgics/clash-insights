import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
test('profile persistence, preference effects and both accessible themes', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'light' });
  await page.goto('http://127.0.0.1:5188');
  await page.getByRole('button', { name: 'My profile', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'My profile', exact: true })).toBeVisible();
  await page.getByLabel('Display name', { exact: true }).fill('Zach');
  await page.getByLabel('Preferred player tag', { exact: true }).fill('p0y29');
  await page.getByLabel('Default history range', { exact: true }).selectOption('7');
  await page.getByRole('button', { name: 'Save preferences', exact: true }).click();
  await expect(page.getByRole('status')).toHaveText('Profile preferences saved.');
  await page.getByRole('button', { name: /^dark mode$/i }).click();
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
  expect(
    (await new AxeBuilder({ page }).analyze()).violations.map((v) => ({
      id: v.id,
      nodes: v.nodes.map((n) => n.target),
    })),
  ).toEqual([]);
  await page.screenshot({ path: 'test-results/profile-dark.png', fullPage: true });
  await page.reload();
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
  await expect(page.getByRole('button', { name: '7D', exact: true })).toHaveAttribute(
    'aria-pressed',
    'true',
  );
  await expect(page.getByRole('heading', { name: 'Ember / trophies' })).toBeVisible();
  for (const section of ['Overview', 'Players', 'Clan', 'Wars', 'Collection', 'My profile']) {
    await page.getByRole('button', { name: section, exact: true }).click();
    expect(
      (await new AxeBuilder({ page }).analyze()).violations.map((v) => ({
        id: v.id,
        nodes: v.nodes.map((n) => n.target),
      })),
    ).toEqual([]);
  }
  await expect(page.getByLabel('Display name', { exact: true })).toHaveValue('Zach');
  await page.getByRole('button', { name: 'Use device theme', exact: true }).click();
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'light');
  await page.emulateMedia({ colorScheme: 'dark' });
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
  await page.getByRole('button', { name: /^light mode$/i }).click();
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  expect((await new AxeBuilder({ page }).analyze()).violations.map((v) => v.id)).toEqual([]);
  await page.screenshot({ path: 'test-results/profile-light-mobile.png', fullPage: true });
  await page.getByRole('button', { name: 'Reset preferences', exact: true }).click();
  await expect(page.getByLabel('Display name', { exact: true })).toHaveValue('');
});
test('profile works when API is unavailable and handles blocked browser storage', async ({
  page,
}) => {
  await page.addInitScript(() => {
    Storage.prototype.setItem = () => {
      throw new Error('Blocked');
    };
  });
  await page.route('**/api/dashboard', (r) => r.fulfill({ status: 503, json: {} }));
  await page.goto('http://127.0.0.1:5188');
  await page.getByRole('button', { name: 'My profile', exact: true }).click();
  await page.getByLabel('Preferred player tag', { exact: true }).fill('invalid!');
  await expect(page.getByRole('button', { name: 'Save preferences' })).toBeDisabled();
  await page.getByLabel('Preferred player tag', { exact: true }).fill('');
  await page.getByRole('button', { name: 'Save preferences' }).click();
  await expect(page.getByRole('alert')).toContainText('Browser storage is unavailable');
});
