import { TestBed } from '@angular/core/testing';
import { THEME_STORAGE_KEY, ThemeService } from '../../core/theme.service';
import { ThemeSelect } from './theme-select';

describe('ThemeSelect', () => {
  beforeEach(() => localStorage.clear());

  function create() {
    const fixture = TestBed.createComponent(ThemeSelect);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const buttons = Array.from(element.querySelectorAll('button[role="radio"]')) as HTMLButtonElement[];
    return { fixture, element, buttons };
  }

  it('offers auto, light and dark and marks the stored preference', () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'dark');

    const { element, buttons } = create();

    expect(buttons.map((b) => b.textContent?.trim())).toEqual(['Auto', 'Light', 'Dark']);
    expect(buttons.map((b) => b.getAttribute('aria-checked'))).toEqual(['false', 'false', 'true']);
    expect(element.querySelector('[role="radiogroup"]')?.getAttribute('aria-label')).toBe('Theme');
  });

  it('applies and persists the picked option', () => {
    const { buttons } = create();
    const set = vi.spyOn(TestBed.inject(ThemeService), 'set');

    buttons[1].click();

    expect(set).toHaveBeenCalledWith('light');
    expect(TestBed.inject(ThemeService).preference()).toBe('light');
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('light');
  });

  it('follows a preference changed elsewhere', () => {
    const { fixture, buttons } = create();

    TestBed.inject(ThemeService).set('dark');
    fixture.detectChanges();

    expect(buttons[2].getAttribute('aria-checked')).toBe('true');
    expect(buttons[2].classList).toContain('on');
  });
});
