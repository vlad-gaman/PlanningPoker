import type { AlarmSynth } from '../alarm-synth';

/** Total length of the sound, in seconds. */
const LENGTH_SECONDS = 2;
/** Wind-up tick at the very start, before the first ding. */
const TICK_SECONDS = 0.03;
const TICK_PEAK = 0.2;
const TICK_CENTER_HZ = 2500;
const DING_COUNT = 4;
const DING_OFFSET_SECONDS = 0.05;
const DING_SPACING_SECONDS = 0.45;
const DING_ATTACK_SECONDS = 0.004;
const DING_DECAY_SECONDS = 0.35;
const DING_PEAK = 0.3;
const DING_FUNDAMENTAL_HZ = 1200;
/** The bright metallic partial sits at about 2.4 times the fundamental. */
const DING_PARTIAL_RATIO = 2.4;
const DING_PARTIAL_LEVEL = 0.3;
const SILENT_GAIN = 0.0001;
const STOP_TAIL_SECONDS = 0.05;

/** A mechanical kitchen timer: a short wind-up tick, then three or four metallic dings. */
export const KITCHEN_TIMER_SYNTH: AlarmSynth = (context, destination, startAt) => {
  const master = context.createGain();
  master.gain.setValueAtTime(1, startAt);
  master.connect(destination);

  addWindUpTick(context, master, startAt);
  for (let index = 0; index < DING_COUNT; index++) {
    addDing(context, master, startAt + DING_OFFSET_SECONDS + index * DING_SPACING_SECONDS);
  }
  return LENGTH_SECONDS;
};

function addWindUpTick(context: BaseAudioContext, destination: AudioNode, when: number): void {
  const frames = Math.ceil(context.sampleRate * TICK_SECONDS);
  const noise = context.createBuffer(1, frames, context.sampleRate);
  const samples = noise.getChannelData(0);
  for (let frame = 0; frame < frames; frame++) {
    samples[frame] = Math.random() * 2 - 1;
  }

  const source = context.createBufferSource();
  source.buffer = noise;

  const band = context.createBiquadFilter();
  band.type = 'bandpass';
  band.frequency.setValueAtTime(TICK_CENTER_HZ, when);
  band.Q.setValueAtTime(1.2, when);

  const envelope = context.createGain();
  envelope.gain.setValueAtTime(0, when);
  envelope.gain.linearRampToValueAtTime(TICK_PEAK, when + 0.002);
  envelope.gain.linearRampToValueAtTime(0, when + TICK_SECONDS);

  source.connect(band);
  band.connect(envelope);
  envelope.connect(destination);
  source.start(when);
  source.stop(when + TICK_SECONDS + STOP_TAIL_SECONDS);
}

function addDing(context: BaseAudioContext, destination: AudioNode, when: number): void {
  const envelope = context.createGain();
  envelope.gain.setValueAtTime(0, when);
  envelope.gain.linearRampToValueAtTime(DING_PEAK, when + DING_ATTACK_SECONDS);
  envelope.gain.exponentialRampToValueAtTime(SILENT_GAIN, when + DING_DECAY_SECONDS);
  envelope.connect(destination);

  const fundamental = context.createOscillator();
  fundamental.type = 'sine';
  fundamental.frequency.setValueAtTime(DING_FUNDAMENTAL_HZ, when);
  fundamental.connect(envelope);

  const partialLevel = context.createGain();
  partialLevel.gain.setValueAtTime(DING_PARTIAL_LEVEL, when);
  partialLevel.connect(envelope);

  const partial = context.createOscillator();
  partial.type = 'sine';
  partial.frequency.setValueAtTime(DING_FUNDAMENTAL_HZ * DING_PARTIAL_RATIO, when);
  partial.connect(partialLevel);

  const stopAt = when + DING_DECAY_SECONDS + STOP_TAIL_SECONDS;
  fundamental.start(when);
  fundamental.stop(stopAt);
  partial.start(when);
  partial.stop(stopAt);
}
