import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoundResult } from '../../../models/room';
import { RoundHistory } from './round-history';

const ROUNDS: RoundResult[] = [
  { round: 3, averageMark: 5.5, revealedAt: '2026-03-01T10:30:00+00:00' },
  { round: 2, averageMark: null, revealedAt: '2026-03-01T10:10:00+00:00' },
  { round: 1, averageMark: 3, revealedAt: '2026-03-01T10:00:00+00:00' },
];

describe('RoundHistory', () => {
  function render(rounds: RoundResult[]): { fixture: ComponentFixture<RoundHistory>; element: HTMLElement } {
    const fixture = TestBed.createComponent(RoundHistory);
    fixture.componentRef.setInput('rounds', rounds);
    fixture.detectChanges();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  const items = (element: HTMLElement) => Array.from(element.querySelectorAll('li'));

  it('has a heading', () => {
    const { element } = render(ROUNDS);

    expect(element.querySelector('h2')?.textContent).toBe('Round history');
  });

  it('lists each round in the order given, newest first', () => {
    const { element } = render(ROUNDS);

    expect(items(element).map((item) => item.querySelector('.round')?.textContent)).toEqual(['Round 3', 'Round 2', 'Round 1']);
  });

  it('shows the average of a round', () => {
    const { element } = render(ROUNDS);

    expect(items(element)[0].querySelector('.average')?.textContent?.trim()).toBe('5.5');
    expect(items(element)[2].querySelector('.average')?.textContent?.trim()).toBe('3');
  });

  it('shows a dash when a round has no average', () => {
    const { element } = render(ROUNDS);
    const average = items(element)[1].querySelector('.average')!;

    expect(average.textContent?.trim()).toBe('–');
    expect(average.classList).toContain('none');
  });

  it.each([
    [1.234, '1.23'],
    [2.5, '2.5'],
    [8, '8'],
    [0.5, '0.5'],
  ])('shows %d as %s', (average, expected) => {
    const { element } = render([{ round: 1, averageMark: average, revealedAt: '2026-03-01T10:00:00+00:00' }]);

    expect(element.querySelector('.average')?.textContent?.trim()).toBe(expected);
  });

  it('says so when nothing has been revealed yet', () => {
    const { element } = render([]);

    expect(items(element)).toHaveLength(0);
    expect(element.textContent).toContain('No rounds revealed yet.');
  });

  it('marks the time of each reveal', () => {
    const { element } = render(ROUNDS);

    expect(items(element)[0].querySelector('time')?.getAttribute('datetime')).toBe('2026-03-01T10:30:00+00:00');
  });

  it('shows nothing about who voted what', () => {
    const { element } = render(ROUNDS);

    expect((element.textContent ?? '').toLowerCase()).not.toContain('vote');
  });

  it('follows a change of the rounds', () => {
    const { fixture, element } = render([]);

    fixture.componentRef.setInput('rounds', ROUNDS);
    fixture.detectChanges();

    expect(items(element)).toHaveLength(3);
  });

  it('keeps one entry per round number', () => {
    const { element } = render([{ round: 4, averageMark: 2, revealedAt: '2026-03-01T10:00:00+00:00' }]);

    expect(items(element)).toHaveLength(1);
  });
});
