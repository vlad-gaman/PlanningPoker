import { BrowserContext, Page, expect, test } from '@playwright/test';
import { seedRoomDefaults } from './room-defaults';

const GUID_ROOM_PATH = /\/room\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;
/** The server's closing window is 5 s; a reload is back well inside it. */
const PAST_CLOSING_WINDOW_MS = 7_000;

async function createRoom(page: Page, name: string): Promise<string> {
  await seedRoomDefaults(page, {});
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

const row = (page: Page, name: string) => page.locator('li.person').filter({ hasText: name });

async function twoPeople(browser: import('@playwright/test').Browser): Promise<{ ann: BrowserContext; bob: BrowserContext; annPage: Page; bobPage: Page }> {
  const ann = await browser.newContext();
  const bob = await browser.newContext();
  const annPage = await ann.newPage();
  const bobPage = await bob.newPage();
  const roomUrl = await createRoom(annPage, 'Ann');
  await join(bobPage, roomUrl, 'Bob');
  await expect(row(annPage, 'Bob')).toBeVisible();
  return { ann, bob, annPage, bobPage };
}

test('a person who closes the tab is shown offgrid at once and is gone from the room soon after', async ({ browser }) => {
  const { ann, bob, annPage, bobPage } = await twoPeople(browser);

  await annPage.close();

  await expect(row(bobPage, 'Ann')).toHaveClass(/offgrid-person/, { timeout: 5_000 });
  await expect(row(bobPage, 'Ann')).toHaveCount(0, { timeout: PAST_CLOSING_WINDOW_MS + 5_000 });

  await Promise.all([ann.close(), bob.close()]);
});

test('a person who reloads the page stays in the room, past the closing window', async ({ browser }) => {
  const { ann, bob, annPage, bobPage } = await twoPeople(browser);

  await annPage.reload();
  await expect(row(annPage, 'Ann')).toBeVisible();
  await bobPage.waitForTimeout(PAST_CLOSING_WINDOW_MS);

  await expect(row(bobPage, 'Ann')).toBeVisible();
  await expect(row(bobPage, 'Ann')).not.toHaveClass(/offgrid-person/);

  await Promise.all([ann.close(), bob.close()]);
});
