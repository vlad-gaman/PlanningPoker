import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
import { formatRoundTime, spokenRoundTime } from './format-round-time';

const TICK_MILLISECONDS = 1000;

/** Counts up from the moment the round began, once a second. The start is the server's stamp, so every client agrees. */
@Component({
  selector: 'app-round-timer',
  templateUrl: './round-timer.html',
  styleUrl: './round-timer.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoundTimer {
  /** When the round began, as milliseconds since the epoch. */
  readonly startedAt = input.required<number>();

  private readonly now = signal(Date.now());
  protected readonly elapsedSeconds = computed(() => Math.max(0, Math.floor((this.now() - this.startedAt()) / 1000)));
  protected readonly text = computed(() => formatRoundTime(this.elapsedSeconds()));
  /** A timer role is not announced on every change, so the per-second ticks stay silent. */
  protected readonly label = computed(() => `Round time ${spokenRoundTime(this.elapsedSeconds())}`);

  constructor() {
    const timer = setInterval(() => this.now.set(Date.now()), TICK_MILLISECONDS);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }
}
