import { AlarmSoundId } from './alarm-sounds';
import { AlarmSynth } from './alarm-synth';
import { BEEP_SYNTH } from './sounds/beep';
import { BELL_SYNTH } from './sounds/bell';
import { CHIME_SYNTH } from './sounds/chime';
import { DUCK_SYNTH } from './sounds/duck';
import { GONG_SYNTH } from './sounds/gong';
import { HORN_SYNTH } from './sounds/horn';
import { KITCHEN_TIMER_SYNTH } from './sounds/kitchen-timer';
import { METROIDVANIA_SYNTH } from './sounds/metroidvania';
import { RETRO_ARCADE_SYNTH } from './sounds/retro-arcade';
import { ROOSTER_SYNTH } from './sounds/rooster';
import { SCI_FI_SYNTH } from './sounds/sci-fi';

/** The synthesizer of every sound that makes a noise: all of them but Silent. */
export const ALARM_SYNTHS: Record<Exclude<AlarmSoundId, 'silent'>, AlarmSynth> = {
  bell: BELL_SYNTH,
  chime: CHIME_SYNTH,
  gong: GONG_SYNTH,
  horn: HORN_SYNTH,
  beep: BEEP_SYNTH,
  duck: DUCK_SYNTH,
  'sci-fi': SCI_FI_SYNTH,
  metroidvania: METROIDVANIA_SYNTH,
  'retro-arcade': RETRO_ARCADE_SYNTH,
  'kitchen-timer': KITCHEN_TIMER_SYNTH,
  rooster: ROOSTER_SYNTH,
};
