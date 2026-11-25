import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AlarmSoundService } from '../../core/alarm-sound.service';
import { AlarmPlayer } from '../../features/room/alarm/alarm-player';
import { ALARM_SOUNDS, isAlarmSoundId } from '../../features/room/alarm/alarm-sounds';

/** The sound this person hears when a room Alarm elapses, as a dropdown beside the accent and theme. A pick plays a preview. */
@Component({
  selector: 'app-alarm-sound-select',
  templateUrl: './alarm-sound-select.html',
  styleUrl: './alarm-sound-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AlarmSoundSelect {
  protected readonly alarmSound = inject(AlarmSoundService);
  private readonly player = inject(AlarmPlayer);
  protected readonly sounds = ALARM_SOUNDS;

  /** Applies a value picked in the dropdown and previews it. Anything that is not a sound is ignored. */
  pick(value: string): void {
    if (isAlarmSoundId(value)) {
      this.alarmSound.set(value);
      void this.player.play(value);
    }
  }
}
