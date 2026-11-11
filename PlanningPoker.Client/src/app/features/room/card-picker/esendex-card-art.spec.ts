import { TestBed } from '@angular/core/testing';
import { CardOption } from '../../../models/room';
import { cardArt, cardText } from './card-picker';
import { EsendexCardArt } from './esendex-card-art';

const ARTS = [
  'esendex-1',
  'esendex-2',
  'esendex-3',
  'esendex-5',
  'esendex-8',
  'esendex-13',
  'esendex-30',
  'esendex-100',
  'esendex-question',
  'esendex-coffee',
];

describe('EsendexCardArt', () => {
  function render(art: string): HTMLElement {
    const fixture = TestBed.createComponent(EsendexCardArt);
    fixture.componentRef.setInput('art', art);
    fixture.componentRef.setInput('label', 'value');
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  for (const art of ARTS) {
    it(`draws ${art} as a labelled image`, () => {
      const img = render(art).querySelector('img') as HTMLImageElement;

      expect(img.getAttribute('alt')).toBe('value');
      expect(img.getAttribute('src')).toMatch(/^esendex\/[\w]+\.svg$/);
    });
  }

  it('draws nothing for an unknown illustration', () => {
    expect(render('esendex-unknown').querySelector('img')).toBeNull();
  });
});

describe('card displays of the Esendex set', () => {
  const card = (display: string | null): CardOption => ({ value: '1', display });

  it('reads the illustration name from an art display', () => {
    expect(cardArt(card('art:esendex-30'))).toBe('esendex-30');
  });

  it('reads no illustration from text, emoji, image or empty displays', () => {
    expect(cardArt(card('☕'))).toBeNull();
    expect(cardArt(card('five-finger-512x512.png'))).toBeNull();
    expect(cardArt(card(null))).toBeNull();
  });

  it('reads text only for text displays', () => {
    expect(cardText(card('½'))).toBe('½');
    expect(cardText(card('art:esendex-coffee'))).toBeNull();
    expect(cardText(card('five-finger-512x512.png'))).toBeNull();
    expect(cardText(card(null))).toBeNull();
  });
});
