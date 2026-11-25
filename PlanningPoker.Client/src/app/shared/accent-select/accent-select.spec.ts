import { TestBed } from '@angular/core/testing';
import { ACCENT_PRESETS, ACCENT_STORAGE_KEY, AccentService } from '../../core/accent.service';
import { AccentSelect } from './accent-select';

describe('AccentSelect', () => {
  beforeEach(() => localStorage.clear());

  function create() {
    const fixture = TestBed.createComponent(AccentSelect);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const trigger = element.querySelector('button[role="combobox"]') as HTMLButtonElement;
    const options = () => Array.from(element.querySelectorAll<HTMLElement>('[role="option"]'));
    const press = (key: string) => {
      trigger.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true }));
      fixture.detectChanges();
    };
    return { fixture, element, trigger, options, press };
  }

  it('shows the stored preset on a labelled button and the list closed', () => {
    localStorage.setItem(ACCENT_STORAGE_KEY, 'indigo');

    const { element, trigger, options } = create();

    expect(trigger.getAttribute('aria-label')).toBe('Accent colour');
    expect(trigger.getAttribute('aria-expanded')).toBe('false');
    expect(trigger.textContent?.trim()).toBe('Indigo');
    expect(element.querySelector('.trigger .dot')?.getAttribute('data-accent')).toBe('indigo');
    expect(options()).toEqual([]);
  });

  it('lists every preset with its own dot when opened', () => {
    const { fixture, trigger, options } = create();

    trigger.click();
    fixture.detectChanges();

    expect(trigger.getAttribute('aria-expanded')).toBe('true');
    expect(options().map((option) => option.textContent?.trim())).toEqual(ACCENT_PRESETS.map((preset) => preset.label));
    expect(options().map((option) => option.querySelector('.dot')?.getAttribute('data-accent'))).toEqual(
      ACCENT_PRESETS.map((preset) => preset.id),
    );
    expect(options()[0].getAttribute('aria-selected')).toBe('true');
  });

  it('applies and persists a clicked preset, then closes', () => {
    const { fixture, trigger, options } = create();
    const set = vi.spyOn(TestBed.inject(AccentService), 'set');

    trigger.click();
    fixture.detectChanges();
    options()[5].click();
    fixture.detectChanges();

    expect(set).toHaveBeenCalledWith('graphite');
    expect(localStorage.getItem(ACCENT_STORAGE_KEY)).toBe('graphite');
    expect(trigger.textContent?.trim()).toBe('Graphite');
    expect(options()).toEqual([]);
  });

  it('works from the keyboard: arrows move, Enter picks, Escape closes', () => {
    const { trigger, options, press } = create();

    press('ArrowDown');
    expect(options().length).toBe(ACCENT_PRESETS.length);
    press('ArrowDown');
    press('ArrowDown');
    expect(trigger.getAttribute('aria-activedescendant')).toBe('accent-option-violet');
    press('Enter');
    expect(TestBed.inject(AccentService).accent()).toBe('violet');
    expect(options()).toEqual([]);

    press('Enter');
    press('End');
    expect(trigger.getAttribute('aria-activedescendant')).toBe(`accent-option-${ACCENT_PRESETS.at(-1)!.id}`);
    press('Escape');
    expect(options()).toEqual([]);
    expect(TestBed.inject(AccentService).accent()).toBe('violet');
  });

  it('closes when it loses focus', () => {
    const { fixture, trigger, options } = create();

    trigger.click();
    fixture.detectChanges();
    trigger.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(options()).toEqual([]);
  });

  it('ignores a value that is not a preset', () => {
    const { fixture } = create();
    const set = vi.spyOn(TestBed.inject(AccentService), 'set');

    fixture.componentInstance.pick('chartreuse');

    expect(set).not.toHaveBeenCalled();
  });

  it('follows a preset changed elsewhere', () => {
    const { fixture, trigger } = create();

    TestBed.inject(AccentService).set('cyan');
    fixture.detectChanges();

    expect(trigger.textContent?.trim()).toBe('Cyan');
  });
});
