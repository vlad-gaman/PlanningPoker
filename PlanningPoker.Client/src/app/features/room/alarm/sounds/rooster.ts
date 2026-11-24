import type { AlarmSynth } from '../alarm-synth';

/** Total length of the sound, in seconds. */
const LENGTH_SECONDS = 2;
const VOICE_PEAK = 0.25;
const SILENT_GAIN = 0.0001;
const STOP_TAIL_SECONDS = 0.05;

// Pitch contour (Hz), relative to the start: a rise, a short break, then a long falling wail.
const RISE_START_HZ = 500;
const RISE_END_HZ = 900;
const BREAK_HZ = 600;
const WAIL_END_HZ = 400;
const RISE_END_SECONDS = 0.45;
const BREAK_START_SECONDS = 0.5;
const BREAK_END_SECONDS = 0.6;
const WAIL_START_SECONDS = 0.65;
const WAIL_END_SECONDS = 1.85;
const FADE_START_SECONDS = 1.95;

// Vibrato that only comes in during the wail.
const WOBBLE_HZ = 5.5;
const WOBBLE_DEPTH_HZ = 35;
const WOBBLE_IN_SECONDS = 0.9;

// Vowel-like band-pass formants, moving with the pitch.
const FORMANT_LOW_HZ = [700, 1100, 500] as const;
const FORMANT_HIGH_HZ = 2600;
const FORMANT_LOW_Q = 5;
const FORMANT_HIGH_Q = 8;
const FORMANT_HIGH_LEVEL = 0.4;

/** A rooster crow: a sawtooth voice shaped by vowel-like band-pass filters, with a rise, a break and a wail. */
export const ROOSTER_SYNTH: AlarmSynth = (context, destination, startAt) => {
  const master = context.createGain();
  master.gain.setValueAtTime(1, startAt);
  master.connect(destination);

  const voice = context.createOscillator();
  voice.type = 'sawtooth';
  const pitch = voice.frequency;
  pitch.setValueAtTime(RISE_START_HZ, startAt);
  pitch.linearRampToValueAtTime(RISE_END_HZ, startAt + RISE_END_SECONDS);
  pitch.linearRampToValueAtTime(BREAK_HZ, startAt + BREAK_START_SECONDS);
  pitch.linearRampToValueAtTime(RISE_END_HZ, startAt + BREAK_END_SECONDS);
  pitch.setValueAtTime(RISE_END_HZ, startAt + WAIL_START_SECONDS);
  pitch.exponentialRampToValueAtTime(WAIL_END_HZ, startAt + WAIL_END_SECONDS);

  const wobble = context.createOscillator();
  wobble.type = 'sine';
  wobble.frequency.setValueAtTime(WOBBLE_HZ, startAt);
  const wobbleDepth = context.createGain();
  wobbleDepth.gain.setValueAtTime(0, startAt);
  wobbleDepth.gain.setValueAtTime(0, startAt + WAIL_START_SECONDS);
  wobbleDepth.gain.linearRampToValueAtTime(WOBBLE_DEPTH_HZ, startAt + WOBBLE_IN_SECONDS);
  wobble.connect(wobbleDepth);
  wobbleDepth.connect(pitch);

  const envelope = context.createGain();
  const level = envelope.gain;
  level.setValueAtTime(SILENT_GAIN, startAt);
  level.linearRampToValueAtTime(VOICE_PEAK, startAt + 0.04);
  level.setValueAtTime(VOICE_PEAK, startAt + RISE_END_SECONDS);
  level.linearRampToValueAtTime(VOICE_PEAK * 0.1, startAt + BREAK_START_SECONDS);
  level.linearRampToValueAtTime(VOICE_PEAK, startAt + BREAK_END_SECONDS);
  level.setValueAtTime(VOICE_PEAK, startAt + FADE_START_SECONDS - 0.35);
  level.linearRampToValueAtTime(0, startAt + FADE_START_SECONDS);
  voice.connect(envelope);

  const low = context.createBiquadFilter();
  low.type = 'bandpass';
  low.Q.setValueAtTime(FORMANT_LOW_Q, startAt);
  low.frequency.setValueAtTime(FORMANT_LOW_HZ[0], startAt);
  low.frequency.linearRampToValueAtTime(FORMANT_LOW_HZ[1], startAt + RISE_END_SECONDS);
  low.frequency.exponentialRampToValueAtTime(FORMANT_LOW_HZ[2], startAt + WAIL_END_SECONDS);

  const high = context.createBiquadFilter();
  high.type = 'bandpass';
  high.Q.setValueAtTime(FORMANT_HIGH_Q, startAt);
  high.frequency.setValueAtTime(FORMANT_HIGH_HZ, startAt);
  const highLevel = context.createGain();
  highLevel.gain.setValueAtTime(FORMANT_HIGH_LEVEL, startAt);

  envelope.connect(low);
  envelope.connect(high);
  low.connect(master);
  high.connect(highLevel);
  highLevel.connect(master);

  const stopAt = startAt + LENGTH_SECONDS;
  voice.start(startAt);
  voice.stop(stopAt - STOP_TAIL_SECONDS);
  wobble.start(startAt);
  wobble.stop(stopAt - STOP_TAIL_SECONDS);
  return LENGTH_SECONDS;
};
