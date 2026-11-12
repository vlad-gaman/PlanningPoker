import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RoundResult } from '../../../models/room';

/** The average vote of each revealed round of the room, newest first, so the team can compare estimates across the session. Never shows who voted what. */
@Component({
  selector: 'app-round-history',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './round-history.html',
  styleUrl: './round-history.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoundHistory {
  /** The revealed rounds, newest first, as the server sends them. */
  readonly rounds = input.required<RoundResult[]>();
}
