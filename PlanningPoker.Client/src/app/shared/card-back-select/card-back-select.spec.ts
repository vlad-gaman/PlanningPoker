import { TestBed } from '@angular/core/testing';
import { CARD_BACKS, CARD_BACK_STORAGE_KEY } from '../../core/card-back.service';
import { CardBackSelect } from './card-back-select';

describe('CardBackSelect', () => {
  beforeEach(() => localStorage.clear());

  function render() {
    const fixture = TestBed.createComponent(CardBackSelect);
    fixture.detectChanges();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  const radios = (element: HTMLElement) => Array.from(element.querySelectorAll<HTMLButtonElement>('[role="radio"]'));

  it('offers every design as a radio, drawn with its own back', () => {
    const { element } = render();

    expect(radios(element)).toHaveLength(CARD_BACKS.length);
    expect(element.querySelectorAll('.card-back[data-back]')).toHaveLength(CARD_BACKS.length);
    expect(radios(element).map((radio) => radio.textContent?.trim())).toEqual(CARD_BACKS.map((back) => back.label));
  });

  it('marks the stored design as checked', () => {
    localStorage.setItem(CARD_BACK_STORAGE_KEY, 'dots');
    const { element } = render();

    const checked = radios(element).filter((radio) => radio.getAttribute('aria-checked') === 'true');
    expect(checked.map((radio) => radio.title)).toEqual(['Dots']);
  });

  it('picks and remembers a design when it is clicked', () => {
    const { fixture, element } = render();

    radios(element)[3].click();
    fixture.detectChanges();

    expect(localStorage.getItem(CARD_BACK_STORAGE_KEY)).toBe('geometric');
    expect(radios(element)[3].getAttribute('aria-checked')).toBe('true');
  });
});
