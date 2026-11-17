import { Browser, BrowserContext, Page, expect, test } from '@playwright/test';
import { seedRoomDefaults } from './room-defaults';

const FUN_ROOM_PATH = /\/room\/\d{1,2}_[\w-]+_[\w-]+_[\w-]+$/;
const GUID_ROOM_PATH = /\/room\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;

async function newPerson(browser: Browser): Promise<{ context: BrowserContext; page: Page }> {
  const context = await browser.newContext();
  return { context, page: await context.newPage() };
}

async function chooseRole(page: Page, role: string): Promise<void> {
  await page.locator('.role-picker label', { hasText: role }).click();
}

async function createRoom(page: Page, name: string, options: { funName: boolean }): Promise<string> {
  await seedRoomDefaults(page, { useFunRoomName: options.funName });
  await page.goto('/');
  await page.getByLabel('Your name').fill(name);
  await page.getByRole('button', { name: 'Create room' }).click();
  await expect(page).toHaveURL(options.funName ? FUN_ROOM_PATH : GUID_ROOM_PATH);
  return page.url();
}

async function joinByLink(page: Page, roomUrl: string, name: string, role?: string): Promise<void> {
  await page.goto(roomUrl);
  await page.getByLabel('Your name').fill(name);
  if (role) {
    await chooseRole(page, role);
  }
  await page.getByRole('button', { name: 'Join room' }).click();
}

const rows = (page: Page) => page.locator('li.person');

test('creating a room with a fun name shows the owner as the only participant', async ({ page }) => {
  await createRoom(page, 'Ann', { funName: true });

  await expect(rows(page)).toHaveCount(1);
  await expect(rows(page).first()).toContainText('Ann');
  await expect(rows(page).first().locator('.crown')).toBeVisible();
});

test('creating a room without a fun name uses a GUID', async ({ page }) => {
  await createRoom(page, 'Ann', { funName: false });

  await expect(rows(page)).toHaveCount(1);
});

test('people joining by link and from the home page show up for everyone', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const cat = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', { funName: true });

  await joinByLink(bob.page, roomUrl, 'Bob');
  await expect(rows(bob.page)).toHaveCount(2);
  await expect(rows(ann.page)).toHaveCount(2);

  await cat.page.goto('/');
  await cat.page.getByLabel('Your name').fill('Cat');
  await chooseRole(cat.page, 'Observer');
  await cat.page.getByLabel('Room Name').fill(roomUrl);
  await cat.page.getByRole('button', { name: 'Join room' }).click();
  await expect(rows(cat.page)).toHaveCount(3);
  await expect(rows(ann.page)).toHaveCount(3);
  await expect(rows(ann.page).filter({ hasText: 'Cat' })).toContainText('Observer');

  await Promise.all([ann, bob, cat].map(({ context }) => context.close()));
});

test('ownership moves to another member when the owner leaves', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', { funName: true });
  await joinByLink(bob.page, roomUrl, 'Bob');
  await expect(rows(ann.page)).toHaveCount(2);

  await ann.page.getByRole('button', { name: 'Leave room' }).click();
  await expect(ann.page).toHaveURL('/');

  await expect(rows(bob.page)).toHaveCount(1);
  await expect(rows(bob.page).first()).toContainText('Bob');
  await expect(rows(bob.page).first().locator('.crown')).toBeVisible();

  await Promise.all([ann, bob].map(({ context }) => context.close()));
});

test('a room disappears when its last person leaves', async ({ page }) => {
  const roomUrl = await createRoom(page, 'Ann', { funName: true });
  const roomId = roomUrl.split('/').pop()!;

  await page.getByRole('button', { name: 'Leave room' }).click();
  await expect(page).toHaveURL('/');
  await expect
    .poll(async () => (await page.request.get(`/api/rooms/${roomId}`)).status())
    .toBe(404);

  await page.goto(roomUrl);
  await expect(page).toHaveURL('/');
  await expect(page.getByRole('status')).toContainText('does not exist');
});

test('old-style /Room/{id} links open the room', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', { funName: true });

  await joinByLink(bob.page, roomUrl.replace('/room/', '/Room/'), 'Bob');

  await expect(rows(bob.page)).toHaveCount(2);
  await Promise.all([ann, bob].map(({ context }) => context.close()));
});

test('an invalid name is rejected on the home page', async ({ page }) => {
  await page.goto('/');
  await page.getByLabel('Your name').fill('A');
  await page.getByRole('button', { name: 'Create room' }).click();

  await expect(page.getByText('Use 2 to 20 characters.')).toBeVisible();
  await expect(page).toHaveURL('/');
});

