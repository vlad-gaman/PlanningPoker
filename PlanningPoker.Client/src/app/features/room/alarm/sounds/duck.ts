import type { AlarmSynth } from '../alarm-synth';

const QUACK_SECONDS = 0.2;
const GLIDE_SECONDS = 0.15;
const PEAK_GAIN = 0.35;
/** Vibrato-like buzz: a slow LFO wobbling the pitch by a few hertz. */
const BUZZ_HZ = 30;
const BUZZ_DEPTH_HZ = 15;
const BAND_PASS_HZ = 900;
const EDGE_SECONDS = 0.02;
/** When the second quack starts, measured from the first. */
const SECOND_QUACK_OFFSET = 0.28;
/** The whole sound: both quacks end by about 0.48 s, so this leaves a little slack. */
const LENGTH_SECONDS = 0.5;

/** One quack: a sawtooth gliding down in pitch through a band-pass filter, with a slight buzz. */
function quack(context: BaseAudioContext, destination: AudioNode, at: number, fromHz: number, toHz: number) {
  const end = at + QUACK_SECONDS;

  const filter = context.createBiquadFilter();
  filter.type = 'bandpass';
  filter.frequency.setValueAtTime(BAND_PASS_HZ, at);
  filter.Q.value = 3;

  const envelope = context.createGain();
  envelope.gain.setValueAtTime(0, at);
  envelope.gain.linearRampToValueAtTime(PEAK_GAIN, at + EDGE_SECONDS);
  envelope.gain.setValueAtTime(PEAK_GAIN, at + GLIDE_SECONDS);
  envelope.gain.linearRampToValueAtTime(0, end);

  const voice = context.createOscillator();
  voice.type = 'sawtooth';
  voice.frequency.setValueAtTime(fromHz, at);
  voice.frequency.exponentialRampToValueAtTime(toHz, at + GLIDE_SECONDS);

  const buzz = context.createOscillator();
  buzz.frequency.setValueAtTime(BUZZ_HZ, at);
  const buzzDepth = context.createGain();
  buzzDepth.gain.setValueAtTime(BUZZ_DEPTH_HZ, at);
  buzz.connect(buzzDepth);
  buzzDepth.connect(voice.frequency);

  voice.connect(filter);
  filter.connect(envelope);
  envelope.connect(destination);

  voice.start(at);
  voice.stop(end);
  buzz.start(at);
  buzz.stop(end);
}

/** A duck quack, twice: the second quack a bit lower than the first. */
export const DUCK_SYNTH: AlarmSynth = (context, destination, startAt) => {
  quack(context, destination, startAt, 600, 300);
  quack(context, destination, startAt + SECOND_QUACK_OFFSET, 500, 250);
  return LENGTH_SECONDS;
};
