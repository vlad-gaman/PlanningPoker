import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { THEME_STORAGE_KEY } from './core/theme.service';

describe('App', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
  });

  it('has a Credits link in the footer', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const link = (fixture.nativeElement as HTMLElement).querySelector('footer a') as HTMLAnchorElement;

    expect(link.textContent).toContain('Credits');
    expect(link.getAttribute('href')).toBe('/credits');
  });

  it('cycles the theme preference auto -> light -> dark -> auto', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const button = (fixture.nativeElement as HTMLElement).querySelector('button') as HTMLButtonElement;

    button.click();
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('light');
    button.click();
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark');
    button.click();
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('auto');
  });
});
