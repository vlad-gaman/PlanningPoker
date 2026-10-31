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

  private readonly canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly factory = inject(ChartFactory);
  private readonly theme = inject(ThemeService);
  private chart: ChartHandle | null = null;

  protected readonly summary = computed(() => chartSummary(this.title(), this.group()));
  /** Blank when no mark is numeric; a real zero still shows. */
  protected readonly average = computed(() => this.group().averageMark?.toString() ?? '');

  constructor() {
    effect(() => {
      const group = this.group();
      const cards = this.cards();
      this.theme.effective();
      const element = this.canvas().nativeElement;
      this.chart?.destroy();
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
