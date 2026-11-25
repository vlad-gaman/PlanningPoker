import { TestBed } from '@angular/core/testing';
import { CARD_BACKS, CARD_BACK_STORAGE_KEY, CardBackService } from '../../core/card-back.service';
import { CardBackSelect } from './card-back-select';

describe('CardBackSelect', () => {
  beforeEach(() => localStorage.clear());

  function create() {
    const fixture = TestBed.createComponent(CardBackSelect);
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

  it('shows the stored design on a labelled button and the list closed', () => {
    localStorage.setItem(CARD_BACK_STORAGE_KEY, 'dots');

    const { element, trigger, options } = create();

    expect(trigger.getAttribute('aria-label')).toBe('Card back');
    expect(trigger.getAttribute('aria-expanded')).toBe('false');
    expect(trigger.textContent?.trim()).toBe('Dots');
    expect(element.querySelector('.trigger .card-back')?.getAttribute('data-back')).toBe('dots');
    expect(options()).toEqual([]);
  });

  it('lists every design drawn with its own back when opened', () => {
    const { fixture, trigger, options } = create();

    trigger.click();
    fixture.detectChanges();

    expect(trigger.getAttribute('aria-expanded')).toBe('true');
    expect(options().map((option) => option.textContent?.trim())).toEqual(CARD_BACKS.map((back) => back.label));
    expect(options().map((option) => option.querySelector('.card-back')?.getAttribute('data-back'))).toEqual(
      CARD_BACKS.map((back) => back.id),
    );
    expect(options()[0].getAttribute('aria-selected')).toBe('true');
  });

  it('applies and persists a clicked design, then closes', () => {
    const { fixture, trigger, options } = create();

    trigger.click();
    fixture.detectChanges();
    options()[3].click();
    fixture.detectChanges();

    expect(TestBed.inject(CardBackService).cardBack()).toBe('geometric');
    expect(localStorage.getItem(CARD_BACK_STORAGE_KEY)).toBe('geometric');
    expect(trigger.getAttribute('aria-expanded')).toBe('false');
    expect(trigger.textContent?.trim()).toBe('Geometric');
  });

  it('opens on the arrow keys, moves with them and picks with Enter', () => {
    const { trigger, press } = create();

    press('ArrowDown');
    expect(trigger.getAttribute('aria-expanded')).toBe('true');
    press('ArrowDown');
    press('ArrowDown');
    expect(trigger.getAttribute('aria-activedescendant')).toBe('card-back-option-dots');
    press('Enter');

    expect(TestBed.inject(CardBackService).cardBack()).toBe('dots');
    expect(trigger.getAttribute('aria-expanded')).toBe('false');
  });

  it('closes on Escape without changing the design', () => {
    const { trigger, press } = create();

    press('ArrowDown');
    press('ArrowDown');
    press('Escape');

    expect(trigger.getAttribute('aria-expanded')).toBe('false');
    expect(TestBed.inject(CardBackService).cardBack()).toBe('classic');
  });

  it('ignores a value that is not a design', () => {
    const { fixture } = create();
    const service = TestBed.inject(CardBackService);

    fixture.componentInstance.pick('sparkles');

    expect(service.cardBack()).toBe('classic');
  });
});
