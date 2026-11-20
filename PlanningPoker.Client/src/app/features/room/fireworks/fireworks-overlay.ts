import {
  ChangeDetectionStrategy,
  Component,
  DOCUMENT,
  ElementRef,
  OnDestroy,
  inject,
  signal,
} from '@angular/core';
import { FireworksDisplay, FireworksLauncher } from './fireworks-launcher';
import { PANDA_RANDOM } from './panda-random';

export const FIREWORK_ROUNDS = 10;
export const FIREWORK_ROUND_MS = 1000;
export const FIREWORKS_PER_ROUND = 10;
/** How long the last rockets get to burn out before the display is removed. */
export const FIREWORK_LINGER_MS = 5000;
/** The red panda shows up at a random moment within this long after the fireworks start... */
export const PANDA_APPEAR_WITHIN_MS = 6000;
/** ...and stays this long, unless the fireworks stop first. */
export const PANDA_VISIBLE_MS = 4000;
export const PANDA_SIZE = 72;
/** Least distance between the panda and the edge of the window. */
export const PANDA_MARGIN = 8;
export const PANDA_LINK = 'https://www.google.com/search?q=why+are+fireworks+bad+for+red+pandas';
export const PANDA_LABEL = 'Why fireworks hurt red pandas';

interface PandaSpot {
  left: number;
  top: number;
  size: number;
}

/**
 * A full-page layer the fireworks are drawn on; it never takes clicks, so the room stays usable underneath. The one
 * exception is the red panda that visits during the show: it links to a search about why fireworks harm red pandas.
 */
@Component({
  selector: 'app-fireworks',
  template: `
    @if (panda(); as spot) {
      <a
        class="panda"
        [href]="link"
        target="_blank"
        rel="noopener noreferrer"
        [attr.aria-label]="label"
        [style.left.px]="spot.left"
        [style.top.px]="spot.top"
        [style.width.px]="spot.size"
        [style.height.px]="spot.size"
        (click)="hidePanda()"
      >
        <img src="/red-panda.svg" alt="" draggable="false" />
      </a>
    }
  `,
  styleUrl: './fireworks-overlay.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FireworksOverlay implements OnDestroy {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly launcher = inject(FireworksLauncher);
  private readonly document = inject(DOCUMENT);
  private readonly random = inject(PANDA_RANDOM);

  protected readonly link = PANDA_LINK;
  protected readonly label = PANDA_LABEL;
  protected readonly panda = signal<PandaSpot | null>(null);

  private display: FireworksDisplay | null = null;
  private timer: ReturnType<typeof setInterval> | null = null;
  private linger: ReturnType<typeof setTimeout> | null = null;
  private pandaArrival: ReturnType<typeof setTimeout> | null = null;
  private pandaDeparture: ReturnType<typeof setTimeout> | null = null;

  /** Fires a round now and one more each second, for ten rounds; a display already running is restarted. */
  start(): void {
    this.stop();
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
        this.linger = setTimeout(() => this.stop(), FIREWORK_LINGER_MS);
      }
    };
    round();
    if (rounds < FIREWORK_ROUNDS) {
      this.timer = setInterval(round, FIREWORK_ROUND_MS);
    }
    this.pandaArrival = setTimeout(() => this.showPanda(), this.random() * PANDA_APPEAR_WITHIN_MS);
  }

  stop(): void {
    this.clearTimer();
    this.clearLinger();
    this.display?.dispose();
    this.display = null;
    this.hidePanda();
  }

  protected hidePanda(): void {
    if (this.pandaArrival !== null) {
      clearTimeout(this.pandaArrival);
      this.pandaArrival = null;
    }
    if (this.pandaDeparture !== null) {
      clearTimeout(this.pandaDeparture);
      this.pandaDeparture = null;
    }
    this.panda.set(null);
  }

  private showPanda(): void {
    this.pandaArrival = null;
    const view = this.document.defaultView;
    const width = view?.innerWidth ?? 0;
    const height = view?.innerHeight ?? 0;
    const size = Math.max(0, Math.min(PANDA_SIZE, width - 2 * PANDA_MARGIN, height - 2 * PANDA_MARGIN));
    this.panda.set({
      left: PANDA_MARGIN + this.random() * Math.max(0, width - size - 2 * PANDA_MARGIN),
      top: PANDA_MARGIN + this.random() * Math.max(0, height - size - 2 * PANDA_MARGIN),
      size,
    });
    this.pandaDeparture = setTimeout(() => this.hidePanda(), PANDA_VISIBLE_MS);
  }

  ngOnDestroy(): void {
    this.stop();
  }

  private clearLinger(): void {
    if (this.linger !== null) {
      clearTimeout(this.linger);
      this.linger = null;
    }
  }

  private clearTimer(): void {
    if (this.timer !== null) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }
}
