import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Credits } from './credits';

describe('Credits', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideRouter([])] }));

  function render(): HTMLElement {
    const fixture = TestBed.createComponent(Credits);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('credits the seven icon sets with Flaticon links', () => {
    const links = Array.from(render().querySelectorAll('section.icons a')) as HTMLAnchorElement[];

    expect(links.map((l) => l.getAttribute('href'))).toEqual(
      ['poker', 'fist', 'finger', 'two', 'three', 'four', 'fingers'].map(
        (slug) => `https://www.flaticon.com/free-icons/${slug}`,
      ),
    );
    expect(links[0].textContent?.trim()).toBe('Poker icons created by Freepik - Flaticon');
    expect(links[0].title).toBe('poker icons');
  });

  it('credits the charting and fireworks libraries after the icons', () => {
    const root = render();

    const links = Array.from(root.querySelectorAll('section.libraries a')) as HTMLAnchorElement[];
    expect(links.map((l) => l.textContent)).toEqual(['Chart.js', 'fireworks-js']);
    expect(root.textContent).toContain('MIT license');
    expect(root.querySelector('section.icons')!.compareDocumentPosition(root.querySelector('section.libraries')!))
      .toBe(Node.DOCUMENT_POSITION_FOLLOWING);
  });

  it('opens every external link safely in a new tab', () => {
    const links = Array.from(render().querySelectorAll('a[href^="http"]')) as HTMLAnchorElement[];

    expect(links.length).toBe(9);
    expect(links.every((l) => l.target === '_blank' && l.rel === 'noopener noreferrer')).toBe(true);
  });

  it('has a Back to Home button', () => {
    const back = render().querySelector('a.back-home') as HTMLAnchorElement;

    expect(back.textContent).toContain('Back to Home');
    expect(back.getAttribute('href')).toBe('/');
  });
});
