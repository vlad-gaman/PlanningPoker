import { expect, test } from '@playwright/test';

async function horizontalOverflow(page: import('@playwright/test').Page): Promise<number> {
  return page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
}

test('the theme picker applies dark, survives a reload and Auto follows the system', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'light' });
  await page.goto('/');
  const html = page.locator('html');
  await expect(html).toHaveAttribute('data-theme', 'light');

  await page.getByRole('radio', { name: 'Dark' }).click();
  await expect(html).toHaveAttribute('data-theme', 'dark');
  await page.reload();
  await expect(html).toHaveAttribute('data-theme', 'dark');
  await expect(page.getByRole('radio', { name: 'Dark' })).toHaveAttribute('aria-checked', 'true');

  await page.getByRole('radio', { name: 'Auto' }).click();
  await expect(html).toHaveAttribute('data-theme', 'light');
  await page.emulateMedia({ colorScheme: 'dark' });
  await expect(html).toHaveAttribute('data-theme', 'dark');
});

test('the footer leads to the credits page, which has icons, libraries and a way home', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('link', { name: 'Credits' }).click();

  await expect(page).toHaveURL(/\/credits$/);
  await expect(page.getByRole('heading', { name: 'Icons' })).toBeVisible();
  await expect(page.getByRole('link', { name: /Poker icons created by Freepik/ })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Libraries' })).toBeVisible();
  await expect(page).toHaveTitle('Credits - Planning Poker');

  await page.getByRole('link', { name: 'Back to Home' }).click();
  await expect(page).toHaveURL(/\/$/);
  await expect(page).toHaveTitle('Home - Planning Poker');

  await page.goto('/Credits');
  await expect(page).toHaveURL(/\/credits$/);
});

for (const [colorScheme, width] of [['light', 375], ['dark', 375], ['light', 360], ['dark', 360]] as const) {
  test(`navbar, footer and credits fit a ${width}px phone in ${colorScheme} mode`, async ({ page }) => {
    await page.emulateMedia({ colorScheme });
    await page.setViewportSize({ width, height: 812 });

    for (const path of ['/', '/credits']) {
      await page.goto(path);
      await expect(page.getByRole('radiogroup', { name: 'Theme' })).toBeVisible();
      await expect(page.getByRole('link', { name: 'Buy Me A Coffee' })).toBeVisible();
      expect(await horizontalOverflow(page)).toBeLessThanOrEqual(0);
    }
  });
}
