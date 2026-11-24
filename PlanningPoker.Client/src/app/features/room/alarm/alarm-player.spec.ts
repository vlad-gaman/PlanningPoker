import { TestBed } from '@angular/core/testing';
import { ALARM_SOUNDS } from './alarm-sounds';
import { ALARM_SYNTHS } from './alarm-synths';
import { AUDIO_CONTEXT_FACTORY, AlarmPlayer } from './alarm-player';
import { MAX_SYNTH_SECONDS } from './alarm-synth';
import { fakeAudioContext } from './sounds/fake-audio-context.testing';

describe('ALARM_SYNTHS', () => {
  it('has a synthesizer for every sound but Silent, and only those', () => {
    const noisy = ALARM_SOUNDS.map((sound) => sound.id).filter((id) => id !== 'silent');

    expect(Object.keys(ALARM_SYNTHS).sort()).toEqual([...noisy].sort());
  });

  it.each(Object.entries(ALARM_SYNTHS))('%s fits the length limit', (_, synth) => {
    const { context, destination } = fakeAudioContext();

    expect(synth(context, destination, 0)).toBeLessThanOrEqual(MAX_SYNTH_SECONDS);
  });
});

describe('AlarmPlayer', () => {
  afterEach(() => vi.useRealTimers());

  function setup(factory?: () => BaseAudioContext) {
    const audio = fakeAudioContext();
    const create = vi.fn(factory ?? (() => audio.context));
    TestBed.configureTestingModule({ providers: [{ provide: AUDIO_CONTEXT_FACTORY, useValue: create }] });
    return { audio, create, player: TestBed.inject(AlarmPlayer) };
  }

  it('plays the chosen sound on a fresh audio context', async () => {
    const { audio, create, player } = setup();

    await player.play('bell');

    expect(create).toHaveBeenCalledOnce();
    expect(audio.sources().length).toBeGreaterThan(0);
    expect(audio.sources().every((source) => source.startedAt !== null)).toBe(true);
  });

  it('makes no sound, and opens no audio context, for Silent', async () => {
    const { create, player } = setup();

    await player.play('silent');

    expect(create).not.toHaveBeenCalled();
  });

  it('resumes a context the browser left suspended', async () => {
    const { audio, player } = setup();
    (audio.context as unknown as { state: string }).state = 'suspended';

    await player.play('chime');

    expect((audio.context as unknown as { resume: () => void }).resume).toHaveBeenCalled();
  });

  it('releases the audio context once the sound is over', async () => {
    vi.useFakeTimers();
    const { audio, player } = setup();

    await player.play('beep');
    expect((audio.context as unknown as { close: () => void }).close).not.toHaveBeenCalled();
    vi.advanceTimersByTime((MAX_SYNTH_SECONDS + 1) * 1000);

    expect((audio.context as unknown as { close: () => void }).close).toHaveBeenCalled();
  });

  it('fails silently when the browser has no audio or blocks it', async () => {
    const { player } = setup(() => {
      throw new Error('blocked');
    });

    await expect(player.play('gong')).resolves.toBeUndefined();
  });

  it('fails silently when resuming is refused', async () => {
    const { audio, player } = setup();
    (audio.context as unknown as { state: string }).state = 'suspended';
    (audio.context as unknown as { resume: () => Promise<void> }).resume = vi.fn().mockRejectedValue(new Error('no'));

    await expect(player.play('gong')).resolves.toBeUndefined();
  });
});
