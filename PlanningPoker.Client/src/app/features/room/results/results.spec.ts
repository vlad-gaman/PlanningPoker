import { TestBed } from '@angular/core/testing';
import { CardOption, VoteStatistics } from '../../../models/room';
import { CONSENSUS_STATISTICS, STATISTICS } from '../../../testing/fake-hub.testing';
import { RESULTS_BY_ROLE_STORAGE_KEY } from '../../../core/results-view.service';
import { Results } from './results';

describe('Results', () => {
  beforeEach(() => localStorage.clear());

  function render(statistics: unknown, voters: number, cards: CardOption[] = []) {
    const fixture = TestBed.createComponent(Results);
    fixture.componentRef.setInput('statistics', { ...(statistics as VoteStatistics), voteCount: voters });
    fixture.componentRef.setInput('cards', cards);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const text = (selector: string) => element.querySelector(selector)?.textContent?.replace(/\s+/g, ' ').trim();
    return { fixture, element, text };
  }

  const byRoleOn = () => localStorage.setItem(RESULTS_BY_ROLE_STORAGE_KEY, 'on');

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

  it('shows the number, not the art name, for an illustrated card', () => {
    const statistics = { ...CONSENSUS_STATISTICS, all: { marks: [{ mark: '13', percentage: 100 }], highestMark: '13', averageMark: 13 } };
    const { element } = render(statistics, 2, [{ value: '13', display: 'art:esendex-13' }]);

    expect(element.querySelector('.segment b')?.textContent).toBe('13');
    expect(element.textContent).not.toContain('art:');
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

  const byRoleSwitch = (element: HTMLElement) => element.querySelector('.by-role input[role="switch"]') as HTMLInputElement;

  describe('by role', () => {
    it('is off by default, with the averages list and no role rows', () => {
      const { element } = render(STATISTICS, 2);

      expect(byRoleSwitch(element).checked).toBe(false);
      expect(element.querySelector('.roles')).toBeNull();
      expect(element.querySelector('.groups')).not.toBeNull();
    });

    it('is on when the stored value is on', () => {
      byRoleOn();

      const { element } = render(STATISTICS, 2);

      expect(byRoleSwitch(element).checked).toBe(true);
    });

    it('turns on when the toggle is clicked, and stores the choice', () => {
      const { fixture, element } = render(STATISTICS, 2);

      byRoleSwitch(element).click();
      fixture.detectChanges();

      expect(byRoleSwitch(element).checked).toBe(true);
      expect(element.querySelector('.roles')).not.toBeNull();
      expect(localStorage.getItem(RESULTS_BY_ROLE_STORAGE_KEY)).toBe('on');
    });

    it('replaces the averages list with a row per role', () => {
      byRoleOn();

      const { element, text } = render(STATISTICS, 2);

      expect(element.querySelector('.groups')).toBeNull();
      expect([...element.querySelectorAll('.role')].map((row) => row.querySelector('.role-label')?.textContent)).toEqual([
        'Developers',
        'Testers',
      ]);
      expect([...element.querySelectorAll('.role-average')].map((el) => el.textContent?.trim())).toEqual(['1', '5']);
      expect(text('.role-count')).toBe('1 vote');
    });

    it('hides a role with no votes', () => {
      byRoleOn();

      const { element } = render(STATISTICS, 2);

      expect(element.textContent).not.toContain('SREs');
    });

    it('counts a role bar by its own votes, not the overall total', () => {
      byRoleOn();
      const statistics = {
        all: {
          marks: [
            { mark: '3', percentage: 66.7 },
            { mark: '13', percentage: 33.3 },
          ],
          highestMark: '3',
          averageMark: 5,
          voteCount: 3,
        },
        developers: { marks: [{ mark: '3', percentage: 100 }], highestMark: '3', averageMark: 3, voteCount: 2 },
        testers: { marks: [], highestMark: null, averageMark: null, voteCount: 0 },
        sres: { marks: [], highestMark: null, averageMark: null, voteCount: 0 },
      };

      const { element } = render(statistics, 3);

      const segments = [...element.querySelectorAll('.role .segment')].map(
        (el) => `${el.querySelector('b')?.textContent} x${el.querySelector('small')?.textContent}`,
      );
      expect(segments).toEqual(['3 x2']);
      expect(element.querySelector('.role-count')?.textContent?.trim()).toBe('2 votes');
    });

    it('colours each role segment the same as its mark in the overall bar', () => {
      byRoleOn();
      const statistics = {
        all: {
          marks: [
            { mark: '5', percentage: 60 },
            { mark: '3', percentage: 40 },
          ],
          highestMark: '5',
          averageMark: 4.2,
          voteCount: 5,
        },
        developers: { marks: [], highestMark: null, averageMark: null, voteCount: 0 },
        testers: { marks: [{ mark: '3', percentage: 100 }], highestMark: '3', averageMark: 3, voteCount: 2 },
        sres: { marks: [{ mark: '5', percentage: 100 }], highestMark: '5', averageMark: 5, voteCount: 3 },
      };

      const { element } = render(statistics, 5);

      const roleStyles = [...element.querySelectorAll('.role .segment')].map((el) => (el as HTMLElement).style.background);
      expect(roleStyles[0]).toContain('var(--seg-2)');
      expect(roleStyles[1]).toContain('var(--seg-1)');
    });

    it('says so when no role voted, as the spike did', () => {
      byRoleOn();
      const statistics = {
        all: { marks: [], highestMark: null, averageMark: null, voteCount: 0 },
        developers: { marks: [], highestMark: null, averageMark: null, voteCount: 0 },
        testers: { marks: [], highestMark: null, averageMark: null, voteCount: 0 },
        sres: { marks: [], highestMark: null, averageMark: null, voteCount: 0 },
      };

      const { element, text } = render(statistics, 0);

      expect(element.querySelector('.roles')).toBeNull();
      expect(text('.roles-empty')).toBe('No votes from Developers, Testers or SREs.');
    });

    it('leaves the overall bar unchanged', () => {
      byRoleOn();

      const { element } = render(STATISTICS, 2);

      const overall = element.querySelector('ul[aria-label="How the votes split"]');
      expect([...(overall?.querySelectorAll('.segment') ?? [])].map((el) => el.querySelector('small')?.textContent)).toEqual(['1', '1']);
    });
  });
});
