import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CardOption } from '../../../models/room';
import { CardPicker, cardImage } from './card-picker';

const CARDS: CardOption[] = [
  { value: '1', display: null },
  { value: '0.5', display: '½' },
  { value: 'five', display: 'five-finger-512x512.png' },
];

describe('CardPicker', () => {
  let fixture: ComponentFixture<CardPicker>;
  let element: HTMLElement;

  function render(inputs: { selected?: string | null; disabled?: boolean } = {}) {
    fixture = TestBed.createComponent(CardPicker);
    fixture.componentRef.setInput('cards', CARDS);
    fixture.componentRef.setInput('selected', inputs.selected ?? null);
    fixture.componentRef.setInput('disabled', inputs.disabled ?? false);
    fixture.detectChanges();
    element = fixture.nativeElement as HTMLElement;
  }

  const radios = () => Array.from(element.querySelectorAll<HTMLInputElement>('input[type="radio"]'));

  it('renders a radio per card with its display, falling back to the value', () => {
    render();

    expect(radios()).toHaveLength(3);
    const labels = Array.from(element.querySelectorAll('label')).map((label) => label.textContent?.trim());
    expect(labels.slice(0, 2)).toEqual(['1', '½']);
  });

  it('renders image displays as an img whose alt is the card value', () => {
    render();

    const image = element.querySelector('img') as HTMLImageElement;
    expect(image.getAttribute('src')).toBe('/five-finger-512x512.png');
    expect(image.alt).toBe('five');
  });

  it('checks the selected card', () => {
    render({ selected: '0.5' });

    expect(radios().map((radio) => radio.checked)).toEqual([false, true, false]);
  });

  it('emits the value of the card that was picked', () => {
    render();
    const picked: string[] = [];
    fixture.componentInstance.markSelected.subscribe((mark) => picked.push(mark));

    radios()[1].click();

    expect(picked).toEqual(['0.5']);
  });

  it('disables every card', () => {
    render({ disabled: true });

    expect(radios().every((radio) => radio.disabled)).toBe(true);
  });

  it('cardImage returns the file for png displays only', () => {
    expect(cardImage({ value: 'x', display: 'x.png' })).toBe('/x.png');
    expect(cardImage({ value: '1', display: null })).toBeNull();
    expect(cardImage({ value: 'c', display: '☕' })).toBeNull();
  });
});
