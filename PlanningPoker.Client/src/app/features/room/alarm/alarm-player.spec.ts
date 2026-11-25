import { TestBed } from '@angular/core/testing';
import { ALARM_SOUNDS, AlarmSoundId, alarmSoundUrl } from './alarm-sounds';
import { AUDIO_ELEMENT_FACTORY, AlarmPlayer } from './alarm-player';

describe('alarmSoundUrl', () => {
  it('points every sound at its own audio file under sounds/alarm, relative to the base href', () => {
    const noisy = ALARM_SOUNDS.map((sound) => sound.id).filter((id): id is Exclude<AlarmSoundId, 'silent'> => id !== 'silent');

    expect(noisy.map(alarmSoundUrl)).toEqual(noisy.map((id) => `sounds/alarm/${id}.wav`));
  });
});

describe('AlarmPlayer', () => {
  function setup(factory?: (src: string) => HTMLAudioElement) {
    const audio = { play: vi.fn().mockResolvedValue(undefined) };
    const create = vi.fn(factory ?? (() => audio as unknown as HTMLAudioElement));
    TestBed.configureTestingModule({ providers: [{ provide: AUDIO_ELEMENT_FACTORY, useValue: create }] });
    return { audio, create, player: TestBed.inject(AlarmPlayer) };
  }

  it("plays the chosen sound's file", async () => {
    const { audio, create, player } = setup();

    await player.play('rooster');

    expect(create).toHaveBeenCalledExactlyOnceWith('sounds/alarm/rooster.wav');
    expect(audio.play).toHaveBeenCalledOnce();
  });

  it('makes no sound, and loads no file, for Silent', async () => {
    const { create, player } = setup();

    await player.play('silent');

    expect(create).not.toHaveBeenCalled();
  });

  it('fails silently when the browser blocks playing before the page was interacted with', async () => {
    const { audio, player } = setup();
    audio.play.mockRejectedValue(new DOMException('blocked', 'NotAllowedError'));

    await expect(player.play('duck')).resolves.toBeUndefined();
  });

  it('fails silently when the browser has no audio at all', async () => {
    const { player } = setup(() => {
      throw new Error('no audio');
    });

    await expect(player.play('gong')).resolves.toBeUndefined();
  });
});
