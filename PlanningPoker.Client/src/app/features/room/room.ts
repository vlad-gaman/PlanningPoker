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
  effect,
  signal,
  viewChild,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiRequestError, errorMessage } from '../../core/api-request-error';
import { ErrorPopupService } from '../../core/error-popup.service';
import { FlashMessageService } from '../../core/flash-message.service';
import { MeService } from '../../core/me.service';
import { RoomDefaultsService } from '../../core/room-defaults.service';
import { RoomHubClient } from '../../core/room-hub.client';
import { ERROR_CODES } from '../../models/api';
import { PERSON_TYPES, Person, PersonType } from '../../models/person';
import { RoomSettingsDialog, RoomSettingsDialogData } from '../home/room-settings-dialog';
import { PersonFields } from '../shared/person-fields';
import { createPersonForm } from '../shared/person-form';
import { CardPicker, cardArt, cardImage, cardText } from './card-picker/card-picker';
import { EsendexCardArt } from './card-picker/esendex-card-art';
import { FireworksOverlay } from './fireworks/fireworks-overlay';
import { Results } from './results/results';
import { RoundTimer } from './round-timer/round-timer';
import { HIDDEN_MARK, ROOM_NOT_FOUND_MESSAGE, RoomStore, isRoomGone } from './room-store';

type Phase = 'loading' | 'name' | 'joined';

const DISCONNECTED_MESSAGE = 'You are disconnected from the server. Trying to reconnect…';

