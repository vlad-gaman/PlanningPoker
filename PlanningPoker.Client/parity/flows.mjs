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
