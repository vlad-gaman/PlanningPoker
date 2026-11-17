import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { errorMessage } from '../../core/api-request-error';
import { Person } from '../../models/person';
import { RoomsService } from '../../core/rooms.service';
import {
  COUNTDOWN_MAX_SECONDS,
  COUNTDOWN_MIN_SECONDS,
  MAX_PARTICIPANTS_MIN,
  RoomConfiguration,
} from '../../models/room';

/** What the owner's room settings dialog needs: the current configuration, saving it, and ownership transfer. */
export interface RoomSettingsDialogData {
  configuration: RoomConfiguration;
  /** Other members the owner can hand the room to. */
  candidates: Person[];
  save: (configuration: RoomConfiguration) => Promise<void>;
  transfer: (personGuid: string) => Promise<void>;
}

@Component({
  selector: 'app-room-settings-dialog',
  imports: [ReactiveFormsModule, MatDialogModule],
  templateUrl: './room-settings-dialog.html',
  styleUrl: './room-settings-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomSettingsDialog {
  private readonly dialogRef = inject<MatDialogRef<RoomSettingsDialog>>(MatDialogRef);

  protected readonly data = inject<RoomSettingsDialogData>(MAT_DIALOG_DATA);
  protected readonly error = signal<string | null>(null);
  protected readonly transferTarget = signal<string | null>(null);

  protected readonly cardSets = toSignal(inject(RoomsService).cardSets(), { initialValue: [] });
  protected readonly countdownRange = { min: COUNTDOWN_MIN_SECONDS, max: COUNTDOWN_MAX_SECONDS };

  protected readonly form = inject(FormBuilder).nonNullable.group({
    cardSet: this.data.configuration.cardSet,
    countdownSeconds: [
      this.data.configuration.countdownSeconds,
      [Validators.required, Validators.min(COUNTDOWN_MIN_SECONDS), Validators.max(COUNTDOWN_MAX_SECONDS)],
    ],
    showFireworks: this.data.configuration.showFireworks,
    maxParticipants: [
      this.data.configuration.maxParticipants,
      [Validators.required, Validators.min(MAX_PARTICIPANTS_MIN)],
    ],
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.error.set(null);
    try {
      await this.data.save(this.form.getRawValue());
      this.dialogRef.close();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  protected async transferOwnership(): Promise<void> {
    const target = this.transferTarget();
    if (!target) {
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
