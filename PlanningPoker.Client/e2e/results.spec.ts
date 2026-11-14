import { Browser, BrowserContext, Page, expect, test } from '@playwright/test';
import { seedRoomDefaults } from './room-defaults';

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
  await seedRoomDefaults(page, { countdownSeconds: 2, cardSet: options.cardSet, showFireworks: options.fireworks });
  await page.goto('/');
  await page.getByLabel('Your name').fill(name);
  await page.getByRole('button', { name: 'Create room' }).click();
  await expect(page).toHaveURL(GUID_ROOM_PATH);
  return page.url();
}

async function join(page: Page, roomUrl: string, name: string, role?: string): Promise<void> {
  await page.goto(roomUrl);
  await page.getByLabel('Your name').fill(name);
  if (role) {
    await page.locator('.role-picker label', { hasText: role }).click();
  }
  await page.getByRole('button', { name: 'Join room' }).click();
  await expect(page.locator('li.person').filter({ hasText: name })).toBeVisible();
}

const pick = (page: Page, value: string) =>
  page
    .locator('label.card')
    .filter({ hasText: new RegExp(`^\\s*${value}\\s*$`) })
    .click();
const results = (page: Page) => page.locator('app-results');
const fireworks = (page: Page) => page.locator('app-fireworks canvas');
const verdict = (page: Page) => results(page).locator('.verdict .label');
const average = (page: Page) => results(page).locator('.ring .average');
const groups = (page: Page) => results(page).locator('.groups li');

async function closeAll(people: Person[]): Promise<void> {
  await Promise.all(people.map(({ context }) => context.close()));
}

test('different votes reveal the hero with the average and a split verdict, and Clear votes removes it', async ({
  browser,
}) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob', 'Tester');
  await expect(results(ann.page)).toHaveCount(0);

  await pick(ann.page, '3');
  await pick(bob.page, '5');

  for (const { page } of [ann, bob]) {
    await expect(results(page)).toHaveCount(1);
    await expect(average(page)).toHaveText('4');
    await expect(verdict(page)).toHaveText('Split vote');
    await expect(results(page).locator('.segment')).toHaveText([/3\s*1/, /5\s*1/]);
    await expect(groups(page)).toHaveText([/Developers avg\s*3/, /Testers avg\s*5/]);
  }
  await expect(fireworks(ann.page)).toHaveCount(0);

  await bob.page.getByRole('button', { name: 'Clear votes' }).click();
  await expect(results(ann.page)).toHaveCount(0);
  await expect(results(bob.page)).toHaveCount(0);

  await closeAll([ann, bob]);
});

test('an SRE vote shows in its own group average and counts in the overall average', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob', 'SRE');

  await pick(ann.page, '3');
  await pick(bob.page, '5');

  for (const { page } of [ann, bob]) {
    await expect(average(page)).toHaveText('4');
    await expect(groups(page)).toHaveText([/Developers avg\s*3/, /SREs avg\s*5/]);
  }

  await closeAll([ann, bob]);
});

test('a consensus is unanimous and launches the fireworks, and Clear votes removes them', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');

  await pick(ann.page, '5');
  await pick(bob.page, '5');

  await expect(verdict(ann.page)).toHaveText('Unanimous');
  await expect(results(ann.page).locator('.detail')).toContainText('100% agree on 5');
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

  await expect(verdict(ann.page)).toHaveText('Unanimous');
  await expect(fireworks(ann.page)).toHaveCount(0);

  await closeAll([ann, bob]);
});

test('a fist of five room shows the cards as images without text labels', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', { cardSet: 'fist-of-five' });
  await join(bob.page, roomUrl, 'Bob');

  await ann.page.getByRole('radio', { name: '2', exact: true }).check({ force: true });
  await bob.page.getByRole('radio', { name: '4', exact: true }).check({ force: true });

  await expect(average(ann.page)).toHaveText('3');
  // Each split segment is an image carrying its mark as the alternative text, not a text label.
  await expect(results(ann.page).locator('.segment img')).toHaveCount(2);
  await expect(results(ann.page).locator('.segment img').first()).toHaveAttribute('alt', /^[24]$/);
  await expect(results(ann.page).locator('.segment b')).toHaveCount(0);

  await closeAll([ann, bob]);
});

test('a late joiner sees the same results and no fireworks', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const cat = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');
  await pick(ann.page, '5');
  await pick(bob.page, '5');
  await expect(fireworks(ann.page)).toHaveCount(1);

  await join(cat.page, roomUrl, 'Cat');

  await expect(verdict(cat.page)).toHaveText('Unanimous');
  await expect(average(cat.page)).toHaveText('5');
  await expect(fireworks(cat.page)).toHaveCount(0);

  await closeAll([ann, bob, cat]);
});

