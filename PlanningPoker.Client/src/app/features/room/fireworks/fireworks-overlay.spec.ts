import { TestBed } from '@angular/core/testing';
import { FIREWORK_ROUNDS, FIREWORK_ROUND_MS, FIREWORKS_PER_ROUND, FireworksOverlay } from './fireworks-overlay';
import { FireworksLauncher } from './fireworks-launcher';

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

  it('does not start in a hidden tab', () => {
    vi.spyOn(document, 'hidden', 'get').mockReturnValue(true);

    overlay().start();

    expect(create).not.toHaveBeenCalled();
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
});
