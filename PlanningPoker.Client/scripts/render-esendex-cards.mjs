// Renders each Esendex Fibonacci card, large, in the light and dark themes, for comparing the art with the photos.
//
// Run it against a running app (the same server the e2e suite uses):
//   npm run build:api
//   ASPNETCORE_URLS=http://localhost:5100 ASPNETCORE_ENVIRONMENT=Production \
//     dotnet run --project ../PlanningPoker.Api --no-launch-profile
//   node scripts/render-esendex-cards.mjs
//
// Environment: BASE_URL (default http://localhost:5100), OUT_DIR (default the render folder below).
// Writes card-<value>-light.png and card-<value>-dark.png, where the value '?' becomes 'question'.

import { mkdirSync } from 'node:fs';
import { join } from 'node:path';
import { chromium } from '@playwright/test';

const BASE_URL = process.env.BASE_URL ?? 'http://localhost:5100';
const OUT_DIR = process.env.OUT_DIR ?? 'C:/Users/scien/AppData/Local/Temp/claude/esendex-renders';
const VALUES = ['1', '2', '3', '5', '8', '13', '30', '100', '?', 'coffee'];
/** Root font size in px: one card is 4em wide, so this sets the render's size (about 600 px wide). */
const ROOT_FONT_PX = 150;

mkdirSync(OUT_DIR, { recursive: true });

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1400, height: 1400 } });

await page.goto(BASE_URL + '/');
await page.getByRole('button', { name: 'Room settings' }).click();
await page.getByLabel('Card Set:').selectOption({ label: 'Esendex Fibonacci' });
await page.getByRole('button', { name: 'Save Settings' }).click();
await page.getByLabel('Your name').fill('Renderer');
await page.getByRole('button', { name: 'Create room' }).click();
await page.waitForURL(/\/room\/[^/]+$/);

const cards = page.getByRole('radiogroup', { name: 'Cards' });
await cards.getByRole('radio').first().waitFor();
await page.evaluate((px) => {
  document.documentElement.style.fontSize = `${px}px`;
}, ROOT_FONT_PX);

for (const theme of ['light', 'dark']) {
  await page.getByLabel('Theme').selectOption(theme);
  // Park the pointer off the cards so no render catches a hover state.
  await page.mouse.move(0, 0);
  for (const value of VALUES) {
    const card = cards.locator('label.card').filter({ has: page.locator(`input[value="${value}"]`) });
    await card.scrollIntoViewIfNeeded();
    const name = value === '?' ? 'question' : value;
    await card.screenshot({ path: join(OUT_DIR, `card-${name}-${theme}.png`) });
  }
}

await browser.close();
console.log(`Rendered ${VALUES.length * 2} cards to ${OUT_DIR}`);
