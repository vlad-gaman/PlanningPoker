import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  computed,
  effect,
  inject,
  input,
  viewChild,
} from '@angular/core';
import { ThemeService } from '../../../core/theme.service';
import { CardOption, GroupStatistics } from '../../../models/room';
import { cardImage } from '../card-picker/card-picker';
import { buildChartConfig, chartSummary, readChartColors } from './chart-config';
import { ChartFactory, ChartHandle } from './chart-factory';

/** One group's bar chart with its average mark underneath. */
@Component({
  selector: 'app-results-chart',
  templateUrl: './results-chart.html',
  styleUrl: './results-chart.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResultsChart implements OnDestroy {
  readonly title = input.required<string>();
  readonly group = input.required<GroupStatistics>();
  readonly cards = input.required<CardOption[]>();

  private readonly canvas = viewChild<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly factory = inject(ChartFactory);
  private readonly theme = inject(ThemeService);
  private chart: ChartHandle | null = null;

  protected readonly summary = computed(() => chartSummary(this.title(), this.group()));
  /** Blank when no mark is numeric; a real zero still shows. */
  protected readonly average = computed(() => this.group().averageMark?.toString() ?? '');

  protected readonly empty = computed(() => this.group().marks.length === 0);
  /** What the strictly highest mark shows as; null on a tie or without votes. An image card shows its mark. */
  protected readonly mostVotes = computed(() => {
    const mark = this.group().highestMark;
    if (mark === null) {
      return null;
    }
    const card = this.cards().find((option) => option.value === mark);
    return card?.display && !cardImage(card) ? card.display : mark;
  });

  constructor() {
    effect(() => {
      const group = this.group();
      const cards = this.cards();
      this.theme.effective();
      const element = this.canvas()?.nativeElement;
      this.chart?.destroy();
      this.chart = null;
      if (!element || group.marks.length === 0) {
        return;
      }
      this.chart = this.factory.create(
        element,
        buildChartConfig(group, cards, readChartColors(element), () => this.chart?.draw()),
      );
    });
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
    this.chart = null;
  }
}
