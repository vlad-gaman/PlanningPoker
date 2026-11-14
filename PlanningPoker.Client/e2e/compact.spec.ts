import { Browser, BrowserContext, Page, expect, test } from '@playwright/test';
import { seedRoomDefaults } from './room-defaults';

const GUID_ROOM_PATH = /\/room\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;

interface Person {
  context: BrowserContext;
  page: Page;
}

async function newPerson(browser: Browser, width: number): Promise<Person> {
  const context = await browser.newContext({ viewport: { width, height: 800 } });
  return { context, page: await context.newPage() };
}

async function createRoom(page: Page, name: string): Promise<string> {
  await seedRoomDefaults(page, { countdownSeconds: 2 });
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
  await expect(page.locator('li.person').filter({ hasText: name })).toBeAttached();
}

const tab = (page: Page, name: 'Vote' | 'Table') => page.getByRole('button', { name, exact: true });
const progress = (page: Page) => page.locator('.compact-progress');
const deck = (page: Page) => page.locator('app-card-picker .cards');
const people = (page: Page) => page.locator('ul.people');

test('a small window shows one view at a time: the deck to vote, the table once votes are shown', async ({ browser }) => {
  const ann = await newPerson(browser, 360);
  const bob = await newPerson(browser, 360);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');

  await expect(progress(ann.page)).toContainText('0 of 2 voted');
  await expect(tab(ann.page, 'Vote')).toHaveAttribute('aria-pressed', 'true');
  await expect(deck(ann.page)).toBeVisible();
  await expect(people(ann.page)).toBeHidden();

  await ann.page.locator('label.card').filter({ hasText: /^\s*5\s*$/ }).click();
  await expect(progress(ann.page)).toContainText('1 of 2 voted');

  await tab(ann.page, 'Table').click();
  await expect(people(ann.page)).toBeVisible();
  await expect(deck(ann.page)).toBeHidden();
  await tab(ann.page, 'Vote').click();
  await expect(deck(ann.page)).toBeVisible();

  // Showing the votes moves to the table, where the results are; clearing them returns to the deck.
  await ann.page.getByRole('button', { name: 'Show votes' }).click();
  await expect(ann.page.locator('app-results')).toBeVisible();
  await expect(tab(ann.page, 'Table')).toHaveAttribute('aria-pressed', 'true');
  await ann.page.getByRole('button', { name: 'Clear votes' }).click();
  await expect(tab(ann.page, 'Vote')).toHaveAttribute('aria-pressed', 'true');
  await expect(deck(ann.page)).toBeVisible();

  const overflow = await ann.page.evaluate(
    () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
  );
  expect(overflow).toBeLessThanOrEqual(0);

  await Promise.all([ann.context.close(), bob.context.close()]);
});

test('from 521px up the room is the full layout: no progress bar, deck and people together', async ({ browser }) => {
  const ann = await newPerson(browser, 521);
  await createRoom(ann.page, 'Ann');

  await expect(progress(ann.page)).toBeHidden();
  await expect(deck(ann.page)).toBeVisible();
  await expect(people(ann.page)).toBeVisible();
  await expect(ann.page.getByRole('button', { name: 'Clear votes' })).toContainText('Clear votes');

  await ann.context.close();
});
