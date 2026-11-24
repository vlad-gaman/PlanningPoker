import type { AlarmSynth } from '../alarm-synth';

/** How long the struck bell rings, in seconds. */
const BELL_SECONDS = 2.4;
/** The bell's fundamental pitch, in Hz. */
const BELL_PITCH = 660;
/** Attack time of each partial, in seconds: short, so the strike is bright. */
const ATTACK_SECONDS = 0.003;

/**
 * Inharmonic partials of a struck bell. Each one is a multiple of the pitch that is not a whole number, which gives the
 * metallic ring. Higher partials are quieter and die away sooner than the fundamental.
 */
const PARTIALS = [
  { ratio: 1, peak: 0.28, decaySeconds: 2.2 },
  { ratio: 2.76, peak: 0.12, decaySeconds: 1.2 },
  { ratio: 5.4, peak: 0.05, decaySeconds: 0.5 },
] as const;

/** A clear struck bell: bright attack, inharmonic partials, a decay of about two seconds. */
export const BELL_SYNTH: AlarmSynth = (context, destination, startAt) => {
  const stopAt = startAt + BELL_SECONDS;

  for (const partial of PARTIALS) {
    const oscillator = context.createOscillator();
    oscillator.type = 'sine';
    oscillator.frequency.setValueAtTime(BELL_PITCH * partial.ratio, startAt);

    const gain = context.createGain();
    const decayEnd = startAt + partial.decaySeconds;
    gain.gain.setValueAtTime(0, startAt);
    gain.gain.linearRampToValueAtTime(partial.peak, startAt + ATTACK_SECONDS);
    gain.gain.exponentialRampToValueAtTime(0.0001, decayEnd);
    gain.gain.setValueAtTime(0, stopAt);

    oscillator.connect(gain);
    gain.connect(destination);
    oscillator.start(startAt);
    oscillator.stop(stopAt);
  }

  return BELL_SECONDS;
};
