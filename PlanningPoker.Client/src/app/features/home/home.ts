import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../core/api-request-error';
import { FlashMessageService } from '../../core/flash-message.service';
import { MeService } from '../../core/me.service';
import { RoomsService } from '../../core/rooms.service';
import { PERSON_TYPES } from '../../models/person';
import { createPersonForm } from '../shared/person-form';
import { RoomSettingsDialog } from './room-settings-dialog';

/** Accepts a bare room id or a pasted room link and returns the room id. */
export function extractRoomId(input: string): string {
  const path = input.trim().split(/[?#]/)[0];
  return path.split('/').filter(Boolean).pop() ?? '';
}

@Component({
  selector: 'app-home',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home implements OnInit, OnDestroy {
  private readonly meService = inject(MeService);
  private readonly roomsService = inject(RoomsService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly builder = inject(FormBuilder);

  protected readonly flash = inject(FlashMessageService);
  protected readonly personTypes = PERSON_TYPES;
  protected readonly error = signal<string | null>(null);

  protected readonly form = createPersonForm(this.builder);
  protected readonly joinForm = this.builder.nonNullable.group({
    room: ['', Validators.required],
  });

  async ngOnInit(): Promise<void> {
    try {
      const me = await firstValueFrom(this.meService.get());
      this.form.patchValue({ name: me.name, personType: me.personType });
    } catch {
      // No session yet: the form simply starts empty.
    }
  }

  ngOnDestroy(): void {
    this.flash.clear();
  }

  protected async createRoom(): Promise<void> {
    if (!this.validName()) {
      return;
    }
    const request = await firstValueFrom(this.dialog.open(RoomSettingsDialog).afterClosed());
    if (!request) {
      return;
    }
    await this.run(async () => {
      await firstValueFrom(this.meService.update(this.form.getRawValue()));
      const room = await firstValueFrom(this.roomsService.create(request));
      await this.router.navigate(['/room', room.id]);
    });
  }

  protected async joinRoom(): Promise<void> {
    this.joinForm.markAllAsTouched();
    const roomId = extractRoomId(this.joinForm.controls.room.value);
    if (!this.validName() || !roomId) {
      return;
    }
    await this.run(async () => {
      await firstValueFrom(this.meService.update(this.form.getRawValue()));
      await this.router.navigate(['/room', roomId]);
    });
  }

  private validName(): boolean {
    this.form.markAllAsTouched();
    return this.form.valid;
  }

  private async run(action: () => Promise<unknown>): Promise<void> {
    this.error.set(null);
    this.flash.clear();
    try {
      await action();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }
}
