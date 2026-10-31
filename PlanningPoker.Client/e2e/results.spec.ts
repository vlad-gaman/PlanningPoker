import { Browser, BrowserContext, Page, expect, test } from '@playwright/test';

const GUID_ROOM_PATH = /\/room\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;

interface Person {
  context: BrowserContext;
  page: Page;
}

interface RoomOptions {
  cardSet?: string;
  fireworks?: boolean;
}

async function newPerson(browser: Browser): Promise<Person> {
  const context = await browser.newContext();
  return { context, page: await context.newPage() };
}

/** Creates a room with a two second countdown and returns its link. */
async function createRoom(page: Page, name: string, options: RoomOptions = {}): Promise<string> {
  await page.goto('/');
  await page.getByLabel('Your name').fill(name);
  await page.getByRole('button', { name: 'Create room' }).click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel('Use a fun room name').uncheck();
  await dialog.getByLabel('Countdown (seconds)').fill('2');
  if (options.cardSet) {
    await dialog.getByRole('combobox', { name: 'Card set' }).click();
    await page.getByRole('option', { name: options.cardSet }).click();
  }
  if (options.fireworks === false) {
    await dialog.getByLabel('Show fireworks').uncheck();
  }
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

const pick = (page: Page, value: string) =>
  page
    .locator('label.card')
    .filter({ hasText: new RegExp(`^\\s*${value}\\s*$`) })
    .click();
const charts = (page: Page) => page.locator('app-results-chart');
const fireworks = (page: Page) => page.locator('app-fireworks canvas');
const averages = (page: Page) => page.locator('app-results-chart .average .value');

async function closeAll(people: Person[]): Promise<void> {
  await Promise.all(people.map(({ context }) => context.close()));
}

test('different votes reveal three charts with the averages, and Clear votes removes them', async ({
  browser,
}) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob', 'Tester');
  await expect(charts(ann.page)).toHaveCount(0);

  await pick(ann.page, '3');
  await pick(bob.page, '5');

  for (const { page } of [ann, bob]) {
    await expect(charts(page)).toHaveCount(3);
    await expect(page.locator('app-results figcaption')).toHaveText([
      'All votes',
      'Developers votes',
      'Testers votes',
    ]);
    await expect(averages(page)).toHaveText(['4', '3', '5']);
    await expect(page.locator('app-results canvas').first()).toHaveAttribute(
      'aria-label',
      'All votes: 3 50%, 5 50%',
    );
  }
  await expect(fireworks(ann.page)).toHaveCount(0);

  await bob.page.getByRole('button', { name: 'Clear votes' }).click();
  await expect(charts(ann.page)).toHaveCount(0);
  await expect(charts(bob.page)).toHaveCount(0);

  await closeAll([ann, bob]);
});

test('a consensus launches the fireworks, and Clear votes removes them', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');

  await pick(ann.page, '5');
  await pick(bob.page, '5');

  await expect(charts(ann.page)).toHaveCount(3);
  await expect(fireworks(ann.page)).toHaveCount(1);
  await expect(fireworks(bob.page)).toHaveCount(1);

  await ann.page.getByRole('button', { name: 'Clear votes' }).click();
  await expect(fireworks(ann.page)).toHaveCount(0);
  await expect(fireworks(bob.page)).toHaveCount(0);

  await closeAll([ann, bob]);
});

test('a room with fireworks turned off shows none on consensus', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', { fireworks: false });
  await join(bob.page, roomUrl, 'Bob');

  await pick(ann.page, '5');
  await pick(bob.page, '5');

  await expect(charts(ann.page)).toHaveCount(3);
  await expect(fireworks(ann.page)).toHaveCount(0);

  await closeAll([ann, bob]);
});

test('a fist of five room charts the cards as images without text labels', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', { cardSet: 'Fist of Five' });
  await join(bob.page, roomUrl, 'Bob');

  await ann.page.getByRole('radio', { name: '2', exact: true }).check({ force: true });
  await bob.page.getByRole('radio', { name: '4', exact: true }).check({ force: true });

  await expect(charts(ann.page)).toHaveCount(3);
  // The labels are images drawn on the canvas, so the page offers the marks only through the chart's text alternative.
  await expect(ann.page.locator('app-results canvas').first()).toHaveAttribute(
    'aria-label',
    'All votes: 2 50%, 4 50%',
  );
  await expect(averages(ann.page)).toHaveText(['3', '3', '']);

  await closeAll([ann, bob]);
});

test('a late joiner sees the same charts and no fireworks', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const cat = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');
  await pick(ann.page, '5');
  await pick(bob.page, '5');
  await expect(fireworks(ann.page)).toHaveCount(1);

  await join(cat.page, roomUrl, 'Cat');

  await expect(charts(cat.page)).toHaveCount(3);
  await expect(averages(cat.page)).toHaveText(['5', '5', '']);
  await expect(fireworks(cat.page)).toHaveCount(0);

  await closeAll([ann, bob, cat]);
});

test('the results look right in dark mode and on a phone without sideways scrolling', async ({
  browser,
}) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');
  await ann.page.emulateMedia({ colorScheme: 'dark' });
  await ann.page.setViewportSize({ width: 375, height: 812 });

  await pick(ann.page, '3');
  await pick(bob.page, '8');

  await expect(charts(ann.page)).toHaveCount(3);
  await expect(ann.page.locator('html')).toHaveAttribute('data-theme', 'dark');
  const overflow = await ann.page.evaluate(
    () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
  );
  expect(overflow).toBeLessThanOrEqual(0);

  await closeAll([ann, bob]);
});
