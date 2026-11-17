const SECONDS_PER_MINUTE = 60;
const SECONDS_PER_HOUR = 3600;

const pad = (value: number): string => String(value).padStart(2, '0');

/** `m:ss`, or `h:mm:ss` from an hour on. Negative or fractional input is clamped to whole seconds from 0. */
export function formatRoundTime(totalSeconds: number): string {
  const total = Math.max(0, Math.floor(totalSeconds));
  const hours = Math.floor(total / SECONDS_PER_HOUR);
  const minutes = Math.floor((total % SECONDS_PER_HOUR) / SECONDS_PER_MINUTE);
  const seconds = total % SECONDS_PER_MINUTE;
  return hours > 0 ? `${hours}:${pad(minutes)}:${pad(seconds)}` : `${minutes}:${pad(seconds)}`;
}

const unit = (value: number, name: string): string => `${value} ${name}${value === 1 ? '' : 's'}`;

/** The same duration in words for a screen reader, e.g. "1 minute 5 seconds". */
export function spokenRoundTime(totalSeconds: number): string {
  const total = Math.max(0, Math.floor(totalSeconds));
  const hours = Math.floor(total / SECONDS_PER_HOUR);
  const minutes = Math.floor((total % SECONDS_PER_HOUR) / SECONDS_PER_MINUTE);
  const seconds = total % SECONDS_PER_MINUTE;
  const parts = [hours > 0 ? unit(hours, 'hour') : '', hours > 0 || minutes > 0 ? unit(minutes, 'minute') : '', unit(seconds, 'second')];
  return parts.filter(Boolean).join(' ');
}
