/** The sounds a person can pick for the room Alarm, each with the icon that stands for it. Silent plays nothing. */
export const ALARM_SOUNDS = [
  { id: 'bell', label: 'Bell', icon: '🔔' },
  { id: 'chime', label: 'Chime', icon: '🎐' },
  { id: 'gong', label: 'Gong', icon: '🥁' },
  { id: 'horn', label: 'Horn', icon: '📯' },
  { id: 'beep', label: 'Beep', icon: '⏰' },
  { id: 'duck', label: 'Duck', icon: '🦆' },
  { id: 'sci-fi', label: 'Sci-fi', icon: '🛸' },
  { id: 'metroidvania', label: 'Metroidvania', icon: '🗝️' },
  { id: 'retro-arcade', label: 'Retro arcade', icon: '🕹️' },
  { id: 'kitchen-timer', label: 'Kitchen timer', icon: '🍳' },
  { id: 'rooster', label: 'Rooster', icon: '🐓' },
  { id: 'silent', label: 'Silent', icon: '🔕' },
] as const;

export type AlarmSoundId = (typeof ALARM_SOUNDS)[number]['id'];

export const DEFAULT_ALARM_SOUND: AlarmSoundId = 'bell';

export function isAlarmSoundId(value: unknown): value is AlarmSoundId {
  return ALARM_SOUNDS.some((sound) => sound.id === value);
}