test('the results look right in dark mode and on a phone without sideways scrolling', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');
  await ann.page.emulateMedia({ colorScheme: 'dark' });
  await ann.page.setViewportSize({ width: 375, height: 812 });

  await pick(ann.page, '3');
  await pick(bob.page, '8');

  await expect(results(ann.page)).toHaveCount(1);
  await expect(ann.page.locator('html')).toHaveAttribute('data-theme', 'dark');
  const overflow = await ann.page.evaluate(
    () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
  );
  expect(overflow).toBeLessThanOrEqual(0);

  await closeAll([ann, bob]);
});

const FIST_THEMES = [
  { name: 'an explicit Dark choice', stored: 'dark', scheme: 'light', theme: 'dark' },
  { name: 'Auto with a dark system', stored: null, scheme: 'dark', theme: 'dark' },
  { name: 'an explicit Light choice', stored: 'light', scheme: 'dark', theme: 'light' },
  { name: 'Auto with a light system', stored: null, scheme: 'light', theme: 'light' },
] as const;

/** The mask styling of one card image: a flat fill under a mask, no padding. */
const imageStyle = (element: Element) => {
  const computed = getComputedStyle(element);
  return {
    background: computed.backgroundColor,
    mask: computed.maskImage || computed.webkitMaskImage,
    padding: computed.padding,
  };
};

for (const { name, stored, scheme, theme } of FIST_THEMES) {
  test(`fist of five images are recoloured for the ${theme} theme with ${name}`, async ({ browser }) => {
    const context = await browser.newContext({ colorScheme: scheme, viewport: { width: 360, height: 800 } });
    const page = await context.newPage();
    if (stored) {
      await page.addInitScript((value) => localStorage.setItem('pp-theme', value), stored);
    }
    const bob = await newPerson(browser);
    const roomUrl = await createRoom(page, 'Ann', { cardSet: 'fist-of-five' });
    await join(bob.page, roomUrl, 'Bob');
    await expect(page.locator('html')).toHaveAttribute('data-theme', theme);

    await page.getByRole('radio', { name: '2', exact: true }).check({ force: true });
    await bob.page.getByRole('radio', { name: '4', exact: true }).check({ force: true });
    await expect(results(page)).toHaveCount(1);

    // Cards on the page surface follow the theme text colour; cards on a coloured surface (the selected card, the
    // vote card, the split bar) are white. Every one is a mask with no backing tile.
    const textColor = await page.evaluate(() => getComputedStyle(document.body).color);
    const expectations = [
      { selector: 'label.card:not(.selected) img', color: textColor },
      { selector: 'label.card.selected img', color: 'rgb(255, 255, 255)' },
      { selector: '.mark img', color: 'rgb(255, 255, 255)' },
      { selector: '.segment img', color: 'rgb(255, 255, 255)' },
    ];
    for (const { selector, color } of expectations) {
      // A phone shows one view at a time: the deck on Vote, the people and results on Table.
      const view = selector.startsWith('label.card') ? 'Vote' : 'Table';
      await page.getByRole('button', { name: view, exact: true }).click();
      const images = page.locator(selector);
      await expect(images.first()).toBeVisible();
      for (const image of await images.all()) {
        const style = await image.evaluate(imageStyle);
        expect(style.background).toBe(color);
        expect(style.mask).toContain('url(');
        expect(style.padding).toBe('0px');
      }
    }

    // Six cards wrapping on a 360px screen never scroll sideways.
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    );
    expect(overflow).toBeLessThanOrEqual(0);

    await closeAll([{ context, page }, bob]);
  });
}

test('a tie names no leader and an agreement names the winning mark', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');

  await pick(ann.page, '5');
  await pick(bob.page, '5');
  await expect(results(ann.page).locator('.detail')).toContainText('agree on 5');

  await ann.page.getByRole('button', { name: 'Clear votes' }).click();
  await expect(results(ann.page)).toHaveCount(0);
  await pick(ann.page, '3');
  await pick(bob.page, '5');
  // 3 and 5 tie.
  await expect(results(ann.page).locator('.detail')).toContainText('Top share 50%');
  await expect(results(ann.page).locator('.detail')).not.toContainText('agree on');

  await closeAll([ann, bob]);
});

test('a hidden vote is a card back the same size as the revealed card', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');
  const annCard = bob.page.locator('li.person', { hasText: 'Ann' }).locator('.mark');

  await pick(ann.page, '3');
  await expect(annCard).toHaveClass(/back/);
  const hidden = (await annCard.boundingBox())!;

  await pick(bob.page, '5');
  await expect(annCard).not.toHaveClass(/back/);
  await expect(annCard).toHaveText('3');
  const revealed = (await annCard.boundingBox())!;

  expect(revealed.width).toBeCloseTo(hidden.width, 0);
  expect(revealed.height).toBeCloseTo(hidden.height, 0);

  // The deck card you pick from is that same size too.
  const deck = (await bob.page.locator('label.card').first().boundingBox())!;
  expect(deck.width).toBeCloseTo(revealed.width, 0);
  expect(deck.height).toBeCloseTo(revealed.height, 0);

  await closeAll([ann, bob]);
});
