import { NgTemplateOutlet } from '@angular/common';
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
import { AlarmSoundService } from '../../core/alarm-sound.service';
import { ErrorPopupService } from '../../core/error-popup.service';
import { PersonDefaultsService } from '../../core/person-defaults.service';
import { RoomDefaultsService } from '../../core/room-defaults.service';
import { RoomHubClient } from '../../core/room-hub.client';
import { PERSON_TYPES, Person, PersonType } from '../../models/person';
import { DEFAULT_ROOM_CONFIGURATION, RoomNameWord } from '../../models/room';
import { RoomSettingsDialog, RoomSettingsDialogData } from '../home/room-settings-dialog';
import { PersonFields } from '../shared/person-fields';
import { createPersonForm } from '../shared/person-form';
import { AlarmButton, MILLISECONDS_PER_SECOND } from './alarm/alarm-button';
import { AlarmPlayer } from './alarm/alarm-player';
import { CardPicker, cardArt, cardImage, cardText } from './card-picker/card-picker';
import { EsendexCardArt } from './card-picker/esendex-card-art';
import { FireworksOverlay } from './fireworks/fireworks-overlay';
import { shouldStartFireworks } from './fireworks/should-start-fireworks';
import { Results } from './results/results';
import { RoundHistory } from './round-history/round-history';
import { RoundTimer } from './round-timer/round-timer';
import { isLolle } from './lolle';
import { HIDDEN_MARK, RoomStore } from './room-store';

/** How long the Alarm icon shakes after it elapsed; the stylesheet's animation lasts the same. */
const ALARM_JINGLE_SECONDS = 3.5;
const DISCONNECTED_MESSAGE = 'You are disconnected from the server. Trying to reconnect…';

