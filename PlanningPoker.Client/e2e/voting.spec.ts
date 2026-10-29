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

/** Creates a room with the given countdown and returns its link. */
async function createRoom(page: Page, name: string, countdown: number): Promise<string> {
  await page.goto('/');
  await page.getByLabel('Your name').fill(name);
  await page.getByRole('button', { name: 'Create room' }).click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel('Use a fun room name').uncheck();
  await dialog.getByLabel('Countdown (seconds)').fill(String(countdown));
  await dialog.getByRole('button', { name: 'Create room' }).click();
  await expect(page).toHaveURL(GUID_ROOM_PATH);
  return page.url();
}

async function join(page: Page, roomUrl: string, name: string, role?: string): Promise<void> {
  await page.goto(roomUrl);
  await page.getByLabel('Your name').fill(name);
  if (role) {
    await page.getByRole('combobox', { name: 'Role' }).click();
    await page.getByRole('option', { name: role }).click();
  }
  await page.getByRole('button', { name: 'Join room' }).click();
  await expect(page.locator('tr[mat-row]').filter({ hasText: name })).toBeVisible();
}

const mark = (page: Page, name: string) => page.locator('tr[mat-row]').filter({ hasText: name }).locator('.mark');
const card = (page: Page, value: string) => page.getByRole('radio', { name: value, exact: true });
const pick = (page: Page, value: string) => page.locator('label.card').filter({ hasText: new RegExp(`^\\s*${value}\\s*$`) }).click();
const countdown = (page: Page) => page.locator('.countdown');

async function closeAll(people: Person[]): Promise<void> {
  await Promise.all(people.map(({ context }) => context.close()));
}

test('a vote shows as a hidden marker, then everyone voting reveals the marks after the countdown', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', 2);
  await join(bob.page, roomUrl, 'Bob');

  await pick(ann.page, '5');
  await expect(mark(bob.page, 'Ann')).toHaveText('▮');
  await expect(mark(bob.page, 'Bob')).toHaveText('');

  await pick(bob.page, '8');
  await expect(countdown(ann.page)).toContainText('Showing results in 2!');
  await expect(countdown(bob.page)).toContainText('Showing results in 2!');

  await expect(mark(ann.page, 'Ann')).toHaveText('5');
  await expect(mark(ann.page, 'Bob')).toHaveText('8');
  await expect(mark(bob.page, 'Ann')).toHaveText('5');
  await expect(countdown(ann.page)).toHaveCount(0);
  await expect(card(ann.page, '5')).toBeDisabled();
  await expect(card(bob.page, '1')).toBeDisabled();

  await closeAll([ann, bob]);
});

test('Clear votes empties marks, selection and countdown and enables the cards again', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', 2);
  await join(bob.page, roomUrl, 'Bob');
  await pick(ann.page, '5');
  await pick(bob.page, '8');
  await expect(mark(ann.page, 'Bob')).toHaveText('8');

  await bob.page.getByRole('button', { name: 'Clear votes' }).click();

  await expect(mark(ann.page, 'Ann')).toHaveText('');
  await expect(mark(ann.page, 'Bob')).toHaveText('');
  await expect(card(ann.page, '5')).toBeEnabled();
  await expect(card(ann.page, '5')).not.toBeChecked();
  await expect(card(bob.page, '8')).not.toBeChecked();
  await expect(countdown(ann.page)).toHaveCount(0);

  await closeAll([ann, bob]);
});

test('changing a vote during the countdown restarts it', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', 3);
  await join(bob.page, roomUrl, 'Bob');
  await pick(ann.page, '5');
  await pick(bob.page, '8');
  await expect(countdown(ann.page)).toContainText('Showing results in 3!');
  await expect(countdown(ann.page)).toContainText('Showing results in 2!');

  await pick(ann.page, '3');

  await expect(countdown(bob.page)).toContainText('Showing results in 3!');
  await expect(mark(bob.page, 'Ann')).toHaveText('3');

  await closeAll([ann, bob]);
});

test('Show votes reveals at once with one vote, and an observer sees the marks but cannot vote', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const cat = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', 2);
  await join(bob.page, roomUrl, 'Bob');
  await join(cat.page, roomUrl, 'Cat', 'Observer');
  await expect(card(cat.page, '5')).toBeDisabled();
  await pick(ann.page, '5');
  await expect(mark(cat.page, 'Ann')).toHaveText('▮');

  await ann.page.getByRole('button', { name: 'Show votes' }).click();

  await expect(mark(cat.page, 'Ann')).toHaveText('5');
  await expect(mark(cat.page, 'Bob')).toHaveText('');
  await expect(mark(cat.page, 'Cat')).toHaveText('');
  await expect(ann.page.getByRole('button', { name: 'Show votes' })).toBeDisabled();

  await closeAll([ann, bob, cat]);
});

test('a voter joining during the countdown cancels it and must vote before the reveal', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const dan = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', 3);
  await join(bob.page, roomUrl, 'Bob');
  await pick(ann.page, '5');
  await pick(bob.page, '8');
  await expect(countdown(ann.page)).toBeVisible();

  await join(dan.page, roomUrl, 'Dan');

  await expect(countdown(ann.page)).toHaveCount(0);
  await expect(countdown(bob.page)).toHaveCount(0);
  await expect(mark(bob.page, 'Ann')).toHaveText('▮');
  await expect(countdown(ann.page)).toHaveCount(0, { timeout: 4_000 });
  await expect(mark(ann.page, 'Ann')).toHaveText('▮');

  await pick(dan.page, '2');
  await expect(mark(ann.page, 'Dan')).toHaveText('2', { timeout: 10_000 });

  await closeAll([ann, bob, dan]);
});
