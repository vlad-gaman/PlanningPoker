import { MAX_SYNTH_SECONDS } from '../alarm-synth';
import { fakeAudioContext } from './fake-audio-context.testing';
import { BELL_SYNTH } from './bell';

describe('BELL_SYNTH', () => {
  it('returns a length within the Alarm sound limit', () => {
    const { context, destination } = fakeAudioContext();

    const length = BELL_SYNTH(context, destination, 0);

    expect(length).toBeGreaterThan(0);
    expect(length).toBeLessThanOrEqual(MAX_SYNTH_SECONDS);
  });

  it('starts and stops every source inside the returned length', () => {
    const { context, destination, sources } = fakeAudioContext();
    const startAt = 2;

    const length = BELL_SYNTH(context, destination, startAt);

    expect(sources().length).toBeGreaterThan(0);
    for (const source of sources()) {
      expect(source.startedAt).toBeGreaterThanOrEqual(startAt);
      expect(source.stoppedAt).toBeGreaterThan(source.startedAt ?? Infinity);
      expect(source.stoppedAt).toBeLessThanOrEqual(startAt + length);
    }
  });

  it('connects its output to the destination', () => {
    const { context, destination } = fakeAudioContext();

    BELL_SYNTH(context, destination, 0);

    const gains = vi.mocked(context.createGain).mock.results.map((result) => result.value as AudioNode);
    expect(gains.some((gain) => vi.mocked(gain.connect).mock.calls.some((call) => (call as unknown[])[0] === destination))).toBe(true);
  });

  it('schedules a second sound started later when given a later startAt', () => {
    const first = fakeAudioContext();
    const second = fakeAudioContext();

    BELL_SYNTH(first.context, first.destination, 0);
    BELL_SYNTH(second.context, second.destination, 5);

    const firstStart = Math.min(...first.sources().map((source) => source.startedAt ?? Infinity));
    const secondStart = Math.min(...second.sources().map((source) => source.startedAt ?? Infinity));
    expect(secondStart).toBeGreaterThan(firstStart);
  });
});
