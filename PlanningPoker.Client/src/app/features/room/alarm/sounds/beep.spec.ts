import { MAX_SYNTH_SECONDS } from '../alarm-synth';
import { BEEP_SYNTH } from './beep';
import { fakeAudioContext } from './fake-audio-context.testing';

/** Whether any node the synth created was connected to the destination. */
function reachesDestination(context: BaseAudioContext, destination: AudioNode): boolean {
  const factories = ['createOscillator', 'createGain', 'createBiquadFilter'] as const;
  return factories.some((name) => {
    const create = context[name] as unknown as { mock: { results: { value: { connect: { mock: { calls: unknown[][] } } } }[] } };
    return create.mock.results.some((result) => result.value.connect.mock.calls.some((call) => call[0] === destination));
  });
}

describe('BEEP_SYNTH', () => {
  it('returns a length within the alarm limit', () => {
    const { context, destination } = fakeAudioContext();
    const length = BEEP_SYNTH(context, destination, 1);
    expect(length).toBeGreaterThan(0);
    expect(length).toBeLessThanOrEqual(MAX_SYNTH_SECONDS);
  });

  it('starts and stops every source inside the reported length', () => {
    const { context, destination, sources } = fakeAudioContext();
    const startAt = 2;
    const length = BEEP_SYNTH(context, destination, startAt);
    const made = sources();
    expect(made.length).toBeGreaterThan(0);
    for (const source of made) {
      expect(source.startedAt).not.toBeNull();
      expect(source.startedAt).toBeGreaterThanOrEqual(startAt);
      expect(source.stoppedAt).not.toBeNull();
      expect(source.stoppedAt).toBeGreaterThan(source.startedAt as number);
      expect(source.stoppedAt).toBeLessThanOrEqual(startAt + length);
    }
  });

  it('connects its output to the destination', () => {
    const { context, destination } = fakeAudioContext();
    BEEP_SYNTH(context, destination, 0);
    expect(reachesDestination(context, destination)).toBe(true);
  });

  it('schedules a later start time later', () => {
    const early = fakeAudioContext();
    const late = fakeAudioContext();
    BEEP_SYNTH(early.context, early.destination, 1);
    BEEP_SYNTH(late.context, late.destination, 5);
    const firstStart = (sources: ReturnType<typeof fakeAudioContext>['sources']) =>
      Math.min(...sources().map((source) => source.startedAt as number));
    expect(firstStart(late.sources)).toBeGreaterThan(firstStart(early.sources));
  });
});
