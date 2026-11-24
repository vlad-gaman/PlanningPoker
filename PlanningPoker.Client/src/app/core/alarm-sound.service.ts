import { Injectable, computed, signal } from '@angular/core';
import { ALARM_SOUNDS, AlarmSoundId, DEFAULT_ALARM_SOUND, isAlarmSoundId } from '../features/room/alarm/alarm-sounds';

export const ALARM_SOUND_STORAGE_KEY = 'pp-alarm-sound';

/** The sound this person hears when a room Alarm elapses, per browser. Nobody else sees the choice. */
@Injectable({ providedIn: 'root' })
export class AlarmSoundService {
  readonly sound = signal<AlarmSoundId>(this.readStored());
  /** The icon of the chosen sound, shown on the Alarm button. */
  readonly icon = computed(() => ALARM_SOUNDS.find((sound) => sound.id === this.sound())?.icon ?? '');

  set(sound: AlarmSoundId): void {
    this.sound.set(sound);
    try {
      localStorage.setItem(ALARM_SOUND_STORAGE_KEY, sound);
    } catch {
      // Storage can be unavailable (private mode); the sound then lasts for the session only.
    }
  }

  private readStored(): AlarmSoundId {
    try {
      const stored = localStorage.getItem(ALARM_SOUND_STORAGE_KEY);
      return isAlarmSoundId(stored) ? stored : DEFAULT_ALARM_SOUND;
    } catch {
      return DEFAULT_ALARM_SOUND;
    }
  }
}
