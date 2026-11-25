import { expect, test } from '@playwright/test';
import { readdirSync, readFileSync } from 'node:fs';
import { join, relative } from 'node:path';

// Guards the shared UI pattern: new screens reuse the global button and form styles in src/styles.scss
// instead of growing their own look-alikes. See "Decorations" in CLAUDE.md.

const SRC = join(__dirname, '..', 'src');

function files(dir: string, extensions: string[]): string[] {
  return readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const path = join(dir, entry.name);
    if (entry.isDirectory()) {
      return files(path, extensions);
    }
    return extensions.some((extension) => entry.name.endsWith(extension)) ? [path] : [];
  });
}

// Opening tags of an element; an attribute value may hold '>' only inside quotes.
function tags(text: string, element: string): string[] {
  return [...text.matchAll(new RegExp(String.raw`<${element}\b(?:[^>"]|"[^"]*")*>`, 'g'))].map(([tag]) => tag);
}

function classesOf(tag: string): string[] {
  return /\sclass="([^"]*)"/.exec(tag)?.[1].split(/\s+/) ?? [];
}

function appPath(path: string): string {
  return relative(join(SRC, 'app'), path).replaceAll('\\', '/');
}

function templates(): string[] {
  return files(SRC, ['.html', '.ts']).filter((path) => !path.endsWith('.spec.ts'));
}

// Retired styles; use .primary-button / .secondary-button / .danger-button (+ .button-sm), .icon-btn, .link and .input (+ .input-sm).
const retiredClasses = [
  'form-control',
  'seg',
  'role-toggle',
  'check',
  'btn',
  'btn-sm',
  'plain-button',
  'plain-input',
  'form-check',
  'form-check-input',
  'act',
  'copy',
  'icon-only',
  'link-button',
];

test('templates do not use retired button and form classes', () => {
  const offences: string[] = [];
  for (const path of templates()) {
    const text = readFileSync(path, 'utf8');
    for (const name of retiredClasses) {
      if (new RegExp(String.raw`class="[^"]*(?<![\w-])${name}(?![\w-])[^"]*"`).test(text)) {
        offences.push(`${path}: ${name}`);
      }
    }
  }
  expect(offences).toEqual([]);
});

test('component styles do not redefine the shared buttons and fields', () => {
  const offences: string[] = [];
  for (const path of files(join(SRC, 'app'), ['.scss'])) {
    const text = readFileSync(path, 'utf8');
    if (/^\s*\.((primary|secondary|danger)-button|button-sm|icon-btn(-sm|-danger)?|link|input(-sm)?|switch(-field)?|field-label|hint|segmented(-icons)?)\s*[{,:]/m.test(text)) {
      offences.push(path);
    }
  }
  expect(offences).toEqual([]);
});

// A button that opens a list (role="combobox") looks like a field, so .input counts too.
const sharedButtonClasses = ['primary-button', 'secondary-button', 'danger-button', 'icon-btn', 'link', 'input'];

// Buttons that keep their own look until a later part of issue #181 moves them; shrink this as each part lands.
const exemptButtons: Record<string, number> = {
  'features/room/alarm/alarm-button.html': 1, // alarm pill
};

// The options of a .segmented control take its look, so they need no class of their own.
const segmented = /<div class="segmented[^"]*"[^>]*>[\s\S]*?<\/div>/g;

test('every button uses a shared button style', () => {
  const found: Record<string, number> = {};
  for (const path of templates()) {
    const text = readFileSync(path, 'utf8').replace(segmented, '');
    for (const tag of tags(text, 'button')) {
      if (!classesOf(tag).some((name) => sharedButtonClasses.includes(name))) {
        const key = appPath(path);
        found[key] = (found[key] ?? 0) + 1;
      }
    }
  }
  expect(found).toEqual(exemptButtons);
});

