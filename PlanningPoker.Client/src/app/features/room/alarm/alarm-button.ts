import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, output, signal } from '@angular/core';
import { formatRoundTime, spokenRoundTime } from '../round-timer/format-round-time';
import { AlarmState } from '../room-store';

const TICK_MILLISECONDS = 1000;
const SECONDS_PER_MINUTE = 60;

/**
 * The room's Alarm, next to the round timer: start it while idle, watch it count down and cancel it while running,
 * reset it once it elapsed. The icon is the one of the sound this person chose, and it jingles when the Alarm elapses.
 */
@Component({
  selector: 'app-alarm-button',
  templateUrl: './alarm-button.html',
  styleUrl: './alarm-button.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AlarmButton {
  readonly alarm = input.required<AlarmState>();
  /** False when the room has turned the Alarm off: nothing is shown. */
  readonly enabled = input.required<boolean>();
  /** The room's Alarm length, shown while idle. */
  readonly minutes = input.required<number>();
  /** The icon of this person's chosen sound. */
  readonly icon = input.required<string>();

  readonly started = output<void>();
  readonly cancelled = output<void>();
  readonly dismissed = output<void>();

  private readonly now = signal(Date.now());
  private readonly remainingSeconds = computed(() => {
    const endsAt = this.alarm().endsAt;
    return endsAt === null ? 0 : Math.max(0, Math.ceil((endsAt - this.now()) / 1000));
  });

  protected readonly text = computed(() => {
    switch (this.alarm().status) {
      case 'running':
        return formatRoundTime(this.remainingSeconds());
      case 'elapsed':
        return "Time's up";
      default:
        return formatRoundTime(this.minutes() * SECONDS_PER_MINUTE);
    }
  });

  protected readonly label = computed(() => {
    switch (this.alarm().status) {
      case 'running':
        return `Cancel the alarm, ${spokenRoundTime(this.remainingSeconds())} left`;
      case 'elapsed':
        return 'The alarm elapsed. Reset it';
      default:
        return `Start the ${this.minutes()} minute alarm`;
    }
  });

  constructor() {
    const timer = setInterval(() => this.now.set(Date.now()), TICK_MILLISECONDS);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  protected press(): void {
    switch (this.alarm().status) {
      case 'running':
        this.cancelled.emit();
        break;
      case 'elapsed':
        this.dismissed.emit();
        break;
      default:
        this.started.emit();
    }
  }
}
