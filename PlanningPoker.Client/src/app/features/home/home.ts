import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, effect, inject, signal } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../core/api-request-error';
import { ErrorPopupService } from '../../core/error-popup.service';
import { FlashMessageService } from '../../core/flash-message.service';
import { MeService } from '../../core/me.service';
import { PersonDefaultsService } from '../../core/person-defaults.service';
import { RoomDefaultsService } from '../../core/room-defaults.service';
import { RoomsService } from '../../core/rooms.service';
import { PersonFields } from '../shared/person-fields';
import { createPersonForm } from '../shared/person-form';
import { RoomSettingsDialog, RoomSettingsDialogData } from './room-settings-dialog';

/** Accepts a bare room id or a pasted room link and returns the room id. */
export function extractRoomId(input: string): string {
  const path = input.trim().split(/[?#]/)[0];
  return path.split('/').filter(Boolean).pop() ?? '';
}

@Component({
  selector: 'app-home',
  imports: [ReactiveFormsModule, PersonFields],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home implements OnInit, OnDestroy {
  private readonly meService = inject(MeService);
  private readonly roomsService = inject(RoomsService);
  private readonly roomDefaults = inject(RoomDefaultsService);
  private readonly personDefaults = inject(PersonDefaultsService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly builder = inject(FormBuilder);

  protected readonly flash = inject(FlashMessageService);
  protected readonly error = signal<string | null>(null);

  protected readonly form = createPersonForm(this.builder);
  protected readonly useFunRoomName = new FormControl(this.roomDefaults.load().useFunRoomName, { nonNullable: true });
  protected readonly joinForm = this.builder.nonNullable.group({
    room: ['', Validators.required],
  });

  constructor() {
    const popup = inject(ErrorPopupService);
    effect(() => {
      const message = this.error();
      if (message) {
        popup.show(message);
      }
    });
  }

  async ngOnInit(): Promise<void> {
    const remembered = this.personDefaults.load();
    if (remembered) {
      this.form.patchValue(remembered);
    }
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
    const useFunRoomName = this.useFunRoomName.value;
    this.roomDefaults.saveUseFunRoomName(useFunRoomName);
    await this.run(async () => {
      await this.saveMe();
      const { configuration } = this.roomDefaults.load();
      const room = await firstValueFrom(this.roomsService.create({ useFunRoomName, configuration }));
      await this.router.navigate(['/room', room.id]);
    });
  }

  /** Edits the remembered settings new rooms are created with; there is no room to hand over yet. */
  protected editSettings(): void {
    const data: RoomSettingsDialogData = {
      configuration: this.roomDefaults.load().configuration,
      candidates: [],
      save: async (configuration) => this.roomDefaults.saveConfiguration(configuration),
      transfer: async () => undefined,
    };
    this.dialog.open(RoomSettingsDialog, { data });
  }

  protected async joinRoom(): Promise<void> {
    this.joinForm.markAllAsTouched();
    const roomId = extractRoomId(this.joinForm.controls.room.value);
    if (!this.validName() || !roomId) {
      return;
    }
    await this.run(async () => {
      await this.saveMe();
      await this.router.navigate(['/room', roomId]);
    });
  }

  private async saveMe(): Promise<void> {
    const request = this.form.getRawValue();
    await firstValueFrom(this.meService.update(request));
    this.personDefaults.save(request);
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
