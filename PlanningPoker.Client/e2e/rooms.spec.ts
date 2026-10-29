import { Browser, BrowserContext, Page, expect, test } from '@playwright/test';

const FUN_ROOM_PATH = /\/room\/\d{1,2}_[\w-]+_[\w-]+_[\w-]+$/;
const GUID_ROOM_PATH = /\/room\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;

async function newPerson(browser: Browser): Promise<{ context: BrowserContext; page: Page }> {
  const context = await browser.newContext();
  return { context, page: await context.newPage() };
}

async function chooseRole(page: Page, role: string): Promise<void> {
  await page.getByRole('combobox', { name: 'Role' }).click();
  await page.getByRole('option', { name: role }).click();
}

async function createRoom(page: Page, name: string, options: { funName: boolean }): Promise<string> {
  await page.goto('/');
  await page.getByLabel('Your name').fill(name);
  await page.getByRole('button', { name: 'Create room' }).click();
  const dialog = page.getByRole('dialog');
  if (!options.funName) {
    await dialog.getByLabel('Use a fun room name').uncheck();
  }
  await dialog.getByRole('button', { name: 'Create room' }).click();
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

const rows = (page: Page) => page.locator('tr[mat-row]');

test('creating a room with a fun name shows the owner as the only participant', async ({ page }) => {
  await createRoom(page, 'Ann', { funName: true });

  await expect(rows(page)).toHaveCount(1);
  await expect(rows(page).first()).toContainText('Ann');
  await expect(rows(page).first()).toContainText('(owner)');
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
  await cat.page.getByLabel('Room name or link').fill(roomUrl);
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
  await expect(rows(bob.page).first()).toContainText('(owner)');

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
  await expect(page.getByRole('dialog')).toHaveCount(0);
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
