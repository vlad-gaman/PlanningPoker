import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CardOption, GroupStatistics, VoteStatistics } from '../../../models/room';
import { cardImage } from '../card-picker/card-picker';

type Tone = 'good' | 'mid' | 'bad';

/** One slice of the split bar: a mark and how many voted it. */
export interface Segment {
  mark: string;
  text: string;
  image: string | null;
  count: number;
}

/** Agreement at or above this share is "strong"; 100 is unanimous. */
const STRONG_AGREEMENT = 60;

/** The revealed votes: the average in a ring that fills with the agreement, a verdict, and a bar of how the votes split. */
@Component({
  selector: 'app-results',
  templateUrl: './results.html',
  styleUrl: './results.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Results {
  readonly statistics = input.required<VoteStatistics>();
  readonly cards = input.required<CardOption[]>();
  /** How many people voted; the statistics carry shares only, so counts are worked back from it. */
  readonly voters = input.required<number>();

  /** Blank (a dash) when no mark is numeric; a real zero still shows. */
  protected readonly average = computed(() => this.statistics().all.averageMark?.toString() ?? '–');

  /** The biggest share any one mark got, 0 to 100. */
  protected readonly agreement = computed(() =>
    Math.round(Math.max(0, ...this.statistics().all.marks.map((share) => share.percentage))),
  );

  protected readonly verdict = computed((): { label: string; tone: Tone } => {
    const agreement = this.agreement();
    if (agreement >= 100) {
      return { label: 'Unanimous', tone: 'good' };
    }
    return agreement >= STRONG_AGREEMENT
      ? { label: 'Strong agreement', tone: 'mid' }
      : { label: 'Split vote', tone: 'bad' };
  });

  /** The marks cast, biggest share first. */
  protected readonly segments = computed((): Segment[] =>
    [...this.statistics().all.marks]
      .sort((a, b) => b.percentage - a.percentage)
      .map((share) => {
        const card = this.cards().find((option) => option.value === share.mark);
        return {
          mark: share.mark,
          text: card?.display && !cardImage(card) ? card.display : share.mark,
          image: card ? cardImage(card) : null,
          count: Math.round((share.percentage * this.voters()) / 100),
        };
      }),
  );

  /** What the winning mark shows as; null on a tie or without votes. */
  protected readonly leader = computed(() => {
    const mark = this.statistics().all.highestMark;
    return mark === null ? null : (this.segments().find((segment) => segment.mark === mark) ?? null);
  });

  /** Lowest to highest numeric mark, e.g. "3 – 8"; blank when no mark is numeric. */
  protected readonly range = computed(() => {
    const numbers = this.statistics()
      .all.marks.map((share) => Number(share.mark))
      .filter((value) => !Number.isNaN(value));
    return numbers.length > 0 ? `${Math.min(...numbers)} – ${Math.max(...numbers)}` : '';
  });

  /** The averages of the groups that voted. */
  protected readonly groups = computed(() => {
    const { developers, testers, sres } = this.statistics();
    const groups: { label: string; group: GroupStatistics }[] = [
      { label: 'Developers', group: developers },
      { label: 'Testers', group: testers },
      { label: 'SREs', group: sres },
    ];
    return groups
      .filter(({ group }) => group.marks.length > 0)
      .map(({ label, group }) => ({ label, average: group.averageMark?.toString() ?? '–' }));
  });
}
