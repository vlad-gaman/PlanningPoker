import { TestBed } from '@angular/core/testing';
import { CardOption, VoteStatistics } from '../../../models/room';
import { CONSENSUS_STATISTICS, STATISTICS } from '../../../testing/fake-hub.testing';
import { Results } from './results';

describe('Results', () => {
  function render(statistics: unknown, voters: number, cards: CardOption[] = []) {
    const fixture = TestBed.createComponent(Results);
    fixture.componentRef.setInput('statistics', statistics as VoteStatistics);
    fixture.componentRef.setInput('cards', cards);
    fixture.componentRef.setInput('voters', voters);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const text = (selector: string) => element.querySelector(selector)?.textContent?.replace(/\s+/g, ' ').trim();
    return { element, text };
  }

  it('shows the average of all votes in the ring', () => {
    expect(render(STATISTICS, 2).text('.ring .average')).toBe('3');
  });

  it('shows a dash when no mark is numeric', () => {
    const statistics = { ...STATISTICS, all: { marks: [{ mark: '?', percentage: 100 }], highestMark: '?', averageMark: null } };

    expect(render(statistics, 2).text('.ring .average')).toBe('–');
  });

  it('calls a split vote when no mark has a clear majority', () => {
    const { element, text } = render(STATISTICS, 2);

    expect(text('.verdict .label')).toBe('Split vote');
    expect(element.querySelector('.verdict')?.getAttribute('data-tone')).toBe('bad');
    expect(text('.verdict .detail')).toContain('Top share 50%');
    expect(text('.verdict .detail')).toContain('range 1 – 5');
    expect(text('.verdict .detail')).toContain('2 votes');
  });

  it('calls strong agreement from 60 percent', () => {
    const statistics = {
      ...STATISTICS,
      all: {
        marks: [
          { mark: '3', percentage: 20 },
          { mark: '5', percentage: 60 },
          { mark: '8', percentage: 20 },
        ],
        highestMark: '5',
        averageMark: 5.2,
      },
    };
    const { element, text } = render(statistics, 5);

    expect(text('.verdict .label')).toBe('Strong agreement');
    expect(element.querySelector('.verdict')?.getAttribute('data-tone')).toBe('mid');
    expect(text('.verdict .detail')).toContain('60% agree on 5');
  });

  it('calls a unanimous vote good and names the one vote singular', () => {
    const { element, text } = render(CONSENSUS_STATISTICS, 1);

    expect(text('.verdict .label')).toBe('Unanimous');
    expect(element.querySelector('.verdict')?.getAttribute('data-tone')).toBe('good');
    expect(text('.verdict .detail')).toContain('1 vote');
    expect(text('.verdict .detail')).not.toContain('1 votes');
  });

  it('fills the ring with the agreement', () => {
    const ring = render(STATISTICS, 2).element.querySelector('.ring') as HTMLElement;

    expect(ring.style.getPropertyValue('--share')).toBe('50');
  });

  it('splits the bar by vote count, biggest first', () => {
    const statistics = {
      ...STATISTICS,
      all: {
        marks: [
          { mark: '3', percentage: 12.5 },
          { mark: '5', percentage: 50 },
          { mark: '8', percentage: 37.5 },
        ],
        highestMark: '5',
        averageMark: 5.9,
      },
    };
    const { element } = render(statistics, 8);

    const segments = [...element.querySelectorAll('.segment')].map(
      (el) => `${el.querySelector('b')?.textContent} x${el.querySelector('small')?.textContent}`,
    );
    expect(segments).toEqual(['5 x4', '8 x3', '3 x1']);
  });

  it('shows a card display instead of its mark', () => {
    const statistics = { ...CONSENSUS_STATISTICS, all: { marks: [{ mark: '0.5', percentage: 100 }], highestMark: '0.5', averageMark: 0.5 } };
    const { element } = render(statistics, 2, [{ value: '0.5', display: '½' }]);

    expect(element.querySelector('.segment b')?.textContent).toBe('½');
  });

  it('shows an image card as a mask image', () => {
    const statistics = { ...CONSENSUS_STATISTICS, all: { marks: [{ mark: '3', percentage: 100 }], highestMark: '3', averageMark: 3 } };
    const { element } = render(statistics, 2, [{ value: '3', display: 'three.png' }]);

    expect(element.querySelector('.segment img')?.getAttribute('src')).toBe('/three.png');
    expect(element.querySelector('.segment b')).toBeNull();
  });

  it('lists the averages of the groups that voted, and skips groups without votes', () => {
    const { element } = render(STATISTICS, 2);

    const groups = [...element.querySelectorAll('.groups li')].map((el) => el.textContent?.replace(/\s+/g, ' ').trim());
    expect(groups).toEqual(['Developers avg 1', 'Testers avg 5']);
  });

  it('leaves the group list out when nobody in a group voted', () => {
    const statistics = {
      ...CONSENSUS_STATISTICS,
      developers: { marks: [], highestMark: null, averageMark: null },
    };

    expect(render(statistics, 1).element.querySelector('.groups')).toBeNull();
  });
});
