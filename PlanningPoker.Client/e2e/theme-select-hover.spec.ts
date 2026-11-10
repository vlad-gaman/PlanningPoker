import { expect, test } from '@playwright/test';

// Each entry is a theme the selector can show: the stored preference and the system scheme that
// the Auto preference resolves against.
const themes = [
  { name: 'Light', preference: 'light', systemScheme: 'dark' },
  { name: 'Dark', preference: 'dark', systemScheme: 'light' },
  { name: 'Auto with a light system', preference: 'auto', systemScheme: 'light' },
  { name: 'Auto with a dark system', preference: 'auto', systemScheme: 'dark' },
] as const;

for (const { name, preference, systemScheme } of themes) {
  test(`theme selector hover uses the ${name} hover fill without a box around the select`, async ({ page }) => {
    await page.emulateMedia({ colorScheme: systemScheme });
    await page.goto('/');
    await page.getByLabel('Theme').selectOption(preference);

    // The themed hover colour as the browser resolves --card-hover-bg in the active theme.
    const themedHover = await page.evaluate(() => {
      const probe = document.createElement('div');
      probe.style.backgroundColor = getComputedStyle(document.documentElement).getPropertyValue('--card-hover-bg');
      document.body.append(probe);
      const color = getComputedStyle(probe).backgroundColor;
      probe.remove();
      return color;
    });
    expect(themedHover).not.toBe('rgba(0, 0, 0, 0)');

    const toggle = page.locator('app-theme-select .theme-toggle');
    await page.getByLabel('Theme').hover();

    await expect(toggle).toHaveCSS('background-color', themedHover);
    await expect(page.getByLabel('Theme')).toHaveCSS('background-color', 'rgba(0, 0, 0, 0)');
  });
}
