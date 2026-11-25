import { Injectable, InjectionToken, inject } from '@angular/core';
import { AlarmSoundId, alarmSoundUrl } from './alarm-sounds';

/** Makes the audio element that plays one sound file; replaced in specs, since a test has no sound card. */
export const AUDIO_ELEMENT_FACTORY = new InjectionToken<(src: string) => HTMLAudioElement>('AUDIO_ELEMENT_FACTORY', {
  providedIn: 'root',
  factory: () => (src: string) => new Audio(src),
});

/** Plays an Alarm sound. A browser that blocks or lacks audio is not an error: the jingle on the button still shows. */
@Injectable({ providedIn: 'root' })
export class AlarmPlayer {
  private readonly createAudio = inject(AUDIO_ELEMENT_FACTORY);

  /** Plays the sound's file once; Silent plays nothing. Never rejects. */
  async play(sound: AlarmSoundId): Promise<void> {
    if (sound === 'silent') {
      return;
    }
    try {
      await this.createAudio(alarmSoundUrl(sound)).play();
    } catch {
      // Blocked until the page is interacted with, or no audio at all: stay quiet.
    }
  }
}
