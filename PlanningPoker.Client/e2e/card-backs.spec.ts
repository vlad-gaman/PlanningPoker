import { Browser, BrowserContext, Page, expect, test } from '@playwright/test';
import { seedRoomDefaults } from './room-defaults';

const GUID_ROOM_PATH = /\/room\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;

interface Person {
  context: BrowserContext;
  page: Page;
}

async function newPerson(browser: Browser): Promise<Person> {
  const context = await browser.newContext();
  return { context, page: await context.newPage() };
}

async function createRoom(page: Page, name: string): Promise<string> {
  await seedRoomDefaults(page, { countdownSeconds: 30 });
  await page.goto('/');
  await page.getByLabel('Your name').fill(name);
  await page.getByRole('button', { name: 'Create room' }).click();
  await expect(page).toHaveURL(GUID_ROOM_PATH);
  return page.url();
}

async function join(page: Page, roomUrl: string, name: string): Promise<void> {
  await page.goto(roomUrl);
  await page.getByLabel('Your name').fill(name);
  await page.getByRole('button', { name: 'Join room' }).click();
  await expect(page.locator('li.person').filter({ hasText: name })).toBeVisible();
}

const mark = (page: Page, name: string) => page.locator('li.person').filter({ hasText: name }).locator('.mark');
const backTrigger = (page: Page) => page.getByRole('combobox', { name: 'Card back' });
/** Opens the card back dropdown and picks a design from it. */
async function chooseBack(page: Page, label: string): Promise<void> {
  await backTrigger(page).click();
  await page.getByRole('listbox', { name: 'Card back' }).getByRole('option', { name: label }).click();
}
const pick = (page: Page, value: string) =>
  page.locator('label.card').filter({ hasText: new RegExp(`^\s*${value}\s*$`) }).click();

test('a face-down vote shows its voter\'s chosen card back to everyone, and the choice survives a reload', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');

  await backTrigger(ann.page).click();
  await expect(ann.page.getByRole('listbox', { name: 'Card back' }).getByRole('option')).toHaveCount(6);
  await ann.page.getByRole('option', { name: 'Dots' }).click();
  await expect(backTrigger(ann.page)).toHaveText(/Dots/);

  await pick(ann.page, '5');
  await expect(mark(bob.page, 'Ann')).toHaveAttribute('data-back', 'dots');
  await expect(mark(ann.page, 'Ann')).toHaveAttribute('data-back', 'dots');

  // Changing the choice while the vote is face down shows at once to the others.
  await chooseBack(ann.page, 'Stripes');
  await expect(mark(bob.page, 'Ann')).toHaveAttribute('data-back', 'stripes');

  // The back is drawn, not a plain fill: the design's gradient reaches the face-down card.
  await expect(mark(bob.page, 'Ann')).toHaveCSS('background-image', /gradient/);

  // Bob never picked one, so his back is the classic default.
  await pick(bob.page, '8');
  await expect(mark(ann.page, 'Bob')).toHaveAttribute('data-back', 'classic');

  // The choice is kept for the browser: after a reload it is still checked, and the room shows it again.
  await ann.page.reload();
  await expect(backTrigger(ann.page)).toHaveText(/Stripes/);
  await expect(mark(bob.page, 'Ann')).toHaveAttribute('data-back', 'stripes');

  await Promise.all([ann.context.close(), bob.context.close()]);
});

test('a revealed vote turns over to a face without a card back', async ({ browser }) => {
  const ann = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await pick(ann.page, '5');
  await expect(mark(ann.page, 'Ann')).toHaveAttribute('data-back', 'classic');

  await ann.page.getByRole('button', { name: 'Show votes' }).click();
  await expect(mark(ann.page, 'Ann')).toHaveText('5');
  await expect(mark(ann.page, 'Ann')).not.toHaveAttribute('data-back', /.+/);
  expect(roomUrl).toMatch(GUID_ROOM_PATH);

  await ann.context.close();
});

test('the card back picker fits a phone-width screen', async ({ browser }) => {
  const context = await browser.newContext({ viewport: { width: 360, height: 800 } });
  const page = await context.newPage();
  await page.goto('/');

  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(overflow).toBeLessThanOrEqual(0);
  await expect(backTrigger(page)).toBeVisible();

  await context.close();
});
