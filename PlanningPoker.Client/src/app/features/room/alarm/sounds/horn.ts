import type { AlarmSynth } from '../alarm-synth';

const ATTACK_SECONDS = 0.05;
const HOLD_SECONDS = 0.9;
const RELEASE_SECONDS = 0.15;
/** The whole sound: attack, hold and release, with a little slack for the release tail. */
const LENGTH_SECONDS = 1.2;
const PEAK_GAIN = 0.35;
const LOW_PASS_START_HZ = 500;
const LOW_PASS_OPEN_HZ = 2200;

/**
 * A brassy air horn: two detuned sawtooth voices through a low-pass filter that opens on the attack, held for about a
 * second, then released.
 */
export const HORN_SYNTH: AlarmSynth = (context, destination, startAt) => {
  const attackEnd = startAt + ATTACK_SECONDS;
  const holdEnd = attackEnd + HOLD_SECONDS;
  const end = holdEnd + RELEASE_SECONDS;

  const filter = context.createBiquadFilter();
  filter.type = 'lowpass';
  filter.Q.value = 2;
  filter.frequency.setValueAtTime(LOW_PASS_START_HZ, startAt);
  filter.frequency.exponentialRampToValueAtTime(LOW_PASS_OPEN_HZ, attackEnd);

  const envelope = context.createGain();
  envelope.gain.setValueAtTime(0, startAt);
  envelope.gain.linearRampToValueAtTime(PEAK_GAIN, attackEnd);
  envelope.gain.setValueAtTime(PEAK_GAIN, holdEnd);
  envelope.gain.linearRampToValueAtTime(0, end);

  for (const frequency of [220, 277]) {
    const voice = context.createOscillator();
    voice.type = 'sawtooth';
    voice.frequency.setValueAtTime(frequency, startAt);
    voice.connect(filter);
    voice.start(startAt);
    voice.stop(end);
  }

  filter.connect(envelope);
  envelope.connect(destination);
  return LENGTH_SECONDS;
};
