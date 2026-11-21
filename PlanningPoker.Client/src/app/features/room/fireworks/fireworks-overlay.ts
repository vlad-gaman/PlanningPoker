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
/** ...stays out this long (after sliding out), unless the fireworks stop first... */
export const PANDA_VISIBLE_MS = 4000;
/** ...and takes this long to slide out from, and back behind, its edge. */
export const PANDA_SLIDE_MS = 400;
export const PANDA_SIZE = 72;
/** How much of the panda sticks out from behind its edge. */
export const PANDA_VISIBLE_SHARE = 0.6;
/** Least distance between the panda and the edges of the window. */
export const PANDA_MARGIN = 8;
/** Most the panda tilts from upright when it peeks over the top of something or up from the bottom of the window. */
export const PANDA_MAX_TILT_DEG = 35;
/** Most it leans when it peeks in from the side of the window; short of 90 so its head stays above its body. */
export const PANDA_MAX_SIDE_TILT_DEG = 70;
/** The things in the room the panda may peek over the top of. */
export const PANDA_EDGE_SELECTOR = '.strip, ul.people, app-results, app-round-history';
export const PANDA_LINK = 'https://www.google.com/search?q=why+are+fireworks+bad+for+red+pandas';
export const PANDA_LABEL = 'Why fireworks hurt red pandas';

interface PandaSpot {
  /** The unrotated square the panda is drawn in; what sticks out past its edge is cut off at the clip. */
  left: number;
  top: number;
  size: number;
  /** CSS clip-path that cuts the panda off at its edge. */
  clip: string;
  /** Tilt from upright, in degrees; positive leans the head to the right. */
  angle: number;
  /** How far it is pushed back behind its edge while it is not out yet, along its own axis. */
  hide: number;
}

type Edge =
  | { kind: 'top'; y: number; from: number; to: number }
  | { kind: 'bottom' }
  | { kind: 'left' }
  | { kind: 'right' };

/**
 * A full-page layer the fireworks are drawn on; it never takes clicks, so the room stays usable underneath. The one
 * exception is the red panda that peeks out from behind an edge during the show: it links to a search about why
 * fireworks harm red pandas.
 */
@Component({
  selector: 'app-fireworks',
  template: `
    @if (panda(); as spot) {
      <div
        class="panda-clip"
        [style.left.px]="spot.left"
        [style.top.px]="spot.top"
        [style.width.px]="spot.size"
        [style.height.px]="spot.size"
        [style.clip-path]="spot.clip"
      >
        <a
          class="panda"
          [class.leaving]="leaving()"
          [href]="link"
          target="_blank"
          rel="noopener noreferrer"
          [attr.aria-label]="label"
          [style.--angle]="spot.angle + 'deg'"
          [style.--hide]="spot.hide + 'px'"
          (click)="hidePanda()"
        >
          <img src="/red-panda.svg" alt="" draggable="false" />
        </a>
      </div>
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
  protected readonly leaving = signal(false);

  private display: FireworksDisplay | null = null;
  private timer: ReturnType<typeof setInterval> | null = null;
  private linger: ReturnType<typeof setTimeout> | null = null;
  private pandaArrival: ReturnType<typeof setTimeout> | null = null;
  private pandaDeparture: ReturnType<typeof setTimeout> | null = null;
  private pandaGone: ReturnType<typeof setTimeout> | null = null;

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
    for (const timer of [this.pandaArrival, this.pandaDeparture, this.pandaGone]) {
      if (timer !== null) {
        clearTimeout(timer);
      }
    }
    this.pandaArrival = this.pandaDeparture = this.pandaGone = null;
    this.leaving.set(false);
    this.panda.set(null);
  }

  /** Picks an edge to peek from, a spot along it and a tilt, so the head is always above the body. */
  private showPanda(): void {
    this.pandaArrival = null;
    const view = this.document.defaultView;
    const width = view?.innerWidth ?? 0;
    const height = view?.innerHeight ?? 0;
    const size = Math.max(0, Math.min(PANDA_SIZE, width - 2 * PANDA_MARGIN, height - 2 * PANDA_MARGIN));
    const hidden = size * (1 - PANDA_VISIBLE_SHARE);
    const edges = this.edges(width, height, size);
    const edge = edges[Math.min(edges.length - 1, Math.floor(this.random() * edges.length))];
    const along = (from: number, to: number) => from + this.random() * Math.max(0, to - from);

    let spot: PandaSpot;
    if (edge.kind === 'top' || edge.kind === 'bottom') {
      const y = edge.kind === 'top' ? edge.y : height;
      const from = edge.kind === 'top' ? edge.from : PANDA_MARGIN;
      const to = edge.kind === 'top' ? edge.to : width - PANDA_MARGIN;
      spot = {
        left: along(from, to - size),
        top: y - size + hidden,
        size,
        clip: `inset(-${size}px -${size}px ${hidden}px -${size}px)`,
        angle: (this.random() * 2 - 1) * PANDA_MAX_TILT_DEG,
        hide: hidden,
      };
    } else {
      const fromLeft = edge.kind === 'left';
      spot = {
        left: fromLeft ? -hidden : width - size + hidden,
        top: along(PANDA_MARGIN, height - size - PANDA_MARGIN),
        size,
        clip: 'none',
        angle: (fromLeft ? 1 : -1) * this.random() * PANDA_MAX_SIDE_TILT_DEG,
        hide: hidden,
      };
    }
    this.leaving.set(false);
    this.panda.set(spot);
    this.pandaDeparture = setTimeout(() => this.leave(), PANDA_SLIDE_MS + PANDA_VISIBLE_MS);
  }

  private leave(): void {
    this.pandaDeparture = null;
    this.leaving.set(true);
    this.pandaGone = setTimeout(() => this.hidePanda(), PANDA_SLIDE_MS);
  }

  /** What the panda can peek out from behind right now: the window's edges, and the top of the room's cards and panels. */
  private edges(width: number, height: number, size: number): Edge[] {
    const edges: Edge[] = [{ kind: 'bottom' }, { kind: 'left' }, { kind: 'right' }];
    const room = size * PANDA_VISIBLE_SHARE + PANDA_MARGIN + size * 0.3;
    for (const element of Array.from(this.document.querySelectorAll(PANDA_EDGE_SELECTOR))) {
      const box = element.getBoundingClientRect();
      const from = Math.max(box.left, PANDA_MARGIN);
      const to = Math.min(box.right, width - PANDA_MARGIN);
      if (to - from >= size && box.top >= room && box.top < height) {
        edges.push({ kind: 'top', y: box.top, from, to });
      }
    }
    return edges;
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
