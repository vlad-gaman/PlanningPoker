import { TestBed } from '@angular/core/testing';
import { Credits } from './credits';

describe('Credits', () => {
  it('credits the charting and fireworks libraries with links', () => {
    const fixture = TestBed.createComponent(Credits);
    fixture.detectChanges();

    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.map((link) => link.textContent)).toEqual(['Chart.js', 'fireworks-js']);
    expect(links.every((link) => link.rel === 'noopener noreferrer')).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('MIT license');
  });
});
