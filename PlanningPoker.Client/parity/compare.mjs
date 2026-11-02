// Visual parity check: drives the old Razor app and the new Angular app through the same states, screenshots them,
// diffs the images and compares the computed style of every visible text. See docs/phase-7-spec.md.
//
//   npm run parity                       compare the new app against docs/parity/reference
//   npm run parity -- --capture-old      regenerate the references from the old app (only if razor-final is retagged)
//   npm run parity -- --props font,color only assert these style properties
//   npm run parity -- --states home,credits --sizes phone --schemes dark
//
// NEW_URL (default http://localhost:5000) serves the built client; OLD_URL (default http://localhost:1333) is the old app,
// needed only with --capture-old.
import { chromium } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import pixelmatch from 'pixelmatch';
import { PNG } from 'pngjs';
import { ALL_STATES, ALL_SIZES, ALL_SCHEMES, SIZES, probeFn, mask } from './probe.mjs';
import { oldFlow, newFlow } from './flows.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const REFERENCE = path.join(here, '..', '..', 'docs', 'parity', 'reference');
const OUT = path.join(here, 'out');
const NEW_URL = process.env.NEW_URL ?? 'http://localhost:5000';
const OLD_URL = process.env.OLD_URL ?? 'http://localhost:1333';
const NOISE_PERCENT = 0.5;
const HEIGHT_TOLERANCE = 0.05;

const args = process.argv.slice(2);
const flag = (name) => args.includes(`--${name}`);
const option = (name, fallback) => {
  const i = args.indexOf(`--${name}`);
  return i >= 0 ? args[i + 1].split(',') : fallback;
};
const states = option('states', ALL_STATES);
const sizes = option('sizes', ALL_SIZES);
const schemes = option('schemes', ALL_SCHEMES);
const props = option('props', ['font', 'size', 'weight', 'color', 'bg']);
const captureOld = flag('capture-old');
const dir = captureOld ? REFERENCE : OUT;
const accepted = JSON.parse(fs.readFileSync(path.join(here, 'accepted.json'), 'utf8'));

fs.mkdirSync(dir, { recursive: true });
fs.mkdirSync(OUT, { recursive: true });

async function capture(page, app, size, scheme, name) {
  await mask(page);
  await page.waitForTimeout(400);
  const file = path.join(dir, `${name}-${size}-${scheme}`);
  await page.screenshot({ path: `${file}.png`, animations: 'disabled', fullPage: true });
  fs.writeFileSync(`${file}.json`, JSON.stringify(await page.evaluate(probeFn), null, 1));
}

function readPng(file) {
  return PNG.sync.read(fs.readFileSync(file));
}

function diffImages(referenceFile, newFile, outFile) {
  const a = readPng(referenceFile);
  const b = readPng(newFile);
  const w = Math.max(a.width, b.width);
  const h = Math.max(a.height, b.height);
  const pad = (img) => {
    const p = new PNG({ width: w, height: h });
    p.data.fill(255);
    PNG.bitblt(img, p, 0, 0, img.width, img.height, 0, 0);
    return p;
  };
  const d = new PNG({ width: w, height: h });
  const changed = pixelmatch(pad(a).data, pad(b).data, d.data, w, h, { threshold: 0.1 });
  const pair = new PNG({ width: a.width + b.width + d.width + 20, height: h });
  pair.data.fill(200);
  PNG.bitblt(a, pair, 0, 0, a.width, a.height, 0, 0);
  PNG.bitblt(b, pair, 0, 0, b.width, b.height, a.width + 10, 0);
  PNG.bitblt(d, pair, 0, 0, d.width, d.height, a.width + b.width + 20, 0);
  fs.writeFileSync(outFile, PNG.sync.write(pair));
  return { percent: (changed / (w * h)) * 100, heightDelta: (b.height - a.height) / a.height };
}

function styleDiffs(key, reference, current) {
  const failures = [];
  for (const [text, ref] of Object.entries(reference)) {
    const cur = current[text];
    if (!cur) {
      continue;
    }
    for (const prop of props) {
      if (ref[prop] !== cur[prop] && !accepted.some((a) => a.key === key && a.text === text && a.prop === prop)) {
        failures.push({ text: text.slice(0, 30), prop, old: ref[prop], new: cur[prop] });
      }
    }
  }
  return failures;
}

const browser = await chromium.launch();
const target = captureOld ? 'old' : 'new';
for (const size of sizes) {
  for (const scheme of schemes) {
    const shot = (page, name) => (states.includes(name) ? capture(page, target, size, scheme, name) : Promise.resolve());
    const options = { viewport: SIZES[size], colorScheme: scheme };
    try {
      await (captureOld ? oldFlow(browser, OLD_URL, options, shot) : newFlow(browser, NEW_URL, options, shot));
    } catch (error) {
      console.error(`FAILED ${target} ${size} ${scheme}: ${error.message.split('\n')[0]}`);
      process.exitCode = 1;
    }
  }
}
await browser.close();

if (captureOld) {
  console.log(`References written to ${REFERENCE}`);
  process.exit(process.exitCode ?? 0);
}

const rows = [];
let failed = 0;
for (const state of states) {
  for (const size of sizes) {
    for (const scheme of schemes) {
      const key = `${state}-${size}-${scheme}`;
      const referenceShot = path.join(REFERENCE, `${key}.png`);
      if (!fs.existsSync(referenceShot) || !fs.existsSync(path.join(OUT, `${key}.png`))) {
        rows.push(`| ${key} | missing | | |`);
        failed++;
        continue;
      }
      const image = diffImages(referenceShot, path.join(OUT, `${key}.png`), path.join(OUT, `pair-${key}.png`));
      const diffs = styleDiffs(
        key,
        JSON.parse(fs.readFileSync(path.join(REFERENCE, `${key}.json`), 'utf8')),
        JSON.parse(fs.readFileSync(path.join(OUT, `${key}.json`), 'utf8')),
      );
      const tall = Math.abs(image.heightDelta) > HEIGHT_TOLERANCE;
      if (diffs.length || tall) {
        failed++;
      }
      rows.push(
        `| ${key} | ${image.percent.toFixed(1)}%${image.percent <= NOISE_PERCENT ? ' (noise)' : ''} | ${(image.heightDelta * 100).toFixed(0)}%${tall ? ' !' : ''} | ${diffs.length} |`,
      );
      for (const d of diffs.slice(0, 5)) {
        rows.push(`|  ${d.text} | ${d.prop} | ${d.old} -> ${d.new} | |`);
      }
    }
  }
}
const report = ['| State | Pixel diff | Height | Style failures |', '|---|---|---|---|', ...rows].join('\n');
fs.writeFileSync(path.join(OUT, 'report.md'), report + '\n');
console.log(report);
console.log(`\n${failed} state(s) outside the bar for props [${props.join(', ')}]. Image pairs: ${OUT}`);
process.exit(failed ? 1 : 0);
