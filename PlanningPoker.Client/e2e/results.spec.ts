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
    await page.getByLabel('Role').selectOption({ label: role });
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

test('different votes reveal four charts with the averages, and Clear votes removes them', async ({
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
    await expect(charts(page)).toHaveCount(4);
    await expect(page.locator('app-results figcaption')).toHaveText([
      'All votes',
      'Developers votes',
      'Testers votes',
      'SRE votes',
    ]);
    await expect(averages(page)).toHaveText(['4', '3', '5', '']);
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

test('an SRE vote gets its own chart and counts in the All average', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob', 'SRE');

  await pick(ann.page, '3');
  await pick(bob.page, '5');

  for (const { page } of [ann, bob]) {
    await expect(charts(page)).toHaveCount(4);
    await expect(page.locator('app-results figcaption')).toHaveText([
      'All votes',
      'Developers votes',
      'Testers votes',
      'SRE votes',
    ]);
    await expect(averages(page)).toHaveText(['4', '3', '', '5']);
  }

  await closeAll([ann, bob]);
});

test('a consensus launches the fireworks, and Clear votes removes them', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');

  await pick(ann.page, '5');
  await pick(bob.page, '5');

  await expect(charts(ann.page)).toHaveCount(4);
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

  await expect(charts(ann.page)).toHaveCount(4);
  await expect(fireworks(ann.page)).toHaveCount(0);

  await closeAll([ann, bob]);
});

test('a fist of five room charts the cards as images without text labels', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', { cardSet: 'fist-of-five' });
  await join(bob.page, roomUrl, 'Bob');

  await ann.page.getByRole('radio', { name: '2', exact: true }).check({ force: true });
  await bob.page.getByRole('radio', { name: '4', exact: true }).check({ force: true });

  await expect(charts(ann.page)).toHaveCount(4);
  // The labels are images drawn on the canvas, so the page offers the marks only through the chart's text alternative.
  await expect(ann.page.locator('app-results canvas').first()).toHaveAttribute(
    'aria-label',
    'All votes: 2 50%, 4 50%',
  );
  await expect(averages(ann.page)).toHaveText(['3', '3', '', '']);

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

  await expect(charts(cat.page)).toHaveCount(4);
  await expect(averages(cat.page)).toHaveText(['5', '5', '', '']);
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

  await expect(charts(ann.page)).toHaveCount(4);
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

/** The average brightness of the card images drawn under a chart's axis, or null while none is drawn yet. */
const labelStripBrightness = (page: Page) =>
  charts(page)
    .first()
    .locator('canvas')
    .evaluate((canvas: HTMLCanvasElement) => {
      // The images sit just under the axis; the bars are coloured and the canvas is otherwise transparent, so the
      // opaque grey pixels in the lower part of the canvas are the images.
      const strip = Math.round(60 * (canvas.height / canvas.clientHeight));
      const { data } = canvas.getContext('2d')!.getImageData(0, canvas.height - strip, canvas.width, strip);
      let sum = 0;
      let count = 0;
      for (let i = 0; i < data.length; i += 4) {
        const grey = Math.abs(data[i] - data[i + 1]) < 8 && Math.abs(data[i + 1] - data[i + 2]) < 8;
        if (data[i + 3] > 200 && grey) {
          sum += (data[i] + data[i + 1] + data[i + 2]) / 3;
          count++;
        }
      }
      return count === 0 ? null : sum / count;
    });

for (const { name, stored, scheme, theme } of FIST_THEMES) {
  test(`fist of five images follow the ${theme} theme text colour with ${name}`, async ({ browser }) => {
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
    await expect(charts(page)).toHaveCount(4);

    // (a) The picker and the vote column are recoloured by a mask over the text colour, with no backing tile.
    const textColor = await page.evaluate(() => getComputedStyle(document.body).color);
    for (const selector of ['label.card img', '.mark img']) {
      const images = page.locator(selector);
      await expect(images.first()).toBeVisible();
      for (const image of await images.all()) {
        const style = await image.evaluate((element) => {
          const computed = getComputedStyle(element);
          return {
            background: computed.backgroundColor,
            mask: computed.maskImage || computed.webkitMaskImage,
            padding: computed.padding,
          };
        });
        expect(style.background).toBe(textColor);
        expect(style.mask).toContain('url(');
        expect(style.padding).toBe('0px');
      }
    }

    // (b) The chart's label strip holds light pixels in dark mode and dark pixels in light mode.
    await expect.poll(() => labelStripBrightness(page)).not.toBeNull();
    const brightness = await labelStripBrightness(page);
    expect(brightness).not.toBeNull();
    if (theme === 'dark') {
      expect(brightness!).toBeGreaterThan(180);
    } else {
      expect(brightness!).toBeLessThan(80);
    }

    // (d) Six cards wrapping on a 360px screen never scroll sideways.
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    );
    expect(overflow).toBeLessThanOrEqual(0);

    await closeAll([{ context, page }, bob]);
  });
}

test('the chart label images change colour when the theme changes while results are showing', async ({
  browser,
}) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', { cardSet: 'fist-of-five' });
  await join(bob.page, roomUrl, 'Bob');
  await ann.page.getByRole('radio', { name: '2', exact: true }).check({ force: true });
  await bob.page.getByRole('radio', { name: '4', exact: true }).check({ force: true });
  await expect(charts(ann.page)).toHaveCount(4);

  await ann.page.getByLabel('Theme').selectOption('light');
  await expect.poll(async () => (await labelStripBrightness(ann.page)) ?? 255).toBeLessThan(80);
  await ann.page.getByLabel('Theme').selectOption('dark');
  await expect.poll(async () => (await labelStripBrightness(ann.page)) ?? 0).toBeGreaterThan(180);

  await closeAll([ann, bob]);
});

