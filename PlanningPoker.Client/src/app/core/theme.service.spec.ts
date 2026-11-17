import { TestBed } from '@angular/core/testing';
import { THEME_STORAGE_KEY, ThemeService } from './theme.service';

function mockSystemDark(matches: boolean): { fire: (value: boolean) => void } {
  let listener: ((event: { matches: boolean }) => void) | undefined;
  vi.stubGlobal(
    'matchMedia',
    vi.fn().mockReturnValue({
      matches,
      addEventListener: (_: string, cb: (event: { matches: boolean }) => void) => (listener = cb),
    }),
  );
  return { fire: (value) => listener?.({ matches: value }) };
}

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  afterEach(() => vi.unstubAllGlobals());

  it('defaults to auto and follows a light system', () => {
    mockSystemDark(false);

    const service = TestBed.inject(ThemeService);

    expect(service.preference()).toBe('auto');
    expect(service.effective()).toBe('light');
  });

  it('follows a dark system when auto, and reacts to changes', () => {
    const system = mockSystemDark(true);
    const service = TestBed.inject(ThemeService);
    expect(service.effective()).toBe('dark');

    system.fire(false);

    expect(service.effective()).toBe('light');
  });

  it('an explicit preference overrides the system and is persisted', () => {
    mockSystemDark(true);
    const service = TestBed.inject(ThemeService);

    service.set('light');

    expect(service.effective()).toBe('light');
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('light');
  });

  it('restores a stored preference', () => {
    mockSystemDark(false);
    localStorage.setItem(THEME_STORAGE_KEY, 'dark');

    const service = TestBed.inject(ThemeService);

    expect(service.preference()).toBe('dark');
    expect(service.effective()).toBe('dark');
  });

  it('ignores an invalid stored value', () => {
    mockSystemDark(false);
    localStorage.setItem(THEME_STORAGE_KEY, 'purple');

    expect(TestBed.inject(ThemeService).preference()).toBe('auto');
  });

  it('sets data-theme on the html element', () => {
    mockSystemDark(false);
    const service = TestBed.inject(ThemeService);

    service.set('dark');
    TestBed.tick();

    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });
});
