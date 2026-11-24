import { Browser, BrowserContext, Page, expect, test } from '@playwright/test';
import { seedRoomDefaults } from './room-defaults';

const GUID_ROOM_PATH = /\/room\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;

interface Person {
  context: BrowserContext;
  page: Page;
}

async function newPerson(browser: Browser): Promise<Person> {
  const context = await browser.newContext();
  return { context, page: await context.newPage() };
}

async function createRoom(page: Page, name: string, alarmMinutes: number): Promise<string> {
  await seedRoomDefaults(page, { alarmMinutes });
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

const alarm = (page: Page) => page.locator('.head app-alarm-button button');

test('anyone starts the Alarm for the whole room and anyone cancels it', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', 7);
  await join(bob.page, roomUrl, 'Bob');
  await expect(alarm(ann.page)).toContainText('7:00');

  await alarm(bob.page).click();

  await expect(alarm(ann.page)).toHaveAttribute('aria-label', /^Cancel the alarm/);
  await expect(alarm(bob.page)).toHaveAttribute('aria-label', /^Cancel the alarm/);
  await expect(alarm(ann.page)).toContainText(/6:5\d|7:00/);

  await alarm(ann.page).click();

  await expect(alarm(bob.page)).toHaveAttribute('aria-label', 'Start the 7 minute alarm');
  await Promise.all([ann.context.close(), bob.context.close()]);
});

test('only the owner can change the Alarm, and turning it off removes the button for everyone', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', 5);
  await join(bob.page, roomUrl, 'Bob');
  await expect(bob.page.getByRole('button', { name: 'Settings' })).toHaveCount(0);

  await ann.page.getByRole('button', { name: 'Settings' }).click();
  const dialog = ann.page.getByRole('dialog');
  await dialog.getByLabel('Alarm length (minutes)').fill('9');
  await dialog.getByRole('button', { name: 'Save Settings' }).click();
  await expect(dialog).toHaveCount(0);
  await expect(alarm(bob.page)).toContainText('9:00');

  await ann.page.getByRole('button', { name: 'Settings' }).click();
  await dialog.locator('label.switch', { has: ann.page.locator('#alarm-enabled') }).click();
  await dialog.getByRole('button', { name: 'Save Settings' }).click();
  await expect(dialog).toHaveCount(0);

  await expect(alarm(bob.page)).toHaveCount(0);
  await expect(alarm(ann.page)).toHaveCount(0);
  await Promise.all([ann.context.close(), bob.context.close()]);
});

test('the countdown starts at the full length and only ever goes down', async ({ browser }) => {
  const ann = await newPerson(browser);
  const bob = await newPerson(browser);
  const roomUrl = await createRoom(ann.page, 'Ann', 7);
  await join(bob.page, roomUrl, 'Bob');
  for (const { page } of [ann, bob]) {
    await page.evaluate(() => {
      const seen: string[] = [];
      (window as unknown as { alarmTexts: string[] }).alarmTexts = seen;
      new MutationObserver(() => {
        const text = document.querySelector('.head app-alarm-button .text')?.textContent?.trim();
        if (text && seen.at(-1) !== text) {
          seen.push(text);
        }
      }).observe(document.body, { subtree: true, childList: true, characterData: true });
    });
  }

  await alarm(bob.page).click();
  await expect(alarm(ann.page)).toHaveAttribute('aria-label', /^Cancel the alarm/);
  await ann.page.waitForTimeout(2500);

  for (const { page } of [ann, bob]) {
    const texts = await page.evaluate(() => (window as unknown as { alarmTexts: string[] }).alarmTexts);
    const seconds = texts.map((text) => text.split(':').reduce((total, part) => total * 60 + Number(part), 0));
    expect(Math.max(...seconds)).toBeLessThanOrEqual(7 * 60);
    expect(seconds).toEqual([...seconds].sort((x, y) => y - x));
  }
  await Promise.all([ann.context.close(), bob.context.close()]);
});

test('a small window shows one Alarm button, also once the votes are shown', async ({ browser }) => {
  const ann = await newPerson(browser);
  await ann.page.setViewportSize({ width: 400, height: 800 });
  await seedRoomDefaults(ann.page, { countdownSeconds: 0 });
  await ann.page.goto('/');
  await ann.page.getByLabel('Your name').fill('Ann');
  await ann.page.getByRole('button', { name: 'Create room' }).click();
  await expect(ann.page).toHaveURL(GUID_ROOM_PATH);
  await expect(ann.page.locator('app-alarm-button button:visible')).toHaveCount(1);

  await ann.page.getByRole('button', { name: 'Show votes' }).click();

  await expect(ann.page.getByRole('button', { name: 'Table' })).toHaveAttribute('aria-pressed', 'true');
  await expect(ann.page.locator('app-alarm-button button:visible')).toHaveCount(1);
  await ann.context.close();
});

test('a person picks their own sound in the header, kept for the next visit', async ({ page }) => {
  await page.goto('/');
  const picker = page.getByLabel('Alarm sound');

  await expect(picker).toHaveValue('bell');
  await picker.selectOption('duck');
  await page.reload();

  await expect(picker).toHaveValue('duck');
});