@Component({
  selector: 'app-room',
  imports: [
    CardPicker,
    EsendexCardArt,
    FireworksOverlay,
    Results,
    ReactiveFormsModule,
    PersonFields,
    RoundTimer,
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
  private readonly dialog = inject(MatDialog);
  private readonly roomDefaults = inject(RoomDefaultsService);

  protected readonly personTypes = PERSON_TYPES;
  protected readonly cardImage = cardImage;

  /** The short role tag on a tile. */
  protected roleShort(type: string): string {
    return { dev: 'DEV', test: 'QA', sre: 'SRE', obs: 'OBS' }[type] ?? type;
  }
  protected readonly phase = signal<Phase>('loading');
  protected readonly error = signal<string | null>(null);
  protected readonly copied = signal<'name' | 'link' | null>(null);

  protected readonly form = createPersonForm(inject(FormBuilder));

  /**
   * Advances whenever an enter, a join, a switch or a leave starts. Each async step checks it after its await and
   * stops if a newer step has started, so a stale step never joins a room or changes the phase.
   */
  private generation = 0;

  private readonly fireworks = viewChild(FireworksOverlay);
  private seenReveals = 0;

  private readonly myGuid = signal<string | null>(null);
  protected readonly isObserver = computed(
    () => this.store.people().find((person) => person.guid === this.myGuid())?.personType === 'obs',
  );

  protected readonly myPersonType = computed(
    () => this.store.people().find((person) => person.guid === this.myGuid())?.personType ?? null,
  );
  /** People who have cast a vote this round; observers never vote. */
  protected readonly voterCount = computed(
    () =>
      this.store
        .people()
        .filter((person) => person.personType !== 'obs' && this.store.votes()[person.guid] !== undefined).length,
  );
  protected readonly isOwner = computed(() => this.myGuid() !== null && this.myGuid() === this.store.ownerGuid());

  constructor() {
    const popup = inject(ErrorPopupService);
    effect(() => {
      const message = this.error();
      if (message) {
        popup.show(message);
      }
    });
    // Losing the server shows a popup that stays until the connection is back.
    effect(() => {
      if (this.phase() !== 'joined') {
        return;
      }
      if (this.store.connected()) {
        popup.dismiss();
      } else {
        popup.show(DISCONNECTED_MESSAGE, true);
      }
    });
    // Fireworks follow a reveal the user watched happen (not a room that was already revealed on joining) that ended
    // in consensus, if the room has them on; they stop as soon as the votes are gone.
    effect(() => {
      const statistics = this.store.statistics();
      const reveals = this.store.revealCount();
      const showFireworks = this.store.configuration()?.showFireworks ?? false;
      const overlay = this.fireworks();
      const isNewReveal = reveals > this.seenReveals;
      this.seenReveals = reveals;
      if (!statistics) {
        overlay?.stop();
      } else if (isNewReveal && statistics.consensus && showFireworks) {
        overlay?.start();
      }
    });
  }

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

  protected clearVotes(): void {
    this.fireworks()?.stop();
    void this.store.clearVotes();
  }

  protected showVotes(): void {
    this.fireworks()?.stop();
    void this.store.showVotes();
  }

  /** Puts the room name, or the full room URL, on the clipboard and says so for a moment. */
  private roomUrl(): string {
    return `${location.origin}/room/${encodeURIComponent(this.id())}`;
  }

  protected async copy(what: 'name' | 'link'): Promise<void> {
    const text = what === 'name' ? this.id() : this.roomUrl();
    if (!(await writeClipboard(text))) {
      return;
    }
    this.copied.set(what);
    setTimeout(() => this.copied.set(null), 1500);
  }

  /** Offgrid people look disabled; so does your own tile while your connection to the server is down. */
  protected isOffgrid(person: Person): boolean {
    return person.isOffgrid === true || (!this.store.connected() && person.guid === this.myGuid());
  }

  protected changeRole(personType: PersonType): void {
    void this.store.changePersonType(personType);
  }

  protected openSettings(): void {
    const configuration = this.store.configuration();
    if (!configuration) {
      return;
    }
    const data: RoomSettingsDialogData = {
      configuration,
      candidates: this.store.people().filter((person) => person.guid !== this.myGuid()),
      save: async (updated) => {
        await this.store.updateConfiguration(updated);
        this.roomDefaults.saveConfiguration(updated);
      },
      transfer: (guid) => this.store.transferOwnership(guid),
    };
    this.dialog.open(RoomSettingsDialog, { data });
  }

  protected leave(): void {
    this.generation++;
    void this.store.leaveRoom();
  }

  /**
   * What a participant's mark card shows: an empty slot for no vote or an observer, a face-down card while hidden
   * (the block stays as its text for assistive tech), the card itself once revealed.
   */
  protected markOf(person: Person): {
    text: string;
    image: string | null;
    art: string | null;
    alt: string;
    hidden: boolean;
    revealed: boolean;
  } {
    const mark = this.store.votes()[person.guid];
    if (mark === undefined || person.personType === 'obs') {
      return { text: '', image: null, art: null, alt: '', hidden: false, revealed: false };
    }
    if (mark === HIDDEN_MARK && !this.store.votingFinished()) {
      return { text: '▮', image: null, art: null, alt: '', hidden: true, revealed: false };
    }
    const card = this.store.cards().find((option) => option.value === mark);
    return {
      text: (card && cardText(card)) ?? mark,
      image: card ? cardImage(card) : null,
      art: card ? cardArt(card) : null,
      alt: mark,
      hidden: false,
      revealed: true,
    };
  }

  /** The letter an avatar shows. */
  protected initial(person: Person): string {
    return person.name.trim().charAt(0).toUpperCase();
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

/**
 * Copies text to the clipboard. The async clipboard API needs a secure context (HTTPS or localhost), so over plain
 * HTTP on the local network it falls back to a hidden textarea and the legacy copy command.
 */
async function writeClipboard(text: string): Promise<boolean> {
  try {
    await navigator.clipboard.writeText(text);
    return true;
  } catch {
    const field = document.createElement('textarea');
    field.value = text;
    field.setAttribute('readonly', '');
    field.style.position = 'fixed';
    field.style.opacity = '0';
    document.body.appendChild(field);
    field.select();
    try {
      return document.execCommand('copy');
    } finally {
      field.remove();
    }
  }
}
