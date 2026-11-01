import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { errorMessage } from '../../core/api-request-error';
import { Person } from '../../models/person';
import { RoomsService } from '../../core/rooms.service';
import {
  COUNTDOWN_MAX_SECONDS,
  COUNTDOWN_MIN_SECONDS,
  CreateRoomRequest,
  DEFAULT_ROOM_CONFIGURATION,
  MAX_PARTICIPANTS_MIN,
  RoomConfiguration,
} from '../../models/room';

/** Opens the dialog for a room that exists: prefilled, saved through the room, with ownership transfer. */
export interface RoomSettingsDialogData {
  configuration: RoomConfiguration;
  /** Other members the owner can hand the room to. */
  candidates: Person[];
  save: (configuration: RoomConfiguration) => Promise<void>;
  transfer: (personGuid: string) => Promise<void>;
}

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

  protected readonly data = inject<RoomSettingsDialogData | null>(MAT_DIALOG_DATA, { optional: true });
  protected readonly error = signal<string | null>(null);
  protected readonly transferTarget = signal<string | null>(null);

  protected readonly cardSets = toSignal(inject(RoomsService).cardSets(), { initialValue: [] });
  protected readonly countdownRange = { min: COUNTDOWN_MIN_SECONDS, max: COUNTDOWN_MAX_SECONDS };

  protected readonly form = inject(FormBuilder).nonNullable.group({
    cardSet: (this.data?.configuration ?? DEFAULT_ROOM_CONFIGURATION).cardSet,
    countdownSeconds: [
      (this.data?.configuration ?? DEFAULT_ROOM_CONFIGURATION).countdownSeconds,
      [Validators.required, Validators.min(COUNTDOWN_MIN_SECONDS), Validators.max(COUNTDOWN_MAX_SECONDS)],
    ],
    showFireworks: (this.data?.configuration ?? DEFAULT_ROOM_CONFIGURATION).showFireworks,
    maxParticipants: [
      (this.data?.configuration ?? DEFAULT_ROOM_CONFIGURATION).maxParticipants,
      [Validators.required, Validators.min(MAX_PARTICIPANTS_MIN)],
    ],
    useFunRoomName: true,
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { useFunRoomName, ...configuration } = this.form.getRawValue();
    if (!this.data) {
      this.dialogRef.close({ useFunRoomName, configuration });
      return;
    }
    this.error.set(null);
    try {
      await this.data.save(configuration);
      this.dialogRef.close();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  protected async transferOwnership(): Promise<void> {
    const target = this.transferTarget();
    if (!this.data || !target) {
      return;
    }
    this.error.set(null);
    try {
      await this.data.transfer(target);
      this.dialogRef.close();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }
}
