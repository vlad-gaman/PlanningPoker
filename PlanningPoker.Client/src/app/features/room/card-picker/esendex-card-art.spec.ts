import { TestBed } from '@angular/core/testing';
import { CardOption } from '../../../models/room';
import { cardArt, cardText } from './card-picker';
import { EsendexCardArt } from './esendex-card-art';

/** The text each illustration draws, in order. Only the value is drawn on it: the corners carry no numbers. */
const DRAWN_TEXT: Record<string, string[]> = {
  'esendex-1': ['1'],
  'esendex-2': ['2'],
  'esendex-3': ['3'],
  'esendex-5': ['5'],
  'esendex-8': ['8'],
  'esendex-13': ['13'],
  'esendex-30': ['30', '20', '10', '0', '30'],
  'esendex-100': ['100'],
  'esendex-question': ['?'],
  'esendex-coffee': [],
};

describe('EsendexCardArt', () => {
  function render(art: string): SVGSVGElement {
    const fixture = TestBed.createComponent(EsendexCardArt);
    fixture.componentRef.setInput('art', art);
    fixture.componentRef.setInput('label', 'value');
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).querySelector('svg') as SVGSVGElement;
  }

  for (const art of Object.keys(DRAWN_TEXT)) {
    it(`draws ${art} as a labelled svg with only its own text`, () => {
      const svg = render(art);

      expect(svg.getAttribute('role')).toBe('img');
      expect(svg.getAttribute('aria-label')).toBe('value');
      expect(Array.from(svg.querySelectorAll('text')).map((text) => text.textContent)).toEqual(
        DRAWN_TEXT[art],
      );
      expect(svg.querySelectorAll('*').length).toBeGreaterThan(1);
    });
  }

  it('draws nothing for an unknown illustration', () => {
    const drawn = Array.from(render('esendex-unknown').children).filter((child) => child.tagName !== 'defs');
    expect(drawn.length).toBe(0);
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
