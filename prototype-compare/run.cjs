// PROTOTYPE (throwaway): drive old (:1333) and new (:5000) through the same states, screenshot, diff.
const { chromium } = require('D:/Git/PlanningPoker/PlanningPoker.Client/node_modules/playwright-core');
const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
const pixelmatch = require('pixelmatch');

const OUT = path.join(__dirname, 'out');
const APPS = {
  old: 'http://localhost:1333',
  new: 'http://localhost:5000',
};
const SIZES = { desktop: { width: 1280, height: 800 }, phone: { width: 375, height: 812 } };
const GUID = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/g;

async function mask(page) {
  // room id differs per run: blank it so it never counts as a diff
  await page.evaluate((src) => {
    const re = new RegExp(src, 'g');
    const w = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
    for (let n = w.nextNode(); n; n = w.nextNode()) n.nodeValue = n.nodeValue.replace(re, 'ROOM-ID');
  }, GUID.source);
}

async function shot(page, app, size, scheme, name) {
  await mask(page);
  await page.waitForTimeout(400);
  const file = path.join(OUT, `${app}-${size}-${scheme}-${name}.png`);
  await page.screenshot({ path: file, animations: 'disabled', fullPage: true });
  const data = await page.evaluate(probeFn);
  fs.writeFileSync(file.replace('.png', '.json'), JSON.stringify(data, null, 1));
}

