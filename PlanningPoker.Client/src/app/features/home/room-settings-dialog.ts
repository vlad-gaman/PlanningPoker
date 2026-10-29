import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RoomsService } from '../../core/rooms.service';
import {
  COUNTDOWN_MAX_SECONDS,
  COUNTDOWN_MIN_SECONDS,
  CreateRoomRequest,
  DEFAULT_ROOM_CONFIGURATION,
  MAX_PARTICIPANTS_MIN,
} from '../../models/room';

@Component({
  selector: 'app-room-settings-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './room-settings-dialog.html',
  styleUrl: './room-settings-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomSettingsDialog {
  private readonly dialogRef = inject<MatDialogRef<RoomSettingsDialog, CreateRoomRequest>>(MatDialogRef);

  protected readonly cardSets = toSignal(inject(RoomsService).cardSets(), { initialValue: [] });
  protected readonly countdownRange = { min: COUNTDOWN_MIN_SECONDS, max: COUNTDOWN_MAX_SECONDS };

  protected readonly form = inject(FormBuilder).nonNullable.group({
    cardSet: DEFAULT_ROOM_CONFIGURATION.cardSet,
    countdownSeconds: [
      DEFAULT_ROOM_CONFIGURATION.countdownSeconds,
      [Validators.required, Validators.min(COUNTDOWN_MIN_SECONDS), Validators.max(COUNTDOWN_MAX_SECONDS)],
    ],
    showFireworks: DEFAULT_ROOM_CONFIGURATION.showFireworks,
    maxParticipants: [
      DEFAULT_ROOM_CONFIGURATION.maxParticipants,
      [Validators.required, Validators.min(MAX_PARTICIPANTS_MIN)],
    ],
    useFunRoomName: true,
  });

  protected create(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { useFunRoomName, ...configuration } = this.form.getRawValue();
    this.dialogRef.close({ useFunRoomName, configuration });
  }
}