test('every text field and select uses the shared .input', () => {
  const offences: string[] = [];
  for (const path of templates()) {
    const text = readFileSync(path, 'utf8');
    const fields = [...tags(text, 'input'), ...tags(text, 'select'), ...tags(text, 'textarea')].filter(
      (tag) => !/\stype="(checkbox|radio|hidden)"/.test(tag),
    );
    for (const tag of fields) {
      if (!classesOf(tag).includes('input')) {
        offences.push(`${appPath(path)}: ${tag.replace(/\s+/g, ' ')}`);
      }
    }
  }
  expect(offences).toEqual([]);
});

test('every checkbox is a shared .switch', () => {
  const offences: string[] = [];
  for (const path of templates()) {
    const text = readFileSync(path, 'utf8');
    for (const tag of tags(text, 'input').filter((tag) => /\stype="checkbox"/.test(tag))) {
      if (!/\srole="switch"/.test(tag)) {
        offences.push(`${appPath(path)}: ${tag.replace(/\s+/g, ' ')}`);
      }
    }
    // The checkbox sits inside its .switch, followed by the track it draws.
    const switches = text.match(/<span class="switch">/g)?.length ?? 0;
    const wellFormed = text.match(/<span class="switch">\s*<input\b[^>]*role="switch"[^>]*\/>\s*<span class="track"><\/span>\s*<\/span>/g)?.length ?? 0;
    if (switches !== wellFormed) {
      offences.push(`${appPath(path)}: a .switch is not <span class="switch"><input role="switch" /><span class="track"></span></span>`);
    }
  }
  expect(offences).toEqual([]);
});

// The colour tokens of one theme block in styles.scss: ':root' (light) or "[data-theme='dark']".
function tokens(block: string): Record<string, string> {
  const styles = readFileSync(join(SRC, 'styles.scss'), 'utf8');
  const start = styles.indexOf(`${block} {`);
  const body = styles.slice(start, styles.indexOf('\n}', start));
  return Object.fromEntries([...body.matchAll(/--([\w-]+):\s*(#[0-9a-f]{6})\s*;/gi)].map(([, name, value]) => [name, value]));
}

function contrast(a: string, b: string): number {
  const luminance = (hex: string) => {
    const [r, g, b] = [1, 3, 5]
      .map((i) => parseInt(hex.slice(i, i + 2), 16) / 255)
      .map((v) => (v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4));
    return 0.2126 * r + 0.7152 * g + 0.0722 * b;
  };
  const [light, dark] = [luminance(a), luminance(b)].sort((x, y) => y - x);
  return (light + 0.05) / (dark + 0.05);
}

for (const [theme, block] of [
  ['light', ':root'],
  ['dark', "[data-theme='dark']"],
]) {
  test(`a switch's off track stands out at 3:1 from what it sits on in the ${theme} theme`, () => {
    const colours = { ...tokens(':root'), ...tokens(block) };
    for (const background of ['panel-bg', 'card-bg', 'bg-color']) {
      expect(contrast(colours['switch-off'], colours[background]), `--switch-off on --${background}`).toBeGreaterThanOrEqual(3);
    }
  });
}

test('no field label ends in a colon', () => {
  const offences: string[] = [];
  for (const path of templates()) {
    const text = readFileSync(path, 'utf8');
    for (const [, label] of text.matchAll(/<(?:label|span class="field-label")\b[^>]*>([^<]*)</g)) {
      if (label.trim().endsWith(':')) {
        offences.push(`${appPath(path)}: ${label.trim()}`);
      }
    }
  }
  expect(offences).toEqual([]);
});

test('segmented options are only buttons or radio labels', () => {
  const offences: string[] = [];
  for (const path of templates()) {
    for (const [group] of readFileSync(path, 'utf8').matchAll(segmented)) {
      const body = group.replace(/^<div[^>]*>/, '').replace(/<\/div>$/, '');
      const options = [...tags(body, 'button'), ...tags(body, 'label')];
      if (options.length === 0 || tags(body, 'input').some((tag) => !/\stype="radio"/.test(tag))) {
        offences.push(`${appPath(path)}: ${group.slice(0, 80)}`);
      }
    }
  }
  expect(offences).toEqual([]);
});
