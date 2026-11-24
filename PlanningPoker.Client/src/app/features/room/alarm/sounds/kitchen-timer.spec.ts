import { MAX_SYNTH_SECONDS } from '../alarm-synth';
import { fakeAudioContext } from './fake-audio-context.testing';
import { KITCHEN_TIMER_SYNTH } from './kitchen-timer';

type FakeNodes = ReturnType<typeof fakeAudioContext>;

function connectedToDestination(fake: FakeNodes): boolean {
  const creators = ['createOscillator', 'createGain', 'createBiquadFilter', 'createBufferSource'];
  const context = fake.context as unknown as Record<
    string,
    { mock: { results: { value: { connect: { mock: { calls: unknown[][] } } } }[] } }
  >;
  return creators.some((name) =>
    context[name].mock.results.some((result) =>
      result.value.connect.mock.calls.some((call) => call[0] === fake.destination),
    ),
  );
}

describe('KITCHEN_TIMER_SYNTH', () => {
  it('returns a length within the Alarm limit', () => {
    const fake = fakeAudioContext();

    const length = KITCHEN_TIMER_SYNTH(fake.context, fake.destination, 0);

    expect(length).toBeGreaterThan(0);
    expect(length).toBeLessThanOrEqual(MAX_SYNTH_SECONDS);
  });

  it('starts and stops every source inside the reported length', () => {
    const fake = fakeAudioContext();
    const startAt = 3;

    const length = KITCHEN_TIMER_SYNTH(fake.context, fake.destination, startAt);

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

    KITCHEN_TIMER_SYNTH(fake.context, fake.destination, 0);

    expect(connectedToDestination(fake)).toBe(true);
  });

  it('schedules a later start time later', () => {
    const early = fakeAudioContext();
    const late = fakeAudioContext();

    KITCHEN_TIMER_SYNTH(early.context, early.destination, 1);
    KITCHEN_TIMER_SYNTH(late.context, late.destination, 5);

    const earliestStart = (fake: FakeNodes) => Math.min(...fake.sources().map((source) => source.startedAt ?? Number.NaN));
    expect(earliestStart(late)).toBeCloseTo(earliestStart(early) + 4);
  });
});
