import {
  ChangeDetectionStrategy,
  Component,
  OnChanges,
  OnDestroy,
  OnInit,
  SimpleChanges,
  inject,
  input,
  computed,
  signal,
} from '@angular/core';
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
import { RoomHubClient } from '../../core/room-hub.client';
import { ERROR_CODES } from '../../models/api';
import { PERSON_TYPES, Person } from '../../models/person';
import { createPersonForm } from '../shared/person-form';
import { CardPicker, cardImage } from './card-picker/card-picker';
import { HIDDEN_MARK, ROOM_NOT_FOUND_MESSAGE, RoomStore, isRoomGone } from './room-store';

type Phase = 'loading' | 'name' | 'joined';

@Component({
  selector: 'app-room',
  imports: [
    CardPicker,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule,
  ],
  providers: [RoomStore, RoomHubClient],
  templateUrl: './room.html',
  styleUrl: './room.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Room implements OnInit, OnChanges, OnDestroy {
  readonly id = input.required<string>();

  protected readonly store = inject(RoomStore);
  private readonly meService = inject(MeService);
  private readonly router = inject(Router);
  private readonly flash = inject(FlashMessageService);

  protected readonly personTypes = PERSON_TYPES;
  protected readonly columns = ['name', 'role', 'mark'];
  protected readonly cardImage = cardImage;
  protected readonly phase = signal<Phase>('loading');
  protected readonly error = signal<string | null>(null);

  protected readonly form = createPersonForm(inject(FormBuilder));

  /**
   * Advances whenever an enter, a join, a switch or a leave starts. Each async step checks it after its await and
   * stops if a newer step has started, so a stale step never joins a room or changes the phase.
   */
  private generation = 0;

  private readonly myGuid = signal<string | null>(null);
  protected readonly isObserver = computed(
    () => this.store.people().find((person) => person.guid === this.myGuid())?.personType === 'obs',
  );

  async ngOnInit(): Promise<void> {
    await this.enter();
  }

  /** The router reuses this component when only the room id changes, so leave the old room and enter the new one. */
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['id'] && !changes['id'].firstChange) {
      void this.switchRoom();
    }
  }

  ngOnDestroy(): void {
    this.generation++;
    void this.store.close();
  }

  protected async submitName(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const gen = this.generation;
    this.error.set(null);
    try {
      this.myGuid.set((await firstValueFrom(this.meService.update(this.form.getRawValue()))).guid);
    } catch (error) {
      if (gen === this.generation) {
        this.error.set(errorMessage(error));
      }
      return;
    }
    if (gen === this.generation) {
      await this.join(gen);
    }
  }

  protected leave(): void {
    this.generation++;
    void this.store.leave();
  }

  /** What a participant's mark cell shows: nothing for no vote or an observer, a block while hidden, the card once revealed. */
  protected markOf(person: Person): { text: string; image: string | null; alt: string } {
    const mark = this.store.votes()[person.guid];
    if (mark === undefined || person.personType === 'obs') {
      return { text: '', image: null, alt: '' };
    }
    if (mark === HIDDEN_MARK && !this.store.votingFinished()) {
      return { text: '▮', image: null, alt: '' };
    }
    const card = this.store.cards().find((option) => option.value === mark);
    return { text: card?.display ?? mark, image: card ? cardImage(card) : null, alt: mark };
  }

  protected roleLabel(type: string): string {
    return this.personTypes.find((option) => option.value === type)?.label ?? type;
  }

  private async switchRoom(): Promise<void> {
    // Supersede the enter or join of the old room before waiting on the store, so they stop when they resume.
    const gen = ++this.generation;
    await this.store.close();
    if (gen !== this.generation) {
      return;
    }
    this.error.set(null);
    this.phase.set('loading');
    await this.enter();
  }

  /** Joins straight away when the visitor has a name, otherwise asks for one. */
  private async enter(): Promise<void> {
    const gen = ++this.generation;
    try {
      const me = await firstValueFrom(this.meService.get());
      if (gen !== this.generation) {
        return;
      }
      if (me.name) {
        this.myGuid.set(me.guid);
        await this.join(gen);
        return;
      }
    } catch (error) {
      if (gen !== this.generation) {
        return;
      }
      if (!(error instanceof ApiRequestError && error.hasCode(ERROR_CODES.noSession))) {
        this.error.set(errorMessage(error));
      }
    }
    this.phase.set('name');
  }

  private async join(gen: number): Promise<void> {
    try {
      await this.store.join(this.id());
      if (gen === this.generation) {
        this.phase.set('joined');
      }
    } catch (error) {
      if (gen !== this.generation) {
        return;
      }
      await this.store.close();
      if (gen !== this.generation) {
        return;
      }
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
