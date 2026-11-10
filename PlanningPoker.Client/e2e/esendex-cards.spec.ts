import { expect, test } from '@playwright/test';

const ROOM_PATH = /\/room\/[^/]+$/;

/**
 * Each card's value and the text its illustration draws, in order. Only the value is drawn on the art (or the
 * scale of the thermometer); the corners of the physical cards carry no numbers, so nothing else is listed.
 */
const DRAWN_TEXT: [string, string[]][] = [
  ['1', ['1']],
  ['2', ['2']],
  ['3', ['3']],
  ['5', ['5']],
  ['8', ['8']],
  ['13', ['13']],
  ['30', ['30', '20', '10', '0', '30']],
  ['100', ['100']],
  ['?', ['?']],
  ['coffee', []],
];

test('the Esendex Fibonacci set, chosen in room settings, draws its illustrated cards with no corner numbers', async ({
  page,
}) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Room settings' }).click();
  await page.getByLabel('Card Set:').selectOption({ label: 'Esendex Fibonacci' });
  await page.getByRole('button', { name: 'Save Settings' }).click();

  await page.getByLabel('Your name').fill('Ann');
  await page.getByRole('button', { name: 'Create room' }).click();
  await expect(page).toHaveURL(ROOM_PATH);

  const cards = page.getByRole('radiogroup', { name: 'Cards' });
  await expect(cards.getByRole('radio')).toHaveCount(DRAWN_TEXT.length);
  for (const [value, texts] of DRAWN_TEXT) {
    const card = cards
      .locator('label.card')
      .filter({ has: page.locator(`input[value="${value}"]`) });
    await expect(card.locator('svg')).toHaveAttribute('aria-label', value);
    await expect(card.locator('span')).toHaveCount(0);
    expect(await card.locator('svg text').allTextContents()).toEqual(texts);
  }

  // Every illustration is visible in the other theme too.
  await page.getByLabel('Theme').selectOption('dark');
  await expect(cards.locator('svg')).toHaveCount(DRAWN_TEXT.length);
  for (const svg of await cards.locator('svg').all()) {
    await expect(svg).toBeVisible();
  }

  // A picked card shows its illustration in the participants table once the votes are shown.
  await cards
    .locator('label.card')
    .filter({ has: page.locator('input[value="30"]') })
    .click();
  await page.getByRole('button', { name: 'Show votes' }).click();
  const mark = page.locator('tr[mat-row]').filter({ hasText: 'Ann' }).locator('.mark');
  await expect(mark.locator('svg')).toHaveAttribute('aria-label', '30');
});
