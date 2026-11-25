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

function templates(): string[] {
  return files(SRC, ['.html', '.ts']).filter((path) => !path.endsWith('.spec.ts'));
}

// Retired styles; use .primary-button / .secondary-button / .danger-button (+ .button-sm), .icon-btn, .link and .input.
const retiredClasses = [
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

test('component styles do not redefine the shared buttons', () => {
  const offences: string[] = [];
  for (const path of files(join(SRC, 'app'), ['.scss'])) {
    const text = readFileSync(path, 'utf8');
    if (/^\s*\.((primary|secondary|danger)-button|button-sm|icon-btn|link)\s*[{,:]/m.test(text)) {
      offences.push(path);
    }
  }
  expect(offences).toEqual([]);
});

const sharedButtonClasses = ['primary-button', 'secondary-button', 'danger-button', 'icon-btn', 'link'];

// Buttons that keep their own look until a later part of issue #181 moves them; shrink this as each part lands.
const exemptButtons: Record<string, number> = {
  'features/room/results/results.html': 1, // "By role" toggle -> shared .switch
  'features/room/room.html': 2, // compact Vote/Table tabs -> shared .segmented
  'shared/theme-select/theme-select.html': 1, // theme options -> shared .segmented
  'shared/accent-select/accent-select.html': 1, // accent trigger -> shared .input-sm
  'features/room/alarm/alarm-button.html': 1, // alarm pill
};

test('every button uses a shared button style', () => {
  const found: Record<string, number> = {};
  for (const path of templates()) {
    const text = readFileSync(path, 'utf8');
    for (const [tag] of text.matchAll(/<button\b(?:[^>"]|"[^"]*")*>/g)) {
      const classes = /\sclass="([^"]*)"/.exec(tag)?.[1].split(/\s+/) ?? [];
      if (!classes.some((name) => sharedButtonClasses.includes(name))) {
        const key = relative(join(SRC, 'app'), path).replaceAll('\\', '/');
        found[key] = (found[key] ?? 0) + 1;
      }
    }
  }
  expect(found).toEqual(exemptButtons);
});
