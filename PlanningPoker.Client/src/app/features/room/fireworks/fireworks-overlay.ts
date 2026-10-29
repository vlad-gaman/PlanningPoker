import { ChangeDetectionStrategy, Component, DOCUMENT, ElementRef, OnDestroy, inject } from '@angular/core';
import { FireworksDisplay, FireworksLauncher } from './fireworks-launcher';

export const FIREWORK_ROUNDS = 10;
export const FIREWORK_ROUND_MS = 1000;
export const FIREWORKS_PER_ROUND = 10;

/** A full-page layer the fireworks are drawn on; it never takes clicks, so the room stays usable underneath. */
@Component({
  selector: 'app-fireworks',
  template: '',
  styleUrl: './fireworks-overlay.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FireworksOverlay implements OnDestroy {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly launcher = inject(FireworksLauncher);
  private readonly document = inject(DOCUMENT);

  private display: FireworksDisplay | null = null;
  private timer: ReturnType<typeof setInterval> | null = null;

  /** Fires a round now and one more each second, for ten rounds; a display already running is restarted. */
  start(): void {
    this.stop();
    if (this.document.hidden) {
      return;
    }
    try {
      this.display = this.launcher.create(this.host.nativeElement);
    } catch {
      // Fireworks are decoration; a browser that cannot draw them must not break the room.
      return;
    }
    let rounds = 0;
    const round = () => {
      if (!this.document.hidden) {
        this.display?.launch(FIREWORKS_PER_ROUND);
      }
      if (++rounds >= FIREWORK_ROUNDS) {
        this.clearTimer();
      }
    };
    round();
    if (rounds < FIREWORK_ROUNDS) {
      this.timer = setInterval(round, FIREWORK_ROUND_MS);
    }
  }

  stop(): void {
    this.clearTimer();
    this.display?.dispose();
    this.display = null;
  }

  ngOnDestroy(): void {
    this.stop();
  }

  private clearTimer(): void {
    if (this.timer !== null) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }
}