test('creating a room asks nothing more and reuses the settings the owner saved last time', async ({ page }) => {
  await page.goto('/');
  await page.getByLabel('Your name').fill('Ann');
  await page.getByLabel('Use a fun name').uncheck();
  await page.getByRole('button', { name: 'Create room' }).click();
  await expect(page).toHaveURL(GUID_ROOM_PATH);
  await expect(page.getByRole('dialog')).toHaveCount(0);

  await page.getByRole('button', { name: 'Settings' }).click();
  await page.getByRole('dialog').getByLabel('Countdown (seconds)').fill('9');
  await page.getByRole('dialog').getByRole('button', { name: 'Save Settings' }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);

  await page.goto('/');
  await expect(page.getByLabel('Use a fun name')).not.toBeChecked();
  await page.getByRole('button', { name: 'Create room' }).click();
  await expect(page).toHaveURL(GUID_ROOM_PATH);
  await page.getByRole('button', { name: 'Settings' }).click();
  await expect(page.getByRole('dialog').getByLabel('Countdown (seconds)')).toHaveValue('9');
});

test('an invalid name is rejected when joining through a link', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', { funName: true });

  await bob.page.goto(roomUrl);
  await bob.page.getByLabel('Your name').fill('B');
  await bob.page.getByRole('button', { name: 'Join room' }).click();

  await expect(bob.page.getByText('Use 2 to 20 characters.')).toBeVisible();
  await expect(rows(ann.page)).toHaveCount(1);
  await Promise.all([ann, bob].map(({ context }) => context.close()));
});

/** Ann gets a session and creates a room through the API, then never opens it. Returns the room's link. */
async function createRoomWithoutOpeningIt(page: Page, name: string): Promise<string> {
  expect((await page.request.put('/api/me', { data: { name, personType: 'dev' } })).status()).toBe(200);
  const created = await page.request.post('/api/rooms', {
    data: {
      useFunRoomName: false,
      configuration: { cardSet: 'modified-fibonacci', countdownSeconds: 5, showFireworks: true, maxParticipants: 50 },
    },
  });
  expect(created.status()).toBe(201);
  const roomId = (await created.json()).data.id as string;
  return `/room/${roomId}`;
}

test('a room whose creator never joins is owned by the first person in and disappears when they leave', async ({
  browser,
}) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoomWithoutOpeningIt(ann.page, 'Ann');
  const roomId = roomUrl.split('/').pop()!;

  await joinByLink(bob.page, roomUrl, 'Bob');

  await expect(rows(bob.page)).toHaveCount(1);
  await expect(rows(bob.page).first()).toContainText('Bob');
  await expect(rows(bob.page).first().locator('.crown')).toBeVisible();
  await expect(bob.page.getByRole('button', { name: 'Settings' })).toBeVisible();

  await bob.page.getByRole('button', { name: 'Leave room' }).click();
  await expect(bob.page).toHaveURL('/');
  await expect
    .poll(async () => (await bob.page.request.get(`/api/rooms/${roomId}`)).status())
    .toBe(404);

  await Promise.all([ann, bob].map(({ context }) => context.close()));
});

test('a creator who opens the room after someone else joins as a regular member', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoomWithoutOpeningIt(ann.page, 'Ann');
  await joinByLink(bob.page, roomUrl, 'Bob');
  await expect(rows(bob.page)).toHaveCount(1);

  await ann.page.goto(roomUrl);
  await expect(rows(ann.page)).toHaveCount(2);

  await expect(rows(bob.page)).toHaveCount(2);
  await expect(rows(bob.page).filter({ hasText: 'Bob' }).locator('.crown')).toBeVisible();
  await expect(rows(bob.page).filter({ hasText: 'Ann' }).locator('.crown')).toHaveCount(0);
  await expect(bob.page.getByRole('button', { name: 'Settings' })).toBeVisible();
  await expect(ann.page.getByRole('button', { name: 'Settings' })).toHaveCount(0);

  await Promise.all([ann, bob].map(({ context }) => context.close()));
});

test('the selected role in the role picker takes the colour of that role', async ({ page }) => {
  await createRoom(page, 'Ann', { funName: true });
  const colourOf = (name: string) =>
    page.locator('.role-picker label', { hasText: name }).evaluate((el) => getComputedStyle(el).backgroundColor);

  const developer = await colourOf('Developer');
  await page.locator('.role-picker label', { hasText: 'Tester' }).click();
  await expect.poll(() => colourOf('Tester')).not.toBe(developer);
  expect(await colourOf('Developer')).not.toBe(await colourOf('Tester'));
});
