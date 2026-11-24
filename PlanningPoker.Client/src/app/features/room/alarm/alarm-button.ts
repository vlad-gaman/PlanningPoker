import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, output, signal, untracked } from '@angular/core';
import { formatRoundTime, spokenRoundTime } from '../round-timer/format-round-time';
import { AlarmState } from '../../../models/room';

export const MILLISECONDS_PER_SECOND = 1000;
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
  /** True for the few seconds after this person saw the Alarm elapse; the icon shakes then. */
  readonly jingle = input.required<boolean>();

  readonly started = output<void>();
  readonly cancelled = output<void>();
  readonly dismissed = output<void>();

  private readonly now = signal(Date.now());
  private readonly remainingSeconds = computed(() => {
    const endsAt = this.alarm().endsAt;
    return endsAt === null ? 0 : Math.max(0, Math.ceil((endsAt - this.now()) / MILLISECONDS_PER_SECOND));
  });

  /** What the button shows, says to a screen reader, and does when pressed, for the state the Alarm is in. */
  private readonly view = computed(() => {
    switch (this.alarm().status) {
      case 'running':
        return {
          text: formatRoundTime(this.remainingSeconds()),
          label: `Cancel the alarm, ${spokenRoundTime(this.remainingSeconds())} left`,
          press: () => this.cancelled.emit(),
        };
      case 'elapsed':
        return { text: "Time's up", label: 'The alarm elapsed. Reset it', press: () => this.dismissed.emit() };
      default:
        return {
          text: formatRoundTime(this.minutes() * SECONDS_PER_MINUTE),
          label: `Start the ${this.minutes()} minute alarm`,
          press: () => this.started.emit(),
        };
    }
  });
  protected readonly text = computed(() => this.view().text);
  protected readonly label = computed(() => this.view().label);

  constructor() {
    // A new state is read against the clock as it is now: the ticker may last have run almost a second ago.
    effect(() => {
      this.alarm();
      untracked(() => this.now.set(Date.now()));
    });
    const timer = setInterval(() => this.now.set(Date.now()), MILLISECONDS_PER_SECOND);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  protected press(): void {
    this.view().press();
  }
}
