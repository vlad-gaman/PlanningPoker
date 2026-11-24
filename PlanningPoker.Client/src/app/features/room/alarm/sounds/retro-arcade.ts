import type { AlarmSynth } from '../alarm-synth';

const LENGTH = 1;
const BLIP_PEAK = 0.18;
const HOLD_PEAK = 0.2;

/** The coin pickup: a short B5 that jumps to a held E6. */
const PICKUP = { first: 987.77, second: 1318.51 };
/** The rapid ascending run of short blips that follows. */
const RUN = [880, 1046.5, 1318.51, 1567.98, 1760];

/**
 * A coin-pickup style arcade blip run: two square-wave notes in quick succession, then a rapid ascending run of short
 * square blips.
 */
export const RETRO_ARCADE_SYNTH: AlarmSynth = (context, destination, startAt) => {
  const out = context.createGain();
  out.gain.value = 1;
  out.connect(destination);

  // One square note with a quick attack and a release that ends at `to`.
  const square = (freq: number, from: number, to: number, peak: number) => {
    const osc = context.createOscillator();
    osc.type = 'square';
    osc.frequency.value = freq;
    const env = context.createGain();
    env.gain.setValueAtTime(0, from);
    env.gain.linearRampToValueAtTime(peak, from + 0.005);
    env.gain.setValueAtTime(peak, Math.max(from + 0.005, to - 0.02));
    env.gain.linearRampToValueAtTime(0, to);
    osc.connect(env);
    env.connect(out);
    osc.start(from);
    osc.stop(to + 0.02);
  };

  square(PICKUP.first, startAt, startAt + 0.08, BLIP_PEAK);
  square(PICKUP.second, startAt + 0.08, startAt + 0.43, HOLD_PEAK);

  RUN.forEach((freq, index) => {
    const from = startAt + 0.5 + index * 0.07;
    square(freq, from, from + 0.05, BLIP_PEAK);
  });

  return LENGTH;
};
