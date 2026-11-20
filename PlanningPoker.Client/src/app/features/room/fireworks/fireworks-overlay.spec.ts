import { TestBed } from '@angular/core/testing';
import {
  FIREWORK_LINGER_MS,
  PANDA_APPEAR_WITHIN_MS,
  PANDA_LABEL,
  PANDA_LINK,
  PANDA_MARGIN,
  PANDA_SIZE,
  PANDA_VISIBLE_MS,
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
    /** Starts a show whose random numbers are these, in the order they are asked for (then zeros). */
    function started(...randoms: number[]) {
      queue = [...randoms];
      const fixture = TestBed.createComponent(FireworksOverlay);
      fixture.componentInstance.start();
      return fixture;
    }
    const panda = (fixture: { nativeElement: HTMLElement }) =>
      fixture.nativeElement.querySelector<HTMLAnchorElement>('a.panda');

    beforeEach(() => {
      TestBed.overrideProvider(PANDA_RANDOM, { useValue: () => queue.shift() ?? 0 });
      vi.spyOn(window, 'innerWidth', 'get').mockReturnValue(1000);
      vi.spyOn(window, 'innerHeight', 'get').mockReturnValue(600);
    });

    it('is not there when the show starts, then arrives at a random moment within the first seconds', () => {
      const fixture = started(0.5, 0, 0);
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
      const fixture = started(0, 0, 0);
      vi.advanceTimersByTime(0);
      fixture.detectChanges();

      const link = panda(fixture)!;
      expect(link.getAttribute('href')).toBe(PANDA_LINK);
      expect(PANDA_LINK).toContain('google.com/search');
      expect(link.target).toBe('_blank');
      expect(link.rel).toBe('noopener noreferrer');
      expect(link.getAttribute('aria-label')).toBe(PANDA_LABEL);
    });

    it('sits at the random position, inside the window with a margin', () => {
      const top = started(0, 0, 0);
      vi.advanceTimersByTime(0);
      top.detectChanges();
      expect(panda(top)!.style.left).toBe(`${PANDA_MARGIN}px`);
      expect(panda(top)!.style.top).toBe(`${PANDA_MARGIN}px`);

      const bottom = started(0, 1, 1);
      vi.advanceTimersByTime(0);
      bottom.detectChanges();
      expect(panda(bottom)!.style.left).toBe(`${1000 - PANDA_SIZE - PANDA_MARGIN}px`);
      expect(panda(bottom)!.style.top).toBe(`${600 - PANDA_SIZE - PANDA_MARGIN}px`);
      expect(panda(bottom)!.style.width).toBe(`${PANDA_SIZE}px`);
    });

    it('shrinks to fit a window narrower than itself', () => {
      vi.spyOn(window, 'innerWidth', 'get').mockReturnValue(60);
      const fixture = started(0, 1, 0);
      vi.advanceTimersByTime(0);
      fixture.detectChanges();

      expect(panda(fixture)!.style.width).toBe(`${60 - 2 * PANDA_MARGIN}px`);
      expect(panda(fixture)!.style.left).toBe(`${PANDA_MARGIN}px`);
    });

    it('leaves again after a few seconds', () => {
      const fixture = started(0, 0, 0);
      vi.advanceTimersByTime(0);
      fixture.detectChanges();
      expect(panda(fixture)).not.toBeNull();

      vi.advanceTimersByTime(PANDA_VISIBLE_MS);
      fixture.detectChanges();

      expect(panda(fixture)).toBeNull();
    });

    it('goes when the fireworks stop, and then never arrives', () => {
      const fixture = started(0, 0, 0);
      vi.advanceTimersByTime(0);
      fixture.detectChanges();

      fixture.componentInstance.stop();
      fixture.detectChanges();
      expect(panda(fixture)).toBeNull();

      const early = started(1, 0, 0);
      early.componentInstance.stop();
      vi.advanceTimersByTime(PANDA_APPEAR_WITHIN_MS);
      early.detectChanges();
      expect(panda(early)).toBeNull();
    });

    it('is replaced, not doubled, when the fireworks restart', () => {
      const fixture = started(0, 0, 0);
      vi.advanceTimersByTime(0);
      fixture.detectChanges();

      fixture.componentInstance.start();
      fixture.detectChanges();
      expect(panda(fixture)).toBeNull();
      vi.advanceTimersByTime(0);
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelectorAll('a.panda')).toHaveLength(1);
    });

    it('goes when clicked', () => {
      const fixture = started(0, 0, 0);
      vi.advanceTimersByTime(0);
      fixture.detectChanges();
      const link = panda(fixture)!;
      link.addEventListener('click', (event) => event.preventDefault());

      link.click();
      fixture.detectChanges();

      expect(panda(fixture)).toBeNull();
    });
  });
});
