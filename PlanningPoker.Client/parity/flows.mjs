// Drives one app through the states. `shot(page, name)` is called at each state; three browser contexts per app:
// Alice (owner, developer), Bob (developer, votes 5), Carol (tester, votes 8).

async function contexts(browser, options) {
  const all = [await browser.newContext(options), await browser.newContext(options), await browser.newContext(options)];
  const pages = await Promise.all(all.map((c) => c.newPage()));
  return { all, pages };
}

export async function oldFlow(browser, url, options, shot) {
  const { all, pages } = await contexts(browser, options);
  const [a, b, c] = pages;
  await a.goto(url);
  await shot(a, 'home');
  await a.click('#room-creation-settings-btn');
  await a.waitForTimeout(600);
  await shot(a, 'home-dialog');
  await a.goto(url);
  await a.fill('#name', 'Alice');
  await a.click('input[value="Create room"]');
  await a.waitForURL(/\/Room\//);
  await a.waitForSelector('#people-dev tr, #people-dev td');
  await shot(a, 'room-lobby');
  const room = a.url();
  for (const [page, name, type] of [[b, 'Bob', 'isDev'], [c, 'Carol', 'isTest']]) {
    await page.goto(room);
    if (name === 'Bob') {
      await shot(page, 'join');
    }
    await page.fill('#name', name);
    await page.locator(`label[for=${type}]`).click();
    await page.click('input[value="Join room"]');
    await page.waitForSelector('input[name=mark]', { state: 'attached' });
  }
  await b.locator('label[for="5"]').first().click();
  await c.locator('label[for="8"]').first().click();
  await a.waitForTimeout(500);
  await shot(a, 'room-voting');
  await a.click('#show-votes');
  await a.waitForTimeout(1500);
  await shot(a, 'room-revealed');
  await a.click('#room-settings-btn');
  await a.waitForTimeout(600);
  await shot(a, 'room-settings');
  await a.goto(`${url}/Credits`);
  await shot(a, 'credits');
  await Promise.all(all.map((x) => x.close()));
}

const NO_ROOM = '00000000-0000-4000-8000-000000000000';

// Second scenario, for the states the main flow cannot reach: Alice (owner, dev), Bob (dev) and Carol (observer) share a
// room; Alice and Bob both vote 5, which starts the countdown and then reveals a consensus.
export async function oldExtraFlow(browser, url, options, shot) {
  const { all, pages } = await contexts(browser, options);
  const [a, b, c] = pages;
  await a.goto(url);
  await a.fill('#name', 'Alice');
  await a.click('input[value="Join room"]');
  await a.waitForTimeout(500);
  await shot(a, 'home-join-error');
  await a.goto(`${url}/Room/${NO_ROOM}`);
  await a.waitForTimeout(500);
  await shot(a, 'room-not-found');
  await a.goto(url);
  await a.fill('#name', 'Alice');
  await a.click('input[value="Create room"]');
  await a.waitForURL(/\/Room\//);
  await a.waitForSelector('input[name=mark]', { state: 'attached' });
  const room = a.url();
  for (const [page, name, type] of [[b, 'Bob', 'isDev'], [c, 'Carol', 'isObserver']]) {
    await page.goto(room);
    await page.fill('#name', name);
    await page.locator(`label[for=${type}]`).click();
    await page.click('input[value="Join room"]');
    await page.waitForSelector('input[name=mark]', { state: 'attached' });
  }
  await a.waitForTimeout(500);
  await shot(c, 'room-observer');
  await shot(b, 'room-nonowner');
  await b.locator('label[for=isTest]').click();
  await a.waitForTimeout(500);
  await shot(b, 'room-role-change');
  await b.locator('label[for=isDev]').click();
  await a.click('#room-settings-btn');
  await a.waitForTimeout(600);
  await a.selectOption('#transfer-ownership-select', { label: 'Bob' });
  await shot(a, 'room-settings-transfer');
  await a.click('#cancel-settings');
  await a.waitForTimeout(400);
  await a.locator('label[for="5"]').first().click();
  await b.locator('label[for="5"]').first().click();
  await a.waitForSelector('#show-votes-countdown', { state: 'visible' });
  await shot(a, 'room-countdown');
  await a.waitForSelector('#show-votes-countdown', { state: 'hidden', timeout: 15000 });
  await a.waitForTimeout(1500);
  await shot(a, 'room-fireworks');
  await Promise.all(all.map((x) => x.close()));
}

export async function newExtraFlow(browser, url, options, shot) {
  const { all, pages } = await contexts(browser, options);
  const [a, b, c] = pages;
  await a.goto(url);
  await a.waitForSelector('form');
  await a.getByLabel('Your name').fill('Alice');
  await a.getByRole('button', { name: 'Join room' }).click();
  await a.waitForTimeout(500);
  await shot(a, 'home-join-error');
  await a.goto(`${url}/room/${NO_ROOM}`);
  await a.waitForTimeout(800);
  await shot(a, 'room-not-found');
  await a.goto(url);
  await a.waitForSelector('form');
  await a.getByLabel('Your name').fill('Alice');
  await a.getByRole('button', { name: 'Create room' }).click();
  const dialog = a.getByRole('dialog');
  await dialog.waitFor();
  await dialog.getByLabel('Use a fun room name').uncheck();
  await dialog.getByRole('button', { name: 'Create room' }).click();
  await a.waitForURL(/\/room\//);
  await a.waitForSelector('tr[mat-row]');
  const room = a.url();
  for (const [page, name, role] of [[b, 'Bob', 'Developer'], [c, 'Carol', 'Observer']]) {
    await page.goto(room);
    await page.getByLabel('Your name').waitFor();
    await page.getByLabel('Your name').fill(name);
    await page.getByRole('combobox', { name: 'Role' }).click();
    await page.getByRole('option', { name: role }).click();
    await page.getByRole('button', { name: 'Join room' }).click();
    await page.waitForSelector('tr[mat-row]');
  }
  await a.waitForTimeout(500);
  await shot(c, 'room-observer');
  await shot(b, 'room-nonowner');
  await b.getByRole('radio', { name: 'Tester' }).check();
  await a.waitForTimeout(500);
  await shot(b, 'room-role-change');
  await b.getByRole('radio', { name: 'Developer' }).check();
  await a.getByRole('button', { name: 'Settings' }).click();
  await a.waitForTimeout(600);
  await a.getByRole('dialog').getByRole('combobox', { name: 'New owner' }).click();
  await a.getByRole('option', { name: 'Bob' }).click();
  await a.waitForTimeout(400);
  await shot(a, 'room-settings-transfer');
  await a.getByRole('button', { name: 'Cancel' }).click();
  await a.waitForTimeout(400);
  await a.getByRole('radio', { name: '5', exact: true }).click();
  await b.getByRole('radio', { name: '5', exact: true }).click();
  await a.getByText(/Showing results in/).waitFor();
  await shot(a, 'room-countdown');
  await a.getByText(/Showing results in/).waitFor({ state: 'hidden', timeout: 15000 });
  await a.waitForTimeout(1500);
  await shot(a, 'room-fireworks');
  await Promise.all(all.map((x) => x.close()));
}

export async function newFlow(browser, url, options, shot) {
  const { all, pages } = await contexts(browser, options);
  const [a, b, c] = pages;
  await a.goto(url);
  await a.waitForSelector('form');
  await shot(a, 'home');
  await a.getByLabel('Your name').fill('Alice');
  await a.getByRole('button', { name: 'Create room' }).click();
  const dialog = a.getByRole('dialog');
  await dialog.waitFor();
  await a.waitForTimeout(600);
  await shot(a, 'home-dialog');
  await dialog.getByLabel('Use a fun room name').uncheck();
  await dialog.getByRole('button', { name: 'Create room' }).click();
  await a.waitForURL(/\/room\//);
  await a.waitForSelector('tr[mat-row]');
  await shot(a, 'room-lobby');
  const room = a.url();
  for (const [page, name, role] of [[b, 'Bob', 'Developer'], [c, 'Carol', 'Tester']]) {
    await page.goto(room);
    await page.getByLabel('Your name').waitFor();
    if (name === 'Bob') {
      await shot(page, 'join');
    }
    await page.getByLabel('Your name').fill(name);
    await page.getByRole('combobox', { name: 'Role' }).click();
    await page.getByRole('option', { name: role }).click();
    await page.getByRole('button', { name: 'Join room' }).click();
    await page.waitForSelector('tr[mat-row]');
  }
  await b.getByRole('radio', { name: '5', exact: true }).click();
  await c.getByRole('radio', { name: '8', exact: true }).click();
  await a.waitForTimeout(500);
  await shot(a, 'room-voting');
  await a.getByRole('button', { name: /show votes/i }).click();
  await a.waitForTimeout(1500);
  await shot(a, 'room-revealed');
  await a.getByRole('button', { name: 'Settings' }).click();
  await a.waitForTimeout(600);
  await shot(a, 'room-settings');
  await a.goto(`${url}/credits`);
  await a.waitForTimeout(500);
  await shot(a, 'credits');
  await Promise.all(all.map((x) => x.close()));
}
