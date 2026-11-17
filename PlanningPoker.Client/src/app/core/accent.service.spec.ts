import { TestBed } from '@angular/core/testing';
import {
  ACCENT_PRESETS,
  ACCENT_STORAGE_KEY,
  AccentService,
  DEFAULT_ACCENT,
  isAccentId,
} from './accent.service';

describe('AccentService', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-accent');
  });

  it('defaults to blue when nothing is stored', () => {
    const service = TestBed.inject(AccentService);

    expect(DEFAULT_ACCENT).toBe('blue');
    expect(service.accent()).toBe('blue');
  });

  it('restores a stored preset', () => {
    localStorage.setItem(ACCENT_STORAGE_KEY, 'violet');

    expect(TestBed.inject(AccentService).accent()).toBe('violet');
  });

  it('ignores an unknown stored value', () => {
    localStorage.setItem(ACCENT_STORAGE_KEY, 'green');

    expect(TestBed.inject(AccentService).accent()).toBe('blue');
  });

  it('sets, persists and marks the html element with the picked preset', () => {
    const service = TestBed.inject(AccentService);

    service.set('cyan');
    TestBed.tick();

    expect(service.accent()).toBe('cyan');
    expect(localStorage.getItem(ACCENT_STORAGE_KEY)).toBe('cyan');
    expect(document.documentElement.getAttribute('data-accent')).toBe('cyan');
  });

  it('keeps the chosen accent across a reload of the service', () => {
    TestBed.inject(AccentService).set('magenta');

    TestBed.resetTestingModule();

    expect(TestBed.inject(AccentService).accent()).toBe('magenta');
  });
});

describe('isAccentId', () => {
  it('accepts every preset id', () => {
    for (const preset of ACCENT_PRESETS) {
      expect(isAccentId(preset.id)).toBe(true);
    }
  });

  it('rejects anything else', () => {
    expect(isAccentId('green')).toBe(false);
    expect(isAccentId('')).toBe(false);
    expect(isAccentId(null)).toBe(false);
  });
});

describe('ACCENT_PRESETS', () => {
  it('lists the six presets, blue first', () => {
    expect(ACCENT_PRESETS.map((preset) => preset.id)).toEqual([
      'blue',
      'indigo',
      'violet',
      'magenta',
      'cyan',
      'graphite',
    ]);
    expect(ACCENT_PRESETS.map((preset) => preset.label)).toEqual([
      'Blue',
      'Indigo',
      'Violet',
      'Magenta',
      'Cyan',
      'Graphite',
    ]);
  });
});
