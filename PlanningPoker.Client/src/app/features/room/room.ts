import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiRequestError } from '../../core/api-request-error';
import { FlashMessageService } from '../../core/flash-message.service';
import { MeService } from '../../core/me.service';
import { NAME_MAX_LENGTH, NAME_MIN_LENGTH, PERSON_TYPES } from '../../models/person';
import { RoomStore } from './room-store';

export const ROOM_NOT_FOUND_MESSAGE = 'That room does not exist (any more).';
const NO_SESSION_CODE = 'no_session';
const ROOM_GONE_CODES = ['room_not_found', 'validation_failed'];

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

  protected readonly form = inject(FormBuilder).nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(NAME_MIN_LENGTH), Validators.maxLength(NAME_MAX_LENGTH)]],
    personType: this.personTypes[0].value,
  });

  async ngOnInit(): Promise<void> {
    try {
      const me = await firstValueFrom(this.meService.get());
      if (me.name) {
        await this.join();
        return;
      }
    } catch (error) {
      if (!(error instanceof ApiRequestError && error.hasCode(NO_SESSION_CODE))) {
        this.error.set(this.messageOf(error));
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
      this.error.set(this.messageOf(error));
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
      if (error instanceof ApiRequestError && ROOM_GONE_CODES.some((code) => error.hasCode(code))) {
        this.flash.show(ROOM_NOT_FOUND_MESSAGE);
        await this.store.close();
        await this.router.navigateByUrl('/');
        return;
      }
      this.error.set(this.messageOf(error));
      this.phase.set('name');
    }
  }

  private messageOf(error: unknown): string {
    return error instanceof ApiRequestError ? error.message : 'Something went wrong.';
  }
}