@Component({
  selector: 'app-room',
  imports: [
    NgTemplateOutlet,
    CardPicker,
    EsendexCardArt,
    FireworksOverlay,
    Results,
    ReactiveFormsModule,
    PersonFields,
    RoundHistory,
    RoundTimer,
    AlarmButton,
  ],
  providers: [RoomStore, RoomHubClient],
  templateUrl: './room.html',
  styleUrl: './room.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Room implements OnInit, OnChanges, OnDestroy {
  readonly id = input.required<string>();

  protected readonly store = inject(RoomStore);
  private readonly dialog = inject(MatDialog);
  private readonly roomDefaults = inject(RoomDefaultsService);
  private readonly personDefaults = inject(PersonDefaultsService);
  private readonly alarmPlayer = inject(AlarmPlayer);
  protected readonly alarmSound = inject(AlarmSoundService);
  private seenAlarmElapses = 0;
  /** True for a few seconds after this person watched the Alarm elapse: the Alarm button's icon shakes then. */
  protected readonly alarmJingling = signal(false);
  protected readonly alarmEnabled = computed(() => this.store.configuration()?.alarmEnabled ?? false);
  protected readonly alarmMinutes = computed(() => this.store.configuration()?.alarmMinutes ?? DEFAULT_ROOM_CONFIGURATION.alarmMinutes);

  protected readonly personTypes = PERSON_TYPES;
  protected readonly cardImage = cardImage;

  /** The short role tag on a tile. */
  protected roleShort(type: string): string {
    return { dev: 'DEV', test: 'QA', sre: 'SRE', obs: 'OBS' }[type] ?? type;
  }
  protected readonly copied = signal<'name' | 'link' | null>(null);
  /** The number a fun room name starts with, shown before its linked words. */
  protected readonly namePrefix = computed(() => this.id().split('_')[0]);

  protected readonly form = createPersonForm(inject(FormBuilder));

  private readonly fireworks = viewChild(FireworksOverlay);
  private seenReveals = 0;

  /** PROTOTYPE (compact layout C): which of its two views shows. Follows the round: vote while open, table once revealed. */
  protected readonly compactView = signal<'vote' | 'table'>('vote');

  constructor() {
    const remembered = this.personDefaults.load();
    if (remembered) {
      this.form.patchValue(remembered);
    }
    const popup = inject(ErrorPopupService);
    effect(() => {
      const message = this.store.error();
      if (message) {
        popup.show(message);
      }
    });
    // Losing the server shows a popup that stays until the connection is back.
    effect(() => {
      if (this.store.phase() !== 'joined') {
        return;
      }
      if (this.store.connected()) {
        popup.dismiss();
      } else {
        popup.show(DISCONNECTED_MESSAGE, true);
      }
    });
    // The Alarm sounds, in this person's own sound, only for an elapse they were in the room to see happen: not for an
    // Alarm that had already elapsed when they joined, and not when it starts or is cancelled.
    effect(() => {
      const elapses = this.store.alarmElapsedCount();
      if (elapses > this.seenAlarmElapses) {
        void this.alarmPlayer.play(this.alarmSound.sound());
        this.alarmJingling.set(true);
        setTimeout(() => this.alarmJingling.set(false), ALARM_JINGLE_SECONDS * MILLISECONDS_PER_SECOND);
      }
      this.seenAlarmElapses = elapses;
    });
    // A reset or a cancel ends the jingle early.
    effect(() => {
      if (this.store.alarm().status !== 'elapsed') {
        this.alarmJingling.set(false);
      }
    });
    // Fireworks follow a reveal the user watched happen (not a room that was already revealed on joining) that ended
    // in consensus, if the room has them on; they stop as soon as the votes are gone.
    effect(() => {
      const statistics = this.store.statistics();
      const reveals = this.store.revealCount();
      const showFireworks = this.store.configuration()?.showFireworks ?? false;
      const overlay = this.fireworks();
      this.compactView.set(statistics ? 'table' : 'vote');
      const isNewReveal = reveals > this.seenReveals;
      this.seenReveals = reveals;
      if (!statistics) {
        overlay?.stop();
      } else if (shouldStartFireworks({ isNewReveal, consensus: statistics.consensus, showFireworks })) {
        overlay?.start();
      }
    });
  }

  async ngOnInit(): Promise<void> {
    await this.store.enter(this.id());
  }

  /** The router reuses this component when only the room id changes, so the store leaves the old room and enters the new one. */
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['id'] && !changes['id'].firstChange) {
      void this.store.enter(this.id());
    }
  }

  ngOnDestroy(): void {
    void this.store.close();
  }

  protected async submitName(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    await this.store.submitName(this.form.getRawValue());
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
    return person.isOffgrid === true || (!this.store.connected() && person.guid === this.store.me());
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
      candidates: this.store.people().filter((person) => person.guid !== this.store.me()),
      save: async (updated) => {
        await this.store.updateConfiguration(updated);
        this.roomDefaults.saveConfiguration(updated);
      },
      transfer: (guid) => this.store.transferOwnership(guid),
    };
    this.dialog.open(RoomSettingsDialog, { data });
  }

  protected startAlarm(): void {
    void this.store.startAlarm();
  }

  protected cancelAlarm(): void {
    void this.store.cancelAlarm();
  }

  protected resetAlarm(): void {
    void this.store.resetAlarm();
  }

  protected leave(): void {
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

  /** Whether a person gets Lolle's rose crown and wand. */
  protected isLolle(person: Person): boolean {
    return isLolle(person.name);
  }

  /** The letter an avatar shows. */
  protected initial(person: Person): string {
    return person.name.trim().charAt(0).toUpperCase();
  }

  protected roleLabel(type: string): string {
    return this.personTypes.find((option) => option.value === type)?.label ?? type;
  }

  /** The Google search a word of a fun room name links to. */
  protected searchUrl(word: RoomNameWord): string {
    return `https://www.google.com/search?q=${encodeURIComponent(word.text.toLowerCase())}+meaning`;
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
