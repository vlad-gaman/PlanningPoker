import { BrowserContext, Page, WebSocketRoute, expect, test } from '@playwright/test';
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

async function join(page: Page, roomUrl: string, name: string): Promise<void> {
  await page.goto(roomUrl);
  await page.getByLabel('Your name').fill(name);
  await page.getByRole('button', { name: 'Join room' }).click();
  await expect(page.locator('tr[mat-row]').filter({ hasText: name })).toBeVisible();
}

const row = (page: Page, name: string) => page.locator('tr[mat-row]').filter({ hasText: name });
const mark = (page: Page, name: string) => row(page, name).locator('.mark');
const card = (page: Page, value: string) => page.getByRole('radio', { name: value, exact: true });
const pick = (page: Page, value: string) =>
  page.locator('label.card').filter({ hasText: new RegExp(`^\\s*${value}\\s*$`) }).click();

test('a voter who loses the connection stays listed with their vote and is the same person when they come back', async ({ browser }) => {
  const ann: BrowserContext = await browser.newContext();
  const bob: BrowserContext = await browser.newContext();
  const annPage = await ann.newPage();
  const bobPage = await bob.newPage();

  // Ann's hub socket is routed. The first one is dropped on demand; the reconnect waits until the test lets it through,
  // so the server has run its disconnect for Ann before she comes back.
  const sockets: WebSocketRoute[] = [];
  let letReconnect: () => void = () => undefined;
  const reconnectAllowed = new Promise<void>((resolve) => (letReconnect = resolve));
  await annPage.routeWebSocket(/\/hubs\/room/, async (socket) => {
    if (sockets.length > 0) {
      await reconnectAllowed;
    }
    sockets.push(socket);
    socket.connectToServer();
  });

  const roomUrl = await createRoom(annPage, 'Ann');
  await join(bobPage, roomUrl, 'Bob');
  await pick(annPage, '5');
  await expect(card(annPage, '5')).toBeChecked();
  await expect(mark(bobPage, 'Ann')).toHaveText('▮');

  // Closing the page side also closes the server side, and a non-1000 code makes SignalR reconnect automatically.
  await sockets[0]!.close({ code: 4000, reason: 'connection lost' });

  // She is still online for the grace period, so nothing flickers on Bob's screen.
  await expect(row(bobPage, 'Ann')).toBeVisible();
  await expect(mark(bobPage, 'Ann')).toHaveText('▮');

  letReconnect();

  await expect(row(bobPage, 'Ann')).toBeVisible();
  await expect(mark(bobPage, 'Ann')).toHaveText('▮');
  await expect(mark(annPage, 'Ann')).toHaveText('▮');
  await expect(card(annPage, '5')).toBeChecked();

  await Promise.all([ann.close(), bob.close()]);
});
