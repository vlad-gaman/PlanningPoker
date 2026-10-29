import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CardOption, VoteStatistics } from '../../../models/room';
import { ResultsChart } from './results-chart';

/** The three charts shown once the votes are revealed. */
@Component({
  selector: 'app-results',
  imports: [ResultsChart],
  templateUrl: './results.html',
  styleUrl: './results.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Results {
  readonly statistics = input.required<VoteStatistics>();
  readonly cards = input.required<CardOption[]>();
}
