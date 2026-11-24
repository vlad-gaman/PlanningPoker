import type { AlarmSynth } from '../alarm-synth';

const LENGTH = 1.5;
const SWEEP_PEAK = 0.2;
const TONE_PEAK = 0.25;
const TREMOLO_HZ = 36;

/** Fades a gain in from silence, holds it, and fades it back to silence at `end`. */
function envelope(gain: GainNode, from: number, end: number, peak: number) {
  gain.gain.setValueAtTime(0, from);
  gain.gain.linearRampToValueAtTime(peak, from + 0.02);
  gain.gain.setValueAtTime(peak, Math.max(from + 0.02, end - 0.04));
  gain.gain.linearRampToValueAtTime(0, end);
}

/**
 * A sci-fi computer alert: a rising triangle sweep, two sine pulses with a tremolo shimmer, then a falling resolve.
 */
export const SCI_FI_SYNTH: AlarmSynth = (context, destination, startAt) => {
  const out = context.createGain();
  out.gain.value = 1;
  out.connect(destination);

  const voice = (type: OscillatorType, from: number, to: number, peak: number) => {
    const osc = context.createOscillator();
    osc.type = type;
    const env = context.createGain();
    envelope(env, from, to, peak);
    osc.connect(env);
    env.connect(out);
    osc.start(from);
    osc.stop(to + 0.02);
    return osc;
  };

  // Rising sweep, 400 to 1600 Hz.
  const sweep = voice('triangle', startAt, startAt + 0.4, SWEEP_PEAK);
  sweep.frequency.setValueAtTime(400, startAt);
  sweep.frequency.exponentialRampToValueAtTime(1600, startAt + 0.4);

  // Two sine pulses; each runs through a gain that a slow LFO wobbles, for the tremolo shimmer.
  const pulse = (from: number, to: number) => {
    const osc = context.createOscillator();
    osc.type = 'sine';
    osc.frequency.value = 1200;
    const shimmer = context.createGain();
    shimmer.gain.value = 0.7;
    const lfo = context.createOscillator();
    lfo.frequency.value = TREMOLO_HZ;
    const depth = context.createGain();
    depth.gain.value = 0.3;
    lfo.connect(depth);
    depth.connect(shimmer.gain);
    const env = context.createGain();
    envelope(env, from, to, TONE_PEAK);
    osc.connect(shimmer);
    shimmer.connect(env);
    env.connect(out);
    osc.start(from);
    osc.stop(to + 0.02);
    lfo.start(from);
    lfo.stop(to + 0.02);
  };
  pulse(startAt + 0.46, startAt + 0.62);
  pulse(startAt + 0.7, startAt + 0.86);

  // Descending resolve, 1000 to 500 Hz.
  const resolve = voice('triangle', startAt + 0.95, startAt + 1.45, TONE_PEAK);
  resolve.frequency.setValueAtTime(1000, startAt + 0.95);
  resolve.frequency.exponentialRampToValueAtTime(500, startAt + 1.45);

  return LENGTH;
};
