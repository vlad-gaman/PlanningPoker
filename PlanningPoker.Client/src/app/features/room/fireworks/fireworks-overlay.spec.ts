import { TestBed } from '@angular/core/testing';
import {
  FIREWORK_LINGER_MS,
  PANDA_APPEAR_WITHIN_MS,
  PANDA_LABEL,
  PANDA_LINK,
  PANDA_MARGIN,
  PANDA_MAX_SIDE_TILT_DEG,
  PANDA_MAX_TILT_DEG,
  PANDA_SIZE,
  PANDA_SLIDE_MS,
  PANDA_VISIBLE_MS,
  PANDA_VISIBLE_SHARE,
  FIREWORK_ROUNDS,
  FIREWORK_ROUND_MS,
  FIREWORKS_PER_ROUND,
  FireworksOverlay,
} from './fireworks-overlay';
import { FireworksLauncher } from './fireworks-launcher';
import { PANDA_RANDOM } from './panda-random';

describe('FireworksOverlay', () => {
  let launch: ReturnType<typeof vi.fn>;
  let dispose: ReturnType<typeof vi.fn>;
  let create: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    vi.useFakeTimers();
    launch = vi.fn();
    dispose = vi.fn();
    create = vi.fn(() => ({ launch, dispose }));
    TestBed.configureTestingModule({
      providers: [{ provide: FireworksLauncher, useValue: { create } }],
    });
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  function overlay(): FireworksOverlay {
    return TestBed.createComponent(FireworksOverlay).componentInstance;
  }

  it('draws on its own element and fires a first round straight away', () => {
    const fixture = TestBed.createComponent(FireworksOverlay);

    fixture.componentInstance.start();

    expect(create).toHaveBeenCalledWith(fixture.nativeElement);
    expect(launch).toHaveBeenCalledTimes(1);
    expect(launch).toHaveBeenCalledWith(FIREWORKS_PER_ROUND);
  });

  it('fires one round a second for ten rounds, then stops firing', () => {
    overlay().start();

    vi.advanceTimersByTime(FIREWORK_ROUND_MS * (FIREWORK_ROUNDS + 5));

    expect(launch).toHaveBeenCalledTimes(FIREWORK_ROUNDS);
    expect(vi.getTimerCount()).toBe(0);
  });

  it('restarts a display that is already running', () => {
    const component = overlay();
    component.start();

    component.start();

    expect(dispose).toHaveBeenCalledTimes(1);
    expect(create).toHaveBeenCalledTimes(2);
    vi.advanceTimersByTime(FIREWORK_ROUND_MS * (FIREWORK_ROUNDS + 5));
    expect(launch).toHaveBeenCalledTimes(1 + FIREWORK_ROUNDS);
  });

  it('stops the rounds and removes the display', () => {
    const component = overlay();
    component.start();

    component.stop();
    vi.advanceTimersByTime(FIREWORK_ROUND_MS * 3);

    expect(dispose).toHaveBeenCalledTimes(1);
    expect(launch).toHaveBeenCalledTimes(1);
  });

  it('does nothing when stopped without a display', () => {
    overlay().stop();

    expect(dispose).not.toHaveBeenCalled();
  });

  it('launches nothing when started in a hidden tab, but fires once it is shown again', () => {
    const hidden = vi.spyOn(document, 'hidden', 'get').mockReturnValue(true);
    overlay().start();
    expect(launch).not.toHaveBeenCalled();

    hidden.mockReturnValue(false);
    vi.advanceTimersByTime(FIREWORK_ROUND_MS);

    expect(launch).toHaveBeenCalledTimes(1);
  });

  it('removes the display a few seconds after the last round', () => {
    overlay().start();

    vi.advanceTimersByTime(FIREWORK_ROUND_MS * FIREWORK_ROUNDS);
    expect(dispose).not.toHaveBeenCalled();
    vi.advanceTimersByTime(FIREWORK_LINGER_MS);

    expect(dispose).toHaveBeenCalledTimes(1);
  });

  it('skips the rounds that fall while the tab is hidden', () => {
    overlay().start();
    vi.spyOn(document, 'hidden', 'get').mockReturnValue(true);

    vi.advanceTimersByTime(FIREWORK_ROUND_MS * 3);

    expect(launch).toHaveBeenCalledTimes(1);
  });

  it('survives a browser that cannot draw them', () => {
    create.mockImplementation(() => {
      throw new Error('no canvas');
    });
    const component = overlay();
    const before = vi.getTimerCount();

    expect(() => component.start()).not.toThrow();
    expect(vi.getTimerCount()).toBe(before);
  });

  it('stops with the component', () => {
    const fixture = TestBed.createComponent(FireworksOverlay);
    const before = vi.getTimerCount();
    fixture.componentInstance.start();

    fixture.destroy();

    expect(dispose).toHaveBeenCalledTimes(1);
    expect(vi.getTimerCount()).toBe(before);
  });

  describe('red panda', () => {
    let queue: number[] = [];
    let added: HTMLElement[] = [];
    /** Starts a show whose random numbers are these, in the order they are asked for (then zeros). */
    function started(...randoms: number[]) {
      queue = [...randoms];
      const fixture = TestBed.createComponent(FireworksOverlay);
      fixture.componentInstance.start();
      return fixture;
    }
    /** Starts a show and lets the panda arrive. */
    function arrived(...randoms: number[]) {
      const fixture = started(0, ...randoms);
      vi.advanceTimersByTime(0);
      fixture.detectChanges();
      return fixture;
    }
    const clip = (fixture: { nativeElement: HTMLElement }) =>
      fixture.nativeElement.querySelector<HTMLElement>('.panda-clip');
    const panda = (fixture: { nativeElement: HTMLElement }) =>
      fixture.nativeElement.querySelector<HTMLAnchorElement>('a.panda');
    const angle = (fixture: { nativeElement: HTMLElement }) => parseFloat(panda(fixture)!.style.getPropertyValue('--angle'));
    /** A panel in the room whose top edge is at this height and that spans these columns. */
    function panelAt(top: number, left: number, right: number): void {
      const element = document.createElement('div');
      element.className = 'strip';
      element.getBoundingClientRect = () => ({ top, left, right, bottom: top + 100, width: right - left, height: 100 }) as DOMRect;
      document.body.append(element);
      added.push(element);
    }
    const hidden = PANDA_SIZE * (1 - PANDA_VISIBLE_SHARE);

    beforeEach(() => {
      TestBed.overrideProvider(PANDA_RANDOM, { useValue: () => queue.shift() ?? 0 });
      vi.spyOn(window, 'innerWidth', 'get').mockReturnValue(1000);
      vi.spyOn(window, 'innerHeight', 'get').mockReturnValue(600);
    });

    afterEach(() => {
      added.forEach((element) => element.remove());
      added = [];
    });

    it('is not there when the show starts, then arrives at a random moment within the first seconds', () => {
      const fixture = started(0.5);
      fixture.detectChanges();
      expect(panda(fixture)).toBeNull();

      vi.advanceTimersByTime(PANDA_APPEAR_WITHIN_MS * 0.5 - 1);
      fixture.detectChanges();
      expect(panda(fixture)).toBeNull();
      vi.advanceTimersByTime(1);
      fixture.detectChanges();

      expect(panda(fixture)).not.toBeNull();
    });

    it('links to a search about why fireworks hurt red pandas, in a new tab, and is labelled', () => {
      const link = panda(arrived())!;

      expect(link.getAttribute('href')).toBe(PANDA_LINK);
      expect(PANDA_LINK).toContain('google.com/search');
      expect(link.target).toBe('_blank');
      expect(link.rel).toBe('noopener noreferrer');
      expect(link.getAttribute('aria-label')).toBe(PANDA_LABEL);
    });

    it('peeks up from behind the bottom of the window, cut off there, at a random spot and tilt', () => {
      const left = arrived(0, 0, 0);
      expect(clip(left)!.style.left).toBe(`${PANDA_MARGIN}px`);
      expect(clip(left)!.style.top).toBe(`${600 - PANDA_SIZE + hidden}px`);
      expect(clip(left)!.style.clipPath).toContain(`${hidden}px`);
      expect(angle(left)).toBeCloseTo(-PANDA_MAX_TILT_DEG);

      const right = arrived(0, 1, 1);
      expect(clip(right)!.style.left).toBe(`${1000 - PANDA_SIZE - PANDA_MARGIN}px`);
      expect(angle(right)).toBeCloseTo(PANDA_MAX_TILT_DEG);
    });

    it('peeks in from the left side of the window, leaning its head away from the edge', () => {
      const fixture = arrived(0.4, 0, 1);

      expect(clip(fixture)!.style.left).toBe(`${-hidden}px`);
      expect(clip(fixture)!.style.top).toBe(`${PANDA_MARGIN}px`);
      expect(angle(fixture)).toBeCloseTo(PANDA_MAX_SIDE_TILT_DEG);
    });

    it('peeks in from the right side of the window, leaning the other way', () => {
      const fixture = arrived(0.9, 1, 1);

      expect(clip(fixture)!.style.left).toBe(`${1000 - PANDA_SIZE + hidden}px`);
      expect(clip(fixture)!.style.top).toBe(`${600 - PANDA_SIZE - PANDA_MARGIN}px`);
      expect(angle(fixture)).toBeCloseTo(-PANDA_MAX_SIDE_TILT_DEG);
    });

    it('never tilts so far that its head is not above the rest of it', () => {
      for (const pick of [0, 0.4, 0.9]) {
        for (const tilt of [0, 0.5, 1]) {
          const fixture = arrived(pick, 0.5, tilt);
          expect(Math.abs(angle(fixture))).toBeLessThan(90);
          fixture.destroy();
        }
      }
    });

    it('can peek over the top of a panel in the room, cut off along that edge', () => {
      panelAt(300, 100, 600);
      // Four edges now: the panel's top is the one after the bottom, left and right of the window.
      const fixture = arrived(0.99, 0.5, 0);

      expect(clip(fixture)!.style.top).toBe(`${300 - PANDA_SIZE + hidden}px`);
      expect(clip(fixture)!.style.left).toBe(`${100 + (500 - PANDA_SIZE) * 0.5}px`);
      expect(clip(fixture)!.style.clipPath).toContain(`${hidden}px`);
    });

    it('skips a panel with no room above it, or too narrow for it', () => {
      panelAt(20, 100, 600);
      panelAt(300, 100, 100 + PANDA_SIZE - 1);
      // Only the window's three edges are left, so the last pick is its right side, not either panel.
      const fixture = arrived(0.99, 0, 0);

      expect(clip(fixture)!.style.left).toBe(`${1000 - PANDA_SIZE + hidden}px`);
    });

    it('shrinks to fit a window narrower than itself', () => {
      vi.spyOn(window, 'innerWidth', 'get').mockReturnValue(60);
      const fixture = arrived(0, 1, 0);

      expect(clip(fixture)!.style.width).toBe(`${60 - 2 * PANDA_MARGIN}px`);
    });

    it('slides back behind its edge and leaves after a few seconds', () => {
      const fixture = arrived();
      expect(panda(fixture)!.classList).not.toContain('leaving');

      vi.advanceTimersByTime(PANDA_SLIDE_MS + PANDA_VISIBLE_MS);
      fixture.detectChanges();
      expect(panda(fixture)!.classList).toContain('leaving');

      vi.advanceTimersByTime(PANDA_SLIDE_MS);
      fixture.detectChanges();
      expect(panda(fixture)).toBeNull();
    });

    it('goes when the fireworks stop, and then never arrives', () => {
      const fixture = arrived();

      fixture.componentInstance.stop();
      fixture.detectChanges();
      expect(panda(fixture)).toBeNull();

      const early = started(1);
      early.componentInstance.stop();
      vi.advanceTimersByTime(PANDA_APPEAR_WITHIN_MS);
      early.detectChanges();
      expect(panda(early)).toBeNull();
    });

    it('is replaced, not doubled, when the fireworks restart', () => {
      const fixture = arrived();

      fixture.componentInstance.start();
      fixture.detectChanges();
      expect(panda(fixture)).toBeNull();
      vi.advanceTimersByTime(0);
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelectorAll('a.panda')).toHaveLength(1);
    });

    it('goes when clicked', () => {
      const fixture = arrived();
      const link = panda(fixture)!;
      link.addEventListener('click', (event) => event.preventDefault());

      link.click();
      fixture.detectChanges();

      expect(panda(fixture)).toBeNull();
    });
  });
});
