import { expect, test } from '@playwright/test';
import { readdirSync, readFileSync } from 'node:fs';
import { join, relative } from 'node:path';

// Guards the shared UI pattern: new screens reuse the global buttons, fields, containers and text styles in src/styles.scss
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

// Retired styles; use .primary-button / .secondary-button / .danger-button (+ .button-sm), .icon-btn, .link, .input (+ .input-sm),
// .page (+ .page-wide), .notice (+ .notice-error) and .dialog-header / .dialog-body / .dialog-footer.
const retiredClasses = [
  'home',
  'banner',
  'settings-header',
  'settings-footer',
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

test('templates do not use retired classes', () => {
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

test('component styles do not redefine the shared classes', () => {
  const offences: string[] = [];
  for (const path of files(join(SRC, 'app'), ['.scss'])) {
    const text = readFileSync(path, 'utf8');
    if (
      /^\s*\.((primary|secondary|danger)-button|button-sm|icon-btn(-sm|-danger)?|link|input(-sm)?|switch(-field)?|field-label|hint|segmented(-icons)?|page(-wide)?|eyebrow|notice(-error)?|chip|list-row|dialog-(header|body|footer))\s*[{,:]/m.test(
        text,
      )
    ) {
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

// The raw token values of one theme block in styles.scss: ':root' (light) or "[data-theme='dark']".
function block(selector: string): Record<string, string> {
  const styles = readFileSync(join(SRC, 'styles.scss'), 'utf8');
  const start = styles.indexOf(`${selector} {`);
  const body = styles.slice(start, styles.indexOf('\n}', start));
  return Object.fromEntries([...body.matchAll(/^\s*--([\w-]+):\s*([^;]+);/gm)].map(([, name, value]) => [name, value.trim()]));
}

// The colour tokens of a theme as six-digit hex. The dark block sits on :root too (data-theme is an attribute of
// <html>), so it overrides the light values and a var() alias resolves against the merged set, as in the browser.
function tokens(theme: 'light' | 'dark'): Record<string, string> {
  const raw = { ...block(':root'), ...(theme === 'dark' ? block("[data-theme='dark']") : {}) };
  const resolve = (value: string, seen: string[]): string | undefined => {
    const alias = /^var\(--([\w-]+)\)$/.exec(value);
    if (alias) {
      return seen.includes(alias[1]) || raw[alias[1]] === undefined ? undefined : resolve(raw[alias[1]], [...seen, alias[1]]);
    }
    const short = /^#([0-9a-f])([0-9a-f])([0-9a-f])$/i.exec(value);
    if (short) {
      return `#${short[1]}${short[1]}${short[2]}${short[2]}${short[3]}${short[3]}`;
    }
    return /^#[0-9a-f]{6}$/i.test(value) ? value : undefined;
  };
  return Object.fromEntries(
    Object.entries(raw)
      .map(([name, value]) => [name, resolve(value, [name])])
      .filter((entry): entry is [string, string] => entry[1] !== undefined),
  );
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

const SURFACES = ['bg-color', 'panel-bg', 'card-bg'];

// Every foreground and the fill or surface it sits on, with the WCAG AA minimum: 4.5:1 for text, 3:1 for a control's
// parts (a switch's track and thumb).
const PAIRS: [string, string, number][] = [
  ...['text-color', 'muted-color', 'primary-color', 'danger', 'verdict-good', 'verdict-mid'].flatMap((text) =>
    SURFACES.map((surface): [string, string, number] => [text, surface, 4.5]),
  ),
  ['on-primary', 'primary-color', 4.5],
  ['on-primary', 'primary-hover', 4.5],
  ['on-primary-container', 'primary-container', 4.5],
  ['on-danger', 'danger', 4.5],
  ...['dev', 'test', 'sre', 'obs'].map((role): [string, string, number] => ['on-role', `role-${role}`, 4.5]),
  ...[1, 2, 3, 4, 5, 6].map((seg): [string, string, number] => ['on-seg', `seg-${seg}`, 4.5]),
  ...SURFACES.map((surface): [string, string, number] => ['switch-off', surface, 3]),
  ['switch-thumb', 'switch-off', 3],
];

for (const theme of ['light', 'dark'] as const) {
  test(`every colour pair meets its contrast minimum in the ${theme} theme`, () => {
    const colours = tokens(theme);
    const offences = PAIRS.flatMap(([fg, bg, min]) => {
      if (!colours[fg] || !colours[bg]) {
        return [`--${fg} on --${bg}: not a hex colour`];
      }
      const ratio = contrast(colours[fg], colours[bg]);
      return ratio < min ? [`--${fg} on --${bg}: ${ratio.toFixed(2)} < ${min}`] : [];
    });
    expect(offences).toEqual([]);
  });
}

test('component styles take white and black from a token, never a literal', () => {
  const offences = componentStyles().filter((path) =>
    /(?<![\w-])(color|background|background-color|border-color):\s*(#fff\b|#ffffff\b|white\b|#000\b|#000000\b|black\b)/i.test(
      readFileSync(path, 'utf8'),
    ),
  );
  expect(offences).toEqual([]);
});

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

// Component styles and compact.scss, which the shared rules in styles.scss must not be undercut by.
function componentStyles(): string[] {
  return [...files(join(SRC, 'app'), ['.scss']), join(SRC, 'compact.scss')];
}

test('error colours come from --danger', () => {
  const offences: string[] = [];
  for (const path of [...componentStyles(), join(SRC, 'styles.scss')]) {
    for (const line of readFileSync(path, 'utf8').split('\n')) {
      // The one allowed use: styles.scss points Material's error token at --danger.
      if (/var\(--mat-sys-error\)|#b3261e/i.test(line)) {
        offences.push(`${path}: ${line.trim()}`);
      }
    }
  }
  expect(offences).toEqual([]);
});

test('only the home hero sizes its own h1', () => {
  const offences = componentStyles()
    .filter((path) => appPath(path) !== 'features/home/home.scss')
    .filter((path) => /\bh1\s*\{[^}]*font-size/.test(readFileSync(path, 'utf8')));
  expect(offences).toEqual([]);
});

test('the focus ring is the one global :focus-visible', () => {
  const offences = componentStyles().filter((path) => /focus-visible[^{]*\{[^}]*outline/.test(readFileSync(path, 'utf8')));
  expect(offences).toEqual([]);
});

test('every dialog has the shared header, body and footer', () => {
  const offences: string[] = [];
  for (const path of templates().filter((path) => readFileSync(path, 'utf8').includes('mat-dialog-title'))) {
    const text = readFileSync(path, 'utf8');
    for (const part of ['dialog-header', 'dialog-body', 'dialog-footer']) {
      if (!new RegExp(String.raw`class="[^"]*(?<![\w-])${part}(?![\w-])`).test(text)) {
        offences.push(`${appPath(path)}: no .${part}`);
      }
    }
    if (!/<div class="dialog-header">\s*<h2 mat-dialog-title>/.test(text)) {
      offences.push(`${appPath(path)}: the title is not the first thing in .dialog-header`);
    }
  }
  expect(offences).toEqual([]);
});
