import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { RoundTimer } from './round-timer';

@Component({
  imports: [RoundTimer],
  template: '<app-round-timer [startedAt]="startedAt()" />',
})
class Host {
  readonly startedAt = signal(0);
}

describe('RoundTimer', () => {
  const START = Date.UTC(2026, 0, 1, 12, 0, 0);

  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(START + 42_000);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  function mount(startedAt = START) {
    const fixture = TestBed.createComponent(Host);
    fixture.componentInstance.startedAt.set(startedAt);
    fixture.detectChanges();
    return fixture;
  }

  const timer = (fixture: ReturnType<typeof mount>): HTMLElement => fixture.nativeElement.querySelector('[role="timer"]');

  it('shows the time since the start, with a spoken label', () => {
    const element = timer(mount());

    expect(element.textContent?.trim()).toBe('Round time 0:42');
    expect(element.getAttribute('aria-label')).toBe('Round time 42 seconds');
  });

  it('ticks each second', () => {
    const fixture = mount();

    vi.advanceTimersByTime(1000);
    fixture.detectChanges();

    expect(timer(fixture).textContent?.trim()).toBe('Round time 0:43');
  });

  it('switches to h:mm:ss past an hour', () => {
    expect(timer(mount(START + 42_000 - 3_623_000)).textContent?.trim()).toBe('Round time 1:00:23');
  });

  it('never goes below zero when the clock is behind the server', () => {
    expect(timer(mount(START + 60_000)).textContent?.trim()).toBe('Round time 0:00');
  });

  it('is not a live region, so the ticks are not announced', () => {
    const element = timer(mount());

    expect(element.getAttribute('aria-live')).toBeNull();
    expect(element.getAttribute('role')).toBe('timer');
  });

  it('stops ticking when destroyed', () => {
    const fixture = mount();

    fixture.destroy();

    expect(vi.getTimerCount()).toBe(0);
  });
});
