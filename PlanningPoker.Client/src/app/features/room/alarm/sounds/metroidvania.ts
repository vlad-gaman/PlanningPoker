import type { AlarmSynth } from '../alarm-synth';

const LENGTH = 2;
const STEP = 0.11;
const NOTE_PEAK = 0.22;
const ECHO_DELAY = 0.3;
const ECHO_PEAK = 0.1;

/** D dorian, climbing: D4 F4 A4 C5, then the held D5. The melody is original, not taken from any game. */
const ARPEGGIO = [293.66, 349.23, 440, 523.25];
const HELD_NOTE = 587.33;

/** Fades a pulse note in, holds it, and fades it back to silence at `end`. */
function envelope(gain: GainNode, from: number, end: number, peak: number) {
  gain.gain.setValueAtTime(0, from);
  gain.gain.linearRampToValueAtTime(peak, from + 0.01);
  gain.gain.setValueAtTime(peak, Math.max(from + 0.01, end - 0.1));
  gain.gain.linearRampToValueAtTime(0, end);
}

/**
 * An "ability unlocked" fanfare: a square-wave (pulse-style) arpeggio climbing a dorian scale, ending on a held note
 * with a quieter delayed echo of its last notes.
 */
export const METROIDVANIA_SYNTH: AlarmSynth = (context, destination, startAt) => {
  const filter = context.createBiquadFilter();
  filter.type = 'lowpass';
  filter.frequency.value = 2400;
  filter.Q.value = 0.7;
  filter.connect(destination);

  const note = (freq: number, from: number, to: number, peak: number) => {
    const osc = context.createOscillator();
    osc.type = 'square';
    osc.frequency.value = freq;
    const env = context.createGain();
    envelope(env, from, to, peak);
    osc.connect(env);
    env.connect(filter);
    osc.start(from);
    osc.stop(to + 0.02);
  };

  ARPEGGIO.forEach((freq, index) => {
    const from = startAt + index * STEP;
    note(freq, from, from + STEP * 1.2, NOTE_PEAK);
  });

  const heldFrom = startAt + ARPEGGIO.length * STEP;
  note(HELD_NOTE, heldFrom, startAt + 1.7, NOTE_PEAK);

  // Quieter echo of the last arpeggio note and of the held note.
  note(ARPEGGIO[ARPEGGIO.length - 1], startAt + (ARPEGGIO.length - 1) * STEP + ECHO_DELAY, startAt + 0.75, ECHO_PEAK);
  note(HELD_NOTE, heldFrom + ECHO_DELAY, startAt + 1.9, ECHO_PEAK);

  return LENGTH;
};
