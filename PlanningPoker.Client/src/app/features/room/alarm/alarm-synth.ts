/**
 * Draws one Alarm sound with the Web Audio API and nothing else: no audio files. It schedules its notes from `startAt`
 * (a time on the context's clock, in seconds), connects everything to `destination`, and returns how long the sound
 * lasts in seconds, so the caller can release the context afterwards. Every sound stays under {@link MAX_SYNTH_SECONDS}.
 */
export type AlarmSynth = (context: BaseAudioContext, destination: AudioNode, startAt: number) => number;

/** The longest an Alarm sound may last, so a ringing room never overlaps itself for long. */
export const MAX_SYNTH_SECONDS = 4;
