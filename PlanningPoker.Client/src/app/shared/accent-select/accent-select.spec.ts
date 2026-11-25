import { TestBed } from '@angular/core/testing';
import { ACCENT_STORAGE_KEY, AccentService } from '../../core/accent.service';
import { AccentSelect } from './accent-select';

describe('AccentSelect', () => {
  beforeEach(() => localStorage.clear());

  function create() {
    const fixture = TestBed.createComponent(AccentSelect);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const select = element.querySelector('select') as HTMLSelectElement;
    return { fixture, element, select };
  }

  it('offers the eighteen presets as a labelled dropdown and shows the stored one', () => {
    localStorage.setItem(ACCENT_STORAGE_KEY, 'indigo');

    const { select } = create();

    expect(Array.from(select.options).map((option) => option.textContent?.trim())).toEqual([
      '● Blue',
      '● Indigo',
      '● Violet',
      '● Magenta',
      '● Cyan',
      '● Graphite',
      '● Red',
      '● Orange',
      '● Amber',
      '● Green',
      '● Teal',
      '● Brown',
      '● Lime',
      '● Sky',
      '● Rose',
      '● Plum',
      '● Slate',
      '● Gold',
    ]);
    expect(select.value).toBe('indigo');
    expect(select.getAttribute('aria-label')).toBe('Accent colour');
  });

  it('applies and persists the picked preset', () => {
    const { fixture, select } = create();
    const set = vi.spyOn(TestBed.inject(AccentService), 'set');

    select.value = 'graphite';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(set).toHaveBeenCalledWith('graphite');
    expect(TestBed.inject(AccentService).accent()).toBe('graphite');
    expect(localStorage.getItem(ACCENT_STORAGE_KEY)).toBe('graphite');
  });

  it('ignores a value that is not a preset', () => {
    const { fixture, select } = create();
    const set = vi.spyOn(TestBed.inject(AccentService), 'set');

    fixture.componentInstance.pick('chartreuse');

    expect(set).not.toHaveBeenCalled();
    expect(select.value).toBe('blue');
  });

  it('follows a preset changed elsewhere', () => {
    const { fixture, select } = create();

    TestBed.inject(AccentService).set('cyan');
    fixture.detectChanges();

    expect(select.value).toBe('cyan');
  });
});
