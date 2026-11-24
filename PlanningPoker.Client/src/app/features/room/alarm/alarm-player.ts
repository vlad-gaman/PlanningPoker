import { Injectable, InjectionToken, inject } from '@angular/core';
import { AlarmSoundId } from './alarm-sounds';
import { ALARM_SYNTHS } from './alarm-synths';

/** Opens the browser's audio context; replaced in specs, since a test has no sound card. */
export const AUDIO_CONTEXT_FACTORY = new InjectionToken<() => BaseAudioContext>('AUDIO_CONTEXT_FACTORY', {
  providedIn: 'root',
  factory: () => () => new AudioContext(),
});

/** How long after a sound's own length the audio context is kept, so its tail is never cut. */
const RELEASE_MARGIN_MILLISECONDS = 500;

/** Plays an Alarm sound. A browser that blocks or lacks audio is not an error: the jingle on the button still shows. */
@Injectable({ providedIn: 'root' })
export class AlarmPlayer {
  private readonly createContext = inject(AUDIO_CONTEXT_FACTORY);

  /** Plays the sound once; Silent plays nothing. Never rejects. */
  async play(sound: AlarmSoundId): Promise<void> {
    if (sound === 'silent') {
      return;
    }
    try {
      const context = this.createContext();
      if (context.state === 'suspended') {
        await (context as AudioContext).resume();
      }
      const seconds = ALARM_SYNTHS[sound](context, context.destination, context.currentTime);
      setTimeout(() => void (context as AudioContext).close().catch(() => undefined), seconds * 1000 + RELEASE_MARGIN_MILLISECONDS);
    } catch {
      // Blocked until the page is interacted with, or no audio at all: stay quiet.
    }
  }
}
