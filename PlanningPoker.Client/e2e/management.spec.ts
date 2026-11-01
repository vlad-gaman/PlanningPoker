import { Browser, BrowserContext, Page, expect, test } from '@playwright/test';

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
  await page.goto('/');
  await page.getByLabel('Your name').fill(name);
  await page.getByRole('button', { name: 'Create room' }).click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel('Use a fun room name').uncheck();
  await dialog.getByLabel('Countdown (seconds)').fill('2');
  await dialog.getByRole('button', { name: 'Create room' }).click();
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
const role = (page: Page, label: string) => page.getByRole('radiogroup', { name: 'Your role' }).getByLabel(label);
const settings = (page: Page) => page.getByRole('button', { name: 'Settings' });

test('changing the role updates everyone, and an observer cannot vote', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');

  await role(bob.page, 'Observer').check();

  await expect(row(ann.page, 'Bob')).toContainText('Observer');
  await expect(bob.page.getByRole('radio', { name: '5', exact: true })).toBeDisabled();

  await Promise.all([ann.context.close(), bob.context.close()]);
});

test('only the owner sees Settings; saving them reaches every member', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');
  await expect(settings(bob.page)).toHaveCount(0);

  await settings(ann.page).click();
  const dialog = ann.page.getByRole('dialog');
  await dialog.getByLabel('Countdown (seconds)').fill('7');
  await dialog.getByRole('button', { name: 'Save settings' }).click();
  await expect(dialog).toHaveCount(0);

  await ann.page.getByRole('button', { name: 'Settings' }).click();
  await expect(ann.page.getByRole('dialog').getByLabel('Countdown (seconds)')).toHaveValue('7');

  await Promise.all([ann.context.close(), bob.context.close()]);
});

test('transferring ownership moves the Settings button to the new owner', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');

  await settings(ann.page).click();
  const dialog = ann.page.getByRole('dialog');
  await dialog.getByRole('combobox', { name: 'New owner' }).click();
  await ann.page.getByRole('option', { name: 'Bob' }).click();
  await dialog.getByRole('button', { name: 'Transfer ownership' }).click();

  await expect(settings(bob.page)).toBeVisible();
  await expect(settings(ann.page)).toHaveCount(0);
  await expect(row(bob.page, 'Bob')).toContainText('(owner)');

  await Promise.all([ann.context.close(), bob.context.close()]);
});
