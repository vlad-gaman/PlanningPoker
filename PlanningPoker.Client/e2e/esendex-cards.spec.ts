import { expect, test } from '@playwright/test';

const ROOM_PATH = /\/room\/[^/]+$/;

/** Each card's value and the file of its illustration. The corners of the physical cards carry no numbers. */
const CARDS: [string, string][] = [
  ['1', '1'],
  ['2', '2'],
  ['3', '3'],
  ['5', '5'],
  ['8', '8'],
  ['13', '13'],
  ['30', '30'],
  ['100', '100'],
  ['?', 'q'],
  ['coffee', 'coffee'],
];

test('the Esendex Fibonacci set, chosen in room settings, draws its illustrated cards with no corner numbers', async ({
  page,
}) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Room settings' }).click();
  await page.getByLabel('Card Set').selectOption({ label: 'Esendex Fibonacci' });
  await page.getByRole('button', { name: 'Save Settings' }).click();

  await page.getByLabel('Your name').fill('Ann');
  await page.getByRole('button', { name: 'Create room' }).click();
  await expect(page).toHaveURL(ROOM_PATH);

  const cards = page.getByRole('radiogroup', { name: 'Cards' });
  await expect(cards.getByRole('radio')).toHaveCount(CARDS.length);
  for (const [value, file] of CARDS) {
    const card = cards
      .locator('label.card')
      .filter({ has: page.locator(`input[value="${value}"]`) });
    const img = card.locator('img');
    await expect(img).toHaveAttribute('alt', value);
    await expect(img).toHaveAttribute('src', `esendex/${file}.svg`);
    // No plain-text label on the card face (the number lives on the hanging tag inside the art).
    await expect(card.locator(':scope > span')).toHaveCount(0);
    await expect.poll(() => img.evaluate((el: HTMLImageElement) => el.naturalWidth)).toBeGreaterThan(0);
  }

  // Every illustration is visible in the other theme too.
  await page.getByRole('radiogroup', { name: 'Theme' }).getByRole('radio', { name: 'Dark' }).click();
  await expect(cards.locator('img')).toHaveCount(CARDS.length);
  for (const img of await cards.locator('img').all()) {
    await expect(img).toBeVisible();
  }

  // A picked card shows its illustration in the person's tile once the votes are shown.
  await cards
    .locator('label.card')
    .filter({ has: page.locator('input[value="30"]') })
    .click();
  await page.getByRole('button', { name: 'Show votes' }).click();
  const mark = page.locator('li.person', { hasText: 'Ann' }).locator('.mark');
  await expect(mark.locator('img')).toHaveAttribute('alt', '30');
});
