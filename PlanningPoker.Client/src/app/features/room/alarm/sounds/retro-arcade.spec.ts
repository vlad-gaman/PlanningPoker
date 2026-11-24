import type { Mock } from 'vitest';
import { MAX_SYNTH_SECONDS } from '../alarm-synth';
import { fakeAudioContext } from './fake-audio-context.testing';
import { RETRO_ARCADE_SYNTH } from './retro-arcade';

/** True when some node the synth made was connected to the fake's destination. */
function connectsToDestination(fake: ReturnType<typeof fakeAudioContext>): boolean {
  const factories = Object.values(fake.context as unknown as Record<string, unknown>).filter(
    (value): value is Mock => vi.isMockFunction(value),
  );
  return factories.some((factory) =>
    factory.mock.results.some((result) => {
      const node = result.value as { connect?: Mock };
      return node.connect?.mock.calls.some((call) => call[0] === fake.destination) ?? false;
    }),
  );
}

describe('RETRO_ARCADE_SYNTH', () => {
  it('returns a length within the alarm limit', () => {
    const fake = fakeAudioContext();

    const length = RETRO_ARCADE_SYNTH(fake.context, fake.destination, 0);

    expect(length).toBeGreaterThan(0);
    expect(length).toBeLessThanOrEqual(MAX_SYNTH_SECONDS);
  });

  it('starts and stops every source within the returned length', () => {
    const startAt = 2;
    const fake = fakeAudioContext();

    const length = RETRO_ARCADE_SYNTH(fake.context, fake.destination, startAt);

    const sources = fake.sources();
    expect(sources.length).toBeGreaterThan(0);
    for (const source of sources) {
      const start = source.startedAt ?? Number.NaN;
      const stop = source.stoppedAt ?? Number.NaN;
      expect(start).toBeGreaterThanOrEqual(startAt);
      expect(stop).toBeGreaterThan(start);
      expect(stop).toBeLessThanOrEqual(startAt + length);
    }
  });

  it('connects its output to the destination', () => {
    const fake = fakeAudioContext();

    RETRO_ARCADE_SYNTH(fake.context, fake.destination, 0);

    expect(connectsToDestination(fake)).toBe(true);
  });

  it('schedules a later start time when given a later startAt', () => {
    const earliestStart = (startAt: number) => {
      const fake = fakeAudioContext();
      RETRO_ARCADE_SYNTH(fake.context, fake.destination, startAt);
      return Math.min(...fake.sources().map((source) => source.startedAt ?? Number.NaN));
    };

    expect(earliestStart(5)).toBeGreaterThan(earliestStart(1));
  });
});
