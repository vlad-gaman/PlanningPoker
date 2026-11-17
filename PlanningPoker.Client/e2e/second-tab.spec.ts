import { Page, expect, test } from '@playwright/test';
import { seedRoomDefaults } from './room-defaults';

const GUID_ROOM_PATH = /\/room\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;

async function createRoom(page: Page, name: string): Promise<string> {
  await seedRoomDefaults(page, {});
  await page.goto('/');
  await page.getByLabel('Your name').fill(name);
  await page.getByRole('button', { name: 'Create room' }).click();
  await expect(page).toHaveURL(GUID_ROOM_PATH);
  return page.url();
}

const card = (page: Page, value: string) => page.getByRole('radio', { name: value, exact: true });
const pick = (page: Page, value: string) =>
  page.locator('label.card').filter({ hasText: new RegExp(`^\s*${value}\s*$`) }).click();

test('a second tab of a person who voted shows their card and takes the room from the first tab', async ({ browser }) => {
  const ann = await browser.newContext();
  const bob = await browser.newContext();
  const first = await ann.newPage();
  const roomUrl = await createRoom(first, 'Ann');

  // Bob joins and does not vote, so the votes stay hidden.
  const bobPage = await bob.newPage();
  await bobPage.goto(roomUrl);
  await bobPage.getByLabel('Your name').fill('Bob');
  await bobPage.getByRole('button', { name: 'Join room' }).click();
  await expect(bobPage.locator('li.person').filter({ hasText: 'Bob' })).toBeVisible();

  await pick(first, '5');
  await expect(card(first, '5')).toBeChecked();

  const second = await ann.newPage();
  await second.goto(roomUrl);

  await expect(card(second, '5')).toBeChecked();
  await expect(first).toHaveURL(/\/$/);
  await expect(first.getByText('opened in another tab')).toBeVisible();

  await ann.close();
  await bob.close();
});
