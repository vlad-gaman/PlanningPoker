import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date('2027-03-04T10:00:00Z'));
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
  });

  afterEach(() => vi.useRealTimers());

  function render(): HTMLElement {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows the logo and name as a link home, and the theme select instead of a cycle button', () => {
    const root = render();

    const brand = root.querySelector('header a') as HTMLAnchorElement;
    expect(brand.getAttribute('href')).toBe('/');
    expect(brand.textContent).toContain('Planning Poker');
    expect(brand.querySelector('img')?.getAttribute('alt')).toBe('Planning Poker logo');
    expect(root.querySelector('header app-theme-select select')).not.toBeNull();
    expect(root.querySelector('header button')).toBeNull();
  });

  it('has the copyright with the current year and a Credits link in the footer', () => {
    const root = render();

    const footer = root.querySelector('footer') as HTMLElement;
    expect(footer.textContent).toContain('© 2027 - Planning Poker');
    const credits = footer.querySelector('a[href="/credits"]') as HTMLAnchorElement;
    expect(credits.textContent).toContain('Credits');
  });

  it('links to Buy Me a Coffee in a new tab', () => {
    const link = render().querySelector('footer a.buymeacoffee-link') as HTMLAnchorElement;

    expect(link.getAttribute('href')).toBe('https://buymeacoffee.com/vladg');
    expect(link.target).toBe('_blank');
    expect(link.rel).toBe('noopener noreferrer');
    expect(link.querySelector('img')?.getAttribute('alt')).toBe('Buy Me A Coffee');
  });
});
