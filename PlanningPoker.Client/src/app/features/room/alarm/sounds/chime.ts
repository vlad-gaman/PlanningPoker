import type { AlarmSynth } from '../alarm-synth';

/** How long the whole chime lasts, in seconds: the last ting plus its tail. */
const CHIME_SECONDS = 2.2;
/** The high pitches of the tings, in Hz, struck in this order. */
const TING_PITCHES = [1318, 1568, 1976, 2349] as const;
/** The gap between one ting and the next, in seconds. */
const TING_STAGGER_SECONDS = 0.12;
/** How long each ting takes to fade away after it is struck, in seconds. */
const TING_DECAY_SECONDS = 1.6;
/** The loudness each ting is struck at. Tings overlap, so this keeps the sum under the overall limit. */
const TING_PEAK = 0.11;
/** Attack time of each ting, in seconds. */
const ATTACK_SECONDS = 0.004;

/** A soft wind-chime: a few high sine tings, staggered and each dying away slowly. */
export const CHIME_SYNTH: AlarmSynth = (context, destination, startAt) => {
  TING_PITCHES.forEach((pitch, index) => {
    const tingAt = startAt + index * TING_STAGGER_SECONDS;
    const decayEnd = tingAt + TING_DECAY_SECONDS;
    const stopAt = Math.min(decayEnd + 0.2, startAt + CHIME_SECONDS);

    const oscillator = context.createOscillator();
    oscillator.type = 'sine';
    oscillator.frequency.setValueAtTime(pitch, tingAt);

    const gain = context.createGain();
    gain.gain.setValueAtTime(0, tingAt);
    gain.gain.linearRampToValueAtTime(TING_PEAK, tingAt + ATTACK_SECONDS);
    gain.gain.exponentialRampToValueAtTime(0.0001, decayEnd);
    gain.gain.setValueAtTime(0, stopAt);

    oscillator.connect(gain);
    gain.connect(destination);
    oscillator.start(tingAt);
    oscillator.stop(stopAt);
  });

  return CHIME_SECONDS;
};