// Style probe: one record per visible text, keyed by normalised text, so old and new pair up despite different DOM.
function probeFn() {
  const out = {};
  const bgOf = (el) => {
    for (let e = el; e; e = e.parentElement) {
      const c = getComputedStyle(e).backgroundColor;
      if (c && c !== 'rgba(0, 0, 0, 0)' && c !== 'transparent') return c;
    }
    return 'rgba(0, 0, 0, 0)';
  };
  const w = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
  for (let n = w.nextNode(); n; n = w.nextNode()) {
    const t = n.nodeValue.replace(/\s+/g, ' ').trim();
    const el = n.parentElement;
    if (!t || !el || /^(SCRIPT|STYLE|NOSCRIPT)$/.test(el.tagName)) continue;
    const r = el.getBoundingClientRect();
    if (r.width === 0 || r.height === 0) continue;
    const s = getComputedStyle(el);
    if (s.visibility === 'hidden' || s.display === 'none') continue;
    const key = t.toLowerCase();
    if (out[key]) continue;
    out[key] = {
      tag: el.tagName.toLowerCase(),
      font: s.fontFamily.split(',')[0].replace(/["']/g, '').trim(),
      size: s.fontSize,
      weight: s.fontWeight,
      color: s.color,
      bg: bgOf(el),
      align: s.textAlign,
      deco: s.textDecorationLine,
      radius: s.borderRadius,
      pad: s.padding,
      x: Math.round(r.x), y: Math.round(r.y), w: Math.round(r.width), h: Math.round(r.height),
    };
  }
  return out;
}

async function oldFlow(browser, size, scheme) {
  const opts = { viewport: SIZES[size], colorScheme: scheme };
  const ctxs = [await browser.newContext(opts), await browser.newContext(opts), await browser.newContext(opts)];
  const [a, b, c] = await Promise.all(ctxs.map((x) => x.newPage()));
  await a.goto(APPS.old);
  await shot(a, 'old', size, scheme, 'home');
  await a.click('#room-creation-settings-btn');
  await a.waitForTimeout(600);
  await shot(a, 'old', size, scheme, 'home-dialog');
  await a.goto(APPS.old);
  await a.fill('#name', 'Alice');
  await a.click('input[value="Create room"]');
  await a.waitForURL(/\/Room\//);
  await a.waitForSelector('#people-dev tr, #people-dev td');
  await shot(a, 'old', size, scheme, 'room-lobby');
  const url = a.url();
  for (const [p, n, t] of [[b, 'Bob', 'dev'], [c, 'Carol', 'test']]) {
    await p.goto(url);
    if (n === 'Bob') await shot(p, 'old', size, scheme, 'join');
    await p.fill('#name', n);
    await p.locator(t === 'dev' ? 'label[for=isDev]' : 'label[for=isTest]').click();
    await p.click('input[value="Join room"]');
    await p.waitForSelector('input[name=mark]', { state: 'attached' });
  }
  await b.locator('label[for="5"], input[value="5"] + label').first().click();
  await c.locator('label[for="8"], input[value="8"] + label').first().click();
  await a.waitForTimeout(500);
  await shot(a, 'old', size, scheme, 'room-voting');
  await a.click('#show-votes');
  await a.waitForTimeout(1500);
  await shot(a, 'old', size, scheme, 'room-revealed');
  await a.click('#room-settings-btn');
  await a.waitForTimeout(600);
  await shot(a, 'old', size, scheme, 'room-settings');
  await a.goto(APPS.old + '/Credits');
  await shot(a, 'old', size, scheme, 'credits');
  await Promise.all(ctxs.map((x) => x.close()));
}

async function newFlow(browser, size, scheme) {
  const opts = { viewport: SIZES[size], colorScheme: scheme };
  const ctxs = [await browser.newContext(opts), await browser.newContext(opts), await browser.newContext(opts)];
  const [a, b, c] = await Promise.all(ctxs.map((x) => x.newPage()));
  await a.goto(APPS.new);
  await a.waitForSelector('form');
  await shot(a, 'new', size, scheme, 'home');
  await a.getByLabel('Your name').fill('Alice');
  await a.getByRole('button', { name: 'Create room' }).click();
  const d = a.getByRole('dialog');
  await d.waitFor();
  await a.waitForTimeout(600);
  await shot(a, 'new', size, scheme, 'home-dialog');
  await d.getByLabel('Use a fun room name').uncheck();
  await d.getByRole('button', { name: 'Create room' }).click();
  await a.waitForURL(/\/room\//);
  await a.waitForSelector('tr[mat-row]');
  await shot(a, 'new', size, scheme, 'room-lobby');
  const url = a.url();
  for (const [p, n, t] of [[b, 'Bob', 'Developer'], [c, 'Carol', 'Tester']]) {
    await p.goto(url);
    await p.getByLabel('Your name').waitFor();
    if (n === 'Bob') await shot(p, 'new', size, scheme, 'join');
    await p.getByLabel('Your name').fill(n);
    await p.getByRole('combobox', { name: 'Role' }).click();
    await p.getByRole('option', { name: t }).click();
    await p.getByRole('button', { name: 'Join room' }).click();
    await p.waitForSelector('tr[mat-row]');
  }
  await b.getByRole('radio', { name: '5', exact: true }).click();
  await c.getByRole('radio', { name: '8', exact: true }).click();
  await a.waitForTimeout(500);
  await shot(a, 'new', size, scheme, 'room-voting');
  await a.getByRole('button', { name: /show votes/i }).click();
  await a.waitForTimeout(1500);
  await shot(a, 'new', size, scheme, 'room-revealed');
  await a.getByRole('button', { name: 'Settings' }).click();
  await a.waitForTimeout(600);
  await shot(a, 'new', size, scheme, 'room-settings');
  await a.goto(APPS.new + '/credits');
  await a.waitForTimeout(500);
  await shot(a, 'new', size, scheme, 'credits');
  await Promise.all(ctxs.map((x) => x.close()));
}

function diff(f1, f2, out) {
  const A = PNG.sync.read(fs.readFileSync(f1));
  const B = PNG.sync.read(fs.readFileSync(f2));
  const w = Math.max(A.width, B.width);
  const h = Math.max(A.height, B.height);
  const pad = (img) => {
    const p = new PNG({ width: w, height: h });
    p.data.fill(255);
    PNG.bitblt(img, p, 0, 0, img.width, img.height, 0, 0);
    return p;
  };
  const pa = pad(A);
  const pb = pad(B);
  const d = new PNG({ width: w, height: h });
  const n = pixelmatch(pa.data, pb.data, d.data, w, h, { threshold: 0.1 });
  fs.writeFileSync(out, PNG.sync.write(d));
  return { pct: +((n / (w * h)) * 100).toFixed(1), sizeA: `${A.width}x${A.height}`, sizeB: `${B.width}x${B.height}` };
}

(async () => {
  fs.mkdirSync(OUT, { recursive: true });
  const browser = await chromium.launch();
  const only = process.argv[2];
  for (const size of Object.keys(SIZES)) {
    for (const scheme of ['light', 'dark']) {
      if (only && only !== `${size}-${scheme}`) continue;
      try { await oldFlow(browser, size, scheme); } catch (e) { console.log('OLD FAIL', size, scheme, e.message.split('\n')[0]); }
      try { await newFlow(browser, size, scheme); } catch (e) { console.log('NEW FAIL', size, scheme, e.message.split('\n')[0]); }
    }
  }
  await browser.close();
  const rows = [];
  for (const f of fs.readdirSync(OUT).filter((x) => x.startsWith('old-'))) {
    const g = f.replace('old-', 'new-');
    if (!fs.existsSync(path.join(OUT, g))) continue;
    const r = diff(path.join(OUT, f), path.join(OUT, g), path.join(OUT, f.replace('old-', 'diff-')));
    rows.push(`${f.replace('old-', '').replace('.png', '').padEnd(34)} old ${r.sizeA.padEnd(10)} new ${r.sizeB.padEnd(10)} diff ${r.pct}%`);
  }
  console.log(rows.join('\n'));
})();