test('the highest mark is named under the average, and a tie names none', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');

  await pick(ann.page, '5');
  await pick(bob.page, '5');
  await expect(charts(ann.page)).toHaveCount(4);
  await expect(charts(ann.page).first().locator('.most')).toHaveText('Most votes: 5');
  await expect(charts(ann.page).first().locator('.most .value')).toHaveText('5');

  await ann.page.getByRole('button', { name: 'Clear votes' }).click();
  await expect(charts(ann.page)).toHaveCount(0);
  await pick(ann.page, '3');
  await pick(bob.page, '5');
  await expect(charts(ann.page)).toHaveCount(4);
  // 3 and 5 tie in All.
  await expect(charts(ann.page).first().locator('.most')).toHaveCount(0);

  await closeAll([ann, bob]);
});

test('a group without votes says "No votes" and draws no canvas', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');

  await pick(ann.page, '3');
  await pick(bob.page, '5');

  await expect(charts(ann.page)).toHaveCount(4);
  // Testers and SRE are empty.
  await expect(ann.page.locator('app-results .no-votes')).toHaveText(['No votes', 'No votes']);
  await expect(ann.page.locator('app-results canvas')).toHaveCount(2);
  await expect(charts(ann.page).nth(2).locator('canvas')).toHaveCount(0);
  await expect(charts(ann.page).nth(3).locator('canvas')).toHaveCount(0);
  await expect(charts(ann.page).nth(2).locator('.average .value')).toHaveText('');

  await closeAll([ann, bob]);
});

test('the chart colour is #1b6ec2 in light mode and #4dabf7 in dark mode', async ({ browser }) => {
  const ann = await newPerson(browser);
  const chartColor = () =>
    ann.page.evaluate(() =>
      getComputedStyle(document.documentElement).getPropertyValue('--chart-color-primary').trim(),
    );
  await createRoom(ann.page, 'Ann');

  await ann.page.getByLabel('Theme').selectOption('light');
  await expect.poll(chartColor).toBe('#1b6ec2');
  await ann.page.getByLabel('Theme').selectOption('dark');
  await expect.poll(chartColor).toBe('#4dabf7');

  await closeAll([ann]);
});

test('the charts are at most 176px tall on a desktop and never scroll sideways on a 375px phone', async ({
  browser,
}) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann');
  await join(bob.page, roomUrl, 'Bob');
  await pick(ann.page, '3');
  await pick(bob.page, '5');
  await expect(charts(ann.page)).toHaveCount(4);

  const box = ann.page.locator('app-results .canvas-box').first();
  expect((await box.boundingBox())!.height).toBeLessThanOrEqual(176);

  await ann.page.setViewportSize({ width: 375, height: 812 });
  expect((await box.boundingBox())!.height).toBeLessThanOrEqual(176);
  const overflow = await ann.page.evaluate(
    () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
  );
  expect(overflow).toBeLessThanOrEqual(0);

  await closeAll([ann, bob]);
});
