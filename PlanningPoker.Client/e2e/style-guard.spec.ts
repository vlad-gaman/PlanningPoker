import { expect, test } from '@playwright/test';
import { readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

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

// Retired styles; use .primary-button / .secondary-button / .danger-button (+ .button-sm) and .input.
const retiredClasses = ['btn', 'btn-sm', 'plain-button', 'plain-input', 'form-check', 'form-check-input'];

test('templates do not use retired button and form classes', () => {
  const offences: string[] = [];
  for (const path of files(SRC, ['.html', '.ts'])) {
    if (path.endsWith('.spec.ts')) {
      continue;
    }
    const text = readFileSync(path, 'utf8');
    for (const name of retiredClasses) {
      if (new RegExp(`class="[^"]*(?<![\w-])${name}(?![\w-])[^"]*"`).test(text)) {
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
    if (/^\s*\.(primary|secondary|danger)-button\s*[{,]/m.test(text)) {
      offences.push(path);
    }
  }
  expect(offences).toEqual([]);
});
