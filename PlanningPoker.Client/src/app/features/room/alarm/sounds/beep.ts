import type { AlarmSynth } from '../alarm-synth';

const TONE_HZ = 1000;
const BEEP_SECONDS = 0.08;
/** Each beep fades in and out over this long, so it starts and ends without a click. */
const EDGE_SECONDS = 0.003;
const PEAK_GAIN = 0.12;
/** Beep start offsets from `startAt`: two pairs of two beeps, each beep 80 ms on and 80 ms off. */
const BEEP_OFFSETS = [0, 0.16, 0.56, 0.72];
const LENGTH_SECONDS = 0.9;

/** A digital alarm-clock beep: four short square-wave beeps in two pairs. */
export const BEEP_SYNTH: AlarmSynth = (context, destination, startAt) => {
  const envelope = context.createGain();
  envelope.gain.setValueAtTime(0, startAt);
  for (const offset of BEEP_OFFSETS) {
    const on = startAt + offset;
    const off = on + BEEP_SECONDS;
    envelope.gain.linearRampToValueAtTime(PEAK_GAIN, on + EDGE_SECONDS);
    envelope.gain.setValueAtTime(PEAK_GAIN, off - EDGE_SECONDS);
    envelope.gain.linearRampToValueAtTime(0, off);
  }

  const tone = context.createOscillator();
  tone.type = 'square';
  tone.frequency.setValueAtTime(TONE_HZ, startAt);
  tone.connect(envelope);
  tone.start(startAt);
  tone.stop(startAt + LENGTH_SECONDS);

  envelope.connect(destination);
  return LENGTH_SECONDS;
};
