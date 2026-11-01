import { TestBed } from '@angular/core/testing';
import { THEME_STORAGE_KEY, ThemeService } from '../../core/theme.service';
import { ThemeSelect } from './theme-select';

describe('ThemeSelect', () => {
  beforeEach(() => localStorage.clear());

  function create() {
    const fixture = TestBed.createComponent(ThemeSelect);
    fixture.detectChanges();
    const select = (fixture.nativeElement as HTMLElement).querySelector('select') as HTMLSelectElement;
    return { fixture, select };
  }

  it('offers auto, light and dark and shows the stored preference', () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'dark');

    const { select } = create();

    expect(Array.from(select.options).map((o) => o.value)).toEqual(['auto', 'light', 'dark']);
    expect(select.value).toBe('dark');
    expect(select.getAttribute('aria-label')).toBe('Theme');
  });

  it('applies and persists the picked option', () => {
    const { select } = create();

    select.value = 'light';
    select.dispatchEvent(new Event('change'));

    expect(TestBed.inject(ThemeService).preference()).toBe('light');
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('light');
  });

  it('follows a preference changed elsewhere', () => {
    const { fixture, select } = create();

    TestBed.inject(ThemeService).set('dark');
    fixture.detectChanges();

    expect(select.value).toBe('dark');
  });
});
