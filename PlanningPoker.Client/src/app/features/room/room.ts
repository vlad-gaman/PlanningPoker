import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiRequestError, errorMessage } from '../../core/api-request-error';
import { FlashMessageService } from '../../core/flash-message.service';
import { MeService } from '../../core/me.service';
import { ERROR_CODES } from '../../models/api';
import { PERSON_TYPES } from '../../models/person';
import { createPersonForm } from '../shared/person-form';
import { ROOM_NOT_FOUND_MESSAGE, RoomStore, isRoomGone } from './room-store';

type Phase = 'loading' | 'name' | 'joined';

@Component({
  selector: 'app-room',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule,
  ],
  providers: [RoomStore],
  templateUrl: './room.html',
  styleUrl: './room.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Room implements OnInit, OnDestroy {
  readonly id = input.required<string>();

  protected readonly store = inject(RoomStore);
  private readonly meService = inject(MeService);
  private readonly router = inject(Router);
  private readonly flash = inject(FlashMessageService);

  protected readonly personTypes = PERSON_TYPES;
  protected readonly columns = ['name', 'role'];
  protected readonly phase = signal<Phase>('loading');
  protected readonly error = signal<string | null>(null);

  protected readonly form = createPersonForm(inject(FormBuilder));

  async ngOnInit(): Promise<void> {
    try {
      const me = await firstValueFrom(this.meService.get());
      if (me.name) {
        await this.join();
        return;
      }
    } catch (error) {
      if (!(error instanceof ApiRequestError && error.hasCode(ERROR_CODES.noSession))) {
        this.error.set(errorMessage(error));
      }
    }
    this.phase.set('name');
  }

  ngOnDestroy(): void {
    void this.store.close();
  }

  protected async submitName(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.error.set(null);
    try {
      await firstValueFrom(this.meService.update(this.form.getRawValue()));
    } catch (error) {
      this.error.set(errorMessage(error));
      return;
    }
    await this.join();
  }

  protected leave(): void {
    void this.store.leave();
  }

  protected roleLabel(type: string): string {
    return this.personTypes.find((option) => option.value === type)?.label ?? type;
  }

  private async join(): Promise<void> {
    try {
      await this.store.join(this.id());
      this.phase.set('joined');
    } catch (error) {
      await this.store.close();
      if (isRoomGone(error)) {
        this.flash.show(ROOM_NOT_FOUND_MESSAGE);
        await this.router.navigateByUrl('/');
        return;
      }
      this.error.set(errorMessage(error));
      this.phase.set('name');
    }
  }
}
