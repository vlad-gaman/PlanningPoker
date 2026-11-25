import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AlarmState } from '../../../models/room';
import { AlarmButton } from './alarm-button';

const NOW = Date.UTC(2026, 0, 1, 12, 0, 0);

@Component({
  imports: [AlarmButton],
  template: `
    <app-alarm-button
      [alarm]="alarm()"
      [enabled]="enabled()"
      [minutes]="5"
      [icon]="'🔔'"
      [jingle]="jingle()"
      (started)="events.push('start')"
      (cancelled)="events.push('cancel')"
      (dismissed)="events.push('reset')"
    />
  `,
})
class Host {
  readonly alarm = signal<AlarmState>({ status: 'idle', endsAt: null });
  readonly enabled = signal(true);
  readonly jingle = signal(false);
  readonly events: string[] = [];
}

describe('AlarmButton', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(NOW);
  });

  afterEach(() => vi.useRealTimers());

  function mount() {
    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    const button = () => fixture.nativeElement.querySelector('button') as HTMLButtonElement | null;
    return { fixture, host: fixture.componentInstance, button };
  }

  it('is not shown when the room has turned the Alarm off', () => {
    const { fixture, host, button } = mount();

    host.enabled.set(false);
    fixture.detectChanges();

    expect(button()).toBeNull();
  });

  it('offers to start the Alarm while idle, showing its length and the icon of the chosen sound', () => {
    const { host, button } = mount();

    expect(button()!.textContent).toContain('🔔');
    expect(button()!.textContent).toContain('5:00');
    expect(button()!.getAttribute('aria-label')).toBe('Start the 5 minute alarm');

    button()!.click();

    expect(host.events).toEqual(['start']);
  });

  it('counts down while running, and a click cancels it', () => {
    const { fixture, host, button } = mount();
    host.alarm.set({ status: 'running', endsAt: NOW + 125_000 });
    fixture.detectChanges();

    expect(button()!.textContent).toContain('2:05');
    expect(button()!.getAttribute('aria-label')).toBe('Cancel the alarm, 2 minutes 5 seconds left');

    vi.advanceTimersByTime(5000);
    fixture.detectChanges();
    expect(button()!.textContent).toContain('2:00');

    button()!.click();
    expect(host.events).toEqual(['cancel']);
  });

  it('starts at the full length, not a second above it, when the last tick was a moment ago', () => {
    const { fixture, host, button } = mount();
    vi.advanceTimersByTime(900);

    host.alarm.set({ status: 'running', endsAt: Date.now() + 300_000 });
    fixture.detectChanges();

    expect(button()!.textContent).toContain('5:00');
    expect(button()!.textContent).not.toContain('5:01');
  });

  it('drops to the next second one second after it starts, however long ago the page loaded', () => {
    const { fixture, host, button } = mount();
    vi.advanceTimersByTime(100);

    host.alarm.set({ status: 'running', endsAt: Date.now() + 300_000 });
    fixture.detectChanges();
    vi.advanceTimersByTime(1000);
    fixture.detectChanges();

    expect(button()!.textContent).toContain('4:59');
  });

  it('never counts below zero while it waits for the room to say it elapsed', () => {
    const { fixture, host, button } = mount();
    host.alarm.set({ status: 'running', endsAt: NOW + 1000 });
    fixture.detectChanges();

    vi.advanceTimersByTime(4000);
    fixture.detectChanges();

    expect(button()!.textContent).toContain('0:00');
  });

  it("shows time's up once elapsed, and a click resets it", () => {
    const { fixture, host, button } = mount();
    host.alarm.set({ status: 'elapsed', endsAt: NOW });
    fixture.detectChanges();

    expect(button()!.textContent).toContain("Time's up");
    expect(button()!.getAttribute('aria-label')).toBe('The alarm elapsed. Reset it');

    button()!.click();
    expect(host.events).toEqual(['reset']);
  });

  it('jingles only while it is told to, whatever state it is in', () => {
    const { fixture, host, button } = mount();
    host.alarm.set({ status: 'elapsed', endsAt: NOW });
    fixture.detectChanges();
    expect(button()!.classList.contains('jingle')).toBe(false);

    host.jingle.set(true);
    fixture.detectChanges();
    expect(button()!.classList.contains('jingle')).toBe(true);
  });
});
