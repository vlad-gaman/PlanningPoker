import type { AlarmSynth } from '../alarm-synth';

/** How long the gong rings, in seconds: the decay plus a short silent tail. */
const GONG_SECONDS = 3.8;
/** When the gong's tone has faded to nothing, in seconds after the strike. */
const GONG_DECAY_END_SECONDS = 3.5;
/** How long the swell takes to reach full loudness after the strike, in seconds. */
const GONG_SWELL_SECONDS = 0.8;
/** The gong's low fundamental, in Hz. */
const GONG_PITCH = 110;
/** The partials of the gong as multiples of its pitch, with their loudness and a detune in cents to make it shimmer. */
const PARTIALS = [
  { ratio: 1, peak: 0.14, detuneCents: 0 },
  { ratio: 1.51, peak: 0.08, detuneCents: -7 },
  { ratio: 2.37, peak: 0.05, detuneCents: 5 },
  { ratio: 3.13, peak: 0.03, detuneCents: -4 },
] as const;

/** The strike's burst of noise: how long it lasts, how loud it is, and where its band-pass filter sits. */
const STRIKE_SECONDS = 0.3;
const STRIKE_PEAK = 0.1;
const STRIKE_FREQUENCY = 800;

/** A deep gong: a low detuned tone that swells and then rings for seconds, with a little noise at the strike. */
export const GONG_SYNTH: AlarmSynth = (context, destination, startAt) => {
  const stopAt = startAt + GONG_SECONDS;
  const decayEnd = startAt + GONG_DECAY_END_SECONDS;
  const swellEnd = startAt + GONG_SWELL_SECONDS;

  for (const partial of PARTIALS) {
    const oscillator = context.createOscillator();
    oscillator.type = 'sine';
    oscillator.frequency.setValueAtTime(GONG_PITCH * partial.ratio, startAt);
    oscillator.detune.setValueAtTime(partial.detuneCents, startAt);

    const gain = context.createGain();
    gain.gain.setValueAtTime(0, startAt);
    gain.gain.linearRampToValueAtTime(partial.peak, swellEnd);
    gain.gain.exponentialRampToValueAtTime(0.0001, decayEnd);
    gain.gain.setValueAtTime(0, stopAt);

    oscillator.connect(gain);
    gain.connect(destination);
    oscillator.start(startAt);
    oscillator.stop(stopAt);
  }

  const strikeEnd = startAt + STRIKE_SECONDS;
  const frames = Math.ceil(context.sampleRate * STRIKE_SECONDS);
  const noise = context.createBuffer(1, frames, context.sampleRate);
  const samples = noise.getChannelData(0);
  for (let index = 0; index < frames; index++) {
    samples[index] = Math.random() * 2 - 1;
  }

  const source = context.createBufferSource();
  source.buffer = noise;

  const band = context.createBiquadFilter();
  band.type = 'bandpass';
  band.frequency.setValueAtTime(STRIKE_FREQUENCY, startAt);

  const strikeGain = context.createGain();
  strikeGain.gain.setValueAtTime(STRIKE_PEAK, startAt);
  strikeGain.gain.exponentialRampToValueAtTime(0.0001, strikeEnd);
  strikeGain.gain.setValueAtTime(0, strikeEnd);

  source.connect(band);
  band.connect(strikeGain);
  strikeGain.connect(destination);
  source.start(startAt);
  source.stop(strikeEnd);

  return GONG_SECONDS;
};
