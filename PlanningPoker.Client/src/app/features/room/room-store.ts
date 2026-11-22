import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiRequestError, errorMessage } from '../../core/api-request-error';
import { ClosingBeacon } from '../../core/closing-beacon';
import { FlashMessageService } from '../../core/flash-message.service';
import { MeService } from '../../core/me.service';
import { PersonDefaultsService } from '../../core/person-defaults.service';
import { RoomHubClient } from '../../core/room-hub.client';
import { ResultsViewService } from '../../core/results-view.service';
import { RoomTabLock } from '../../core/room-tab-lock';
import { RoomsService } from '../../core/rooms.service';
import { ERROR_CODES, ROOM_ID_FIELD } from '../../models/api';
import { HUB_EVENTS } from '../../models/hub-events';
import { Person, PersonType, UpdateMeRequest } from '../../models/person';
import { orderPeople } from './order-people';
import {
  CardOption,
  CountdownEvent,
  JoinRoomResponse,
  OwnershipTransferredEvent,
  PersonBackEvent,
  PersonExitedEvent,
  PersonJoinedEvent,
  PersonOffgridEvent,
  PersonTypeChangedEvent,
  RoomConfigurationUpdatedEvent,
  RoomConfiguration,
  RoomDisposedEvent,
  RoomNameWord,
  RoundResult,
  VoteCastEvent,
  VoteStatistics,
  VoteStatus,
  VotesClearedEvent,
} from '../../models/room';

export const ROOM_NOT_FOUND_MESSAGE = 'That room does not exist (any more).';
/** The mark the server sends for a vote that is not revealed yet. */
export const HIDDEN_MARK = 'hide';
export const ROOM_OPENED_ELSEWHERE_MESSAGE = 'This room was opened in another tab, so it is no longer shown here.';
export const ROOM_DISPOSED_MESSAGE = 'The room was closed because everyone left.';
export const ROOM_CLOSED_BY_OPERATOR_MESSAGE = 'This room was closed by an operator.';
export const REMOVED_BY_OPERATOR_MESSAGE = 'You were removed by an operator.';
/** Why the server sent a RoomDisposed or PersonExited that was not the ordinary one. Must match PlanningPoker.Common/Constants/RoomEventReasons.cs. */
export const EVENT_REASONS = { closedByOperator: 'closedByOperator', removedByOperator: 'removedByOperator' } as const;

/** Milliseconds since the epoch for the server's ISO stamp; null when it is missing or unreadable. */
function parseStamp(stamp: string | undefined): number | null {
  const milliseconds = stamp ? Date.parse(stamp) : Number.NaN;
  return Number.isNaN(milliseconds) ? null : milliseconds;
}

/** True when the server says the room cannot be joined because it does not exist (any more). */
export function isRoomGone(error: unknown): boolean {
  return (
    error instanceof ApiRequestError &&
    (error.hasCode(ERROR_CODES.roomNotFound) || error.hasError(ERROR_CODES.validationFailed, { field: ROOM_ID_FIELD }))
  );
}

/** Where entering a room has got to: checking it, asking for a name, or in. */
export type RoomPhase = 'loading' | 'name' | 'joined';

/** Live state of the room the user is in, fed by the join response and the hub events. */
@Injectable()
export class RoomStore {
  private readonly hub = inject(RoomHubClient);
  private readonly router = inject(Router);
  private readonly flash = inject(FlashMessageService);
  private readonly tabLock = inject(RoomTabLock);
  private readonly resultsView = inject(ResultsViewService);
  private readonly meService = inject(MeService);
  private readonly rooms = inject(RoomsService);
  private readonly personDefaults = inject(PersonDefaultsService);
  private readonly closingBeacon = inject(ClosingBeacon);
  private stopWatchingTabs: (() => void) | null = null;
  /** Stops the page-hide watch that tells the server this page is closing; only a shown room has one. */
  private stopWatchingPage: (() => void) | null = null;

  private readonly peopleState = signal<Person[]>([]);
  private readonly ownerState = signal<string | null>(null);
  private readonly votesState = signal<Record<string, string>>({});
  private readonly configurationState = signal<RoomConfiguration | null>(null);
  private readonly cardsState = signal<CardOption[]>([]);
  private readonly votingFinishedState = signal(false);
  private readonly countdownState = signal<number | null>(null);
  private readonly everyoneVotedState = signal(false);
  private readonly myMarkState = signal<string | null>(null);
  private readonly statisticsState = signal<VoteStatistics | null>(null);
  private readonly revealCountState = signal(0);
  private readonly roundStartedAtState = signal<number | null>(null);
  private readonly roundHistoryState = signal<RoundResult[]>([]);
  private readonly connectedState = signal(true);
  private readonly meState = signal<string | null>(null);
  private readonly phaseState = signal<RoomPhase>('loading');
  private readonly errorState = signal<string | null>(null);
  private readonly nameWordsState = signal<RoomNameWord[] | null>(null);

  private roomId = '';
  private bound = false;
  /** False after close(); a reconnect only rejoins while the store is open. */
  private open = false;
  /**
   * Advances whenever an enter, a submitted name, a join, a leave or a close starts. Each async step checks it after
   * its await and stops quietly if a newer one has started, so a stale step never joins a room or changes the state.
   */
  private generation = 0;
  /** While a JoinRoom call is in flight, hub events are queued here and replayed after its snapshot is applied. */
  private buffered: (() => void)[] | null = null;

  /** False while the connection to the server is down (trying to get back) or closed. */
  readonly connected = this.connectedState.asReadonly();
  /** The guid of the person using this client; null until enter() or submitName() learns it. */
  readonly me = this.meState.asReadonly();
  /** Entering the room: `loading` while checking, `name` while asking for a name, `joined` once in. */
  readonly phase = this.phaseState.asReadonly();
  /** Why entering stopped short of joining, for the name form; null when there is nothing to report. */
  readonly error = this.errorState.asReadonly();
  /** The words of a fun room name, once the existence check has returned them; null for any other name. */
  readonly nameWords = this.nameWordsState.asReadonly();
  readonly people = this.peopleState.asReadonly();
  /** `people` with observers after voters, for the participants table only. */
  readonly orderedPeople = computed(() => orderPeople(this.peopleState()));
  readonly ownerGuid = this.ownerState.asReadonly();
  /** Marks are masked as "hide" until votes are revealed. */
  readonly votes = this.votesState.asReadonly();
  readonly configuration = this.configurationState.asReadonly();
  readonly cards = this.cardsState.asReadonly();
  /** True once the votes are revealed, until they are cleared. */
  readonly votingFinished = this.votingFinishedState.asReadonly();
  /** Seconds left before the votes are revealed; null when no countdown runs. */
  readonly countdown = this.countdownState.asReadonly();
  readonly hasEveryoneVoted = this.everyoneVotedState.asReadonly();
  /** The card this client picked, shown as selected; null before a pick and after the votes are cleared. */
  readonly myMark = this.myMarkState.asReadonly();
  /** The statistics of the revealed votes; null while voting is open. */
  readonly statistics = this.statisticsState.asReadonly();
  /** The room's revealed rounds, newest first: from the join snapshot, then renewed by every reveal. */
  readonly roundHistory = this.roundHistoryState.asReadonly();
  /** When the current round began, as milliseconds since the epoch; null before the join snapshot. */
  readonly roundStartedAt = this.roundStartedAtState.asReadonly();
  /**
   * Counts the reveals that arrived as a live VotesShown event. A join snapshot of an already revealed room does not
   * count, so the fireworks only follow a reveal the user watched happen.
   */
  readonly revealCount = this.revealCountState.asReadonly();

  private readonly myPerson = computed(() => this.peopleState().find((person) => person.guid === this.meState()));
  readonly isOwner = computed(() => this.meState() !== null && this.meState() === this.ownerState());
  readonly isObserver = computed(() => this.myPerson()?.personType === 'obs');
  readonly myPersonType = computed(() => this.myPerson()?.personType ?? null);
  /** People who have cast a vote this round; observers never vote. */
  readonly voterCount = computed(
    () =>
      this.peopleState().filter((person) => person.personType !== 'obs' && this.votesState()[person.guid] !== undefined)
        .length,
  );
  /** People who can vote this round, for the "3 of 5 voted" summary of the compact layouts. */
  readonly voterTotal = computed(() => this.peopleState().filter((person) => person.personType !== 'obs').length);

  /**
   * Gets into a room: leaves the one shown before, checks this one exists, then joins straight away when the visitor
   * has a name, or asks for one (phase `name`). A missing room sends the visitor home with a message.
   */
  async enter(roomId: string): Promise<void> {
    const gen = ++this.generation;
    this.errorState.set(null);
    this.phaseState.set('loading');
    this.nameWordsState.set(null);
    if (this.open) {
      await this.teardown();
      if (gen !== this.generation) {
        return;
      }
    }
    this.roomId = roomId;
    if (!(await this.checkRoomExists(roomId, gen))) {
      return;
    }
    let me: Person | null = null;
    try {
      me = await firstValueFrom(this.meService.get());
    } catch (error) {
      if (!(error instanceof ApiRequestError && error.hasCode(ERROR_CODES.noSession))) {
        await this.fail(error, gen, 'form');
        return;
      }
    }
    if (gen !== this.generation) {
      return;
    }
    if (me?.name) {
      this.meState.set(me.guid);
      await this.joinOrFail(roomId, gen);
      return;
    }
    this.phaseState.set('name');
  }

  /** Saves the name and role the visitor gave, remembers them for next time, and joins the room. */
  async submitName(request: UpdateMeRequest): Promise<void> {
    const gen = ++this.generation;
    this.errorState.set(null);
    try {
      const me = await firstValueFrom(this.meService.update(request));
      if (gen === this.generation) {
        this.meState.set(me.guid);
        this.personDefaults.save(request);
      }
    } catch (error) {
      await this.fail(error, gen, 'form');
      return;
    }
    if (gen === this.generation) {
      await this.joinOrFail(this.roomId, gen);
    }
  }

  private async connect(roomId: string, gen: number): Promise<void> {
    this.roomId = roomId;
    this.open = true;
    await this.hub.connect();
    if (gen !== this.generation) {
      return;
    }
    this.bind();
    await this.joinRoom(gen);
    if (gen !== this.generation) {
      return;
    }
    this.stopWatchingTabs?.();
    this.stopWatchingTabs = this.tabLock.onTaken(roomId, () => void this.giveUpToAnotherTab(gen));
    this.tabLock.claim(roomId);
    this.stopWatchingPage?.();
    this.stopWatchingPage = this.closingBeacon.watch(() => this.hub.connectionId);
    this.phaseState.set('joined');
  }

  async leave(): Promise<void> {
    await this.close();
    await this.router.navigateByUrl('/');
  }

  /**
   * The person's own choice to go: the server removes them at once. Merely dropping the connection (a refresh, a lost
   * network) keeps them in the room as offgrid, so leaving on purpose has to say so.
   */
  async leaveRoom(): Promise<void> {
    await this.send(++this.generation, 'LeaveRoom');
    await this.leave();
  }

  /** Picks a card at once and tells the room; a refused vote puts the previous pick back. */
  async vote(mark: string): Promise<void> {
    const gen = this.generation;
    const previous = this.myMarkState();
    this.myMarkState.set(mark);
    if (!(await this.send(gen, 'Vote', mark)) && gen === this.generation && this.myMarkState() === mark) {
      this.myMarkState.set(previous);
    }
  }

  async clearVotes(): Promise<void> {
    await this.send(this.generation, 'ClearVotes');
  }

  async showVotes(): Promise<void> {
    await this.send(this.generation, 'ForceShowVotes');
  }

  /** Switches the caller's own role; the server refuses an unknown one. */
  async changePersonType(personType: PersonType): Promise<boolean> {
    return this.send(this.generation, 'ChangePersonType', personType);
  }

  /** Owner only: replaces the whole room configuration. Returns the server's error instead of flashing it. */
  async updateConfiguration(configuration: RoomConfiguration): Promise<void> {
    await this.hub.invoke('UpdateRoomConfiguration', configuration);
  }

  /** Owner only: hands the room to another member. */
  async transferOwnership(targetPersonGuid: string): Promise<void> {
    await this.hub.invoke('TransferOwnership', targetPersonGuid);
  }

  /** Drops the hub connection and clears the room state; leaving the page triggers this too. */
  async close(): Promise<void> {
    this.generation++;
    await this.teardown();
  }

  private async teardown(): Promise<void> {
    this.stopWatchingTabs?.();
    this.stopWatchingTabs = null;
    this.stopWatchingPage?.();
    this.stopWatchingPage = null;
    this.open = false;
    this.connectedState.set(true);
    this.bound = false;
    this.buffered = null;
    await this.hub.disconnect();
    this.peopleState.set([]);
    this.ownerState.set(null);
    this.configurationState.set(null);
    this.cardsState.set([]);
    this.myMarkState.set(null);
    this.roundHistoryState.set([]);
    this.revealCountState.set(0);
    this.applyVoteStatus({
      votes: {},
      hasEveryoneVoted: false,
      votingFinished: false,
      countdown: null,
      roundStartedAt: '',
      statistics: null,
    });
  }

  private bind(): void {
    if (this.bound) {
      return;
    }
    this.bound = true;
    this.hub.on(HUB_EVENTS.PersonJoined, (event: PersonJoinedEvent) => this.apply(() => this.personJoined(event)));
    this.hub.on(HUB_EVENTS.PersonExited, (event: PersonExitedEvent) => this.apply(() => this.personExited(event)));
    this.hub.on(HUB_EVENTS.PersonOffgrid, (event: PersonOffgridEvent) => this.apply(() => this.personReplaced(event.person)));
    this.hub.on(HUB_EVENTS.PersonBack, (event: PersonBackEvent) => this.apply(() => this.personReplaced(event.person)));
    this.hub.on(HUB_EVENTS.OwnershipTransferred, (event: OwnershipTransferredEvent) =>
      this.apply(() => this.ownerState.set(event.newOwner.guid)),
    );
    this.hub.on(HUB_EVENTS.PersonTypeChanged, (event: PersonTypeChangedEvent) =>
      this.apply(() => this.personTypeChanged(event)),
    );
    this.hub.on(HUB_EVENTS.RoomConfigurationUpdated, (event: RoomConfigurationUpdatedEvent) =>
      this.apply(() => this.configurationUpdated(event)),
    );
    this.hub.on(HUB_EVENTS.RoomDisposed, (event: RoomDisposedEvent) => this.apply(() => void this.roomDisposed(event)));
    this.hub.on(HUB_EVENTS.VoteCast, (event: VoteCastEvent) => this.apply(() => this.voteCast(event)));
    this.hub.on(HUB_EVENTS.VoteStatusUpdate, (status: VoteStatus) => this.apply(() => this.applyVoteStatus(status)));
    this.hub.on(HUB_EVENTS.VotesShown, (status: VoteStatus) => this.apply(() => this.votesShown(status)));
    this.hub.on(HUB_EVENTS.CountdownUpdate, (event: CountdownEvent) => this.apply(() => this.countdownUpdate(event)));
    this.hub.on(HUB_EVENTS.CountdownReset, () => this.apply(() => this.countdownState.set(null)));
    this.hub.on(HUB_EVENTS.VotesCleared, (event: VotesClearedEvent) => this.apply(() => this.votesCleared(event)));
    this.hub.onReconnecting(() => this.connectedState.set(false));
    this.hub.onClosed(() => {
      if (this.open) {
        this.connectedState.set(false);
      }
    });
    this.hub.onReconnected(() => {
      this.connectedState.set(true);
      void this.rejoin();
    });
  }

  /** Runs a hub event now, or queues it when the join snapshot has not been applied yet. */
  private apply(change: () => void): void {
    if (this.buffered) {
      this.buffered.push(change);
      return;
    }
    change();
  }

  private async joinRoom(gen: number): Promise<void> {
    const queued: (() => void)[] = [];
    this.buffered = queued;
    let response: JoinRoomResponse;
    try {
      response = await this.hub.invoke<JoinRoomResponse>('JoinRoom', this.roomId);
    } catch (error) {
      // Only clear the queue this call owns; a newer join may have replaced it while this one was in flight.
      if (this.buffered === queued) {
        this.buffered = null;
      }
      throw error;
    }
    if (this.buffered === queued) {
      this.buffered = null;
    }
    if (gen !== this.generation) {
      return;
    }
    this.peopleState.set(response.people);
    this.ownerState.set(response.ownerGuid);
    this.applyVoteStatus(response.voteStatus);
    this.cardsState.set(response.cards);
    this.myMarkState.set(response.myMark ?? null);
    this.configurationState.set(response.configuration);
    this.roundHistoryState.set(response.roundHistory ?? []);
    for (const change of queued) {
      if (gen !== this.generation) {
        return;
      }
      change();
    }
  }

  /** Runs after the hub reconnected: the server has forgotten this connection, so join again. */
  private async rejoin(): Promise<void> {
    if (!this.open) {
      return;
    }
    const gen = this.generation;
    try {
      await this.joinRoom(gen);
    } catch (error) {
      await this.fail(error, gen, 'rejoin');
    }
  }

  /** Another tab opened this room: this one stops showing it and goes home. */
  private async giveUpToAnotherTab(gen: number): Promise<void> {
    if (gen !== this.generation) {
      return;
    }
    this.flash.show(ROOM_OPENED_ELSEWHERE_MESSAGE);
    await this.leave();
  }

  /** Checks the room exists before asking for a name or joining. Returns false when entering should stop. */
  private async checkRoomExists(roomId: string, gen: number): Promise<boolean> {
    let gone: boolean;
    try {
      const result = await firstValueFrom(this.rooms.exists(roomId));
      gone = !result.exists;
      if (gen === this.generation) {
        this.nameWordsState.set(result.nameWords ?? null);
      }
    } catch (error) {
      // Any other failure of the check is ignored, as joining reports it.
      gone = isRoomGone(error);
    }
    if (gen !== this.generation) {
      return false;
    }
    if (gone) {
      await this.roomGone();
      return false;
    }
    return true;
  }

  private async joinOrFail(roomId: string, gen: number): Promise<void> {
    try {
      await this.connect(roomId, gen);
    } catch (error) {
      await this.fail(error, gen, 'form');
    }
  }

  /**
   * The one place a failed step ends up. A room that is gone flashes a message and goes home. Otherwise a rejoin
   * after a reconnect flashes the reason and goes home, while the name form shows it and stays for another try.
   */
  private async fail(error: unknown, gen: number, where: 'rejoin' | 'form'): Promise<void> {
    if (gen !== this.generation) {
      return;
    }
    if (this.open) {
      await this.teardown();
      if (gen !== this.generation) {
        return;
      }
    }
    if (isRoomGone(error)) {
      await this.roomGone();
    } else if (where === 'rejoin') {
      this.flash.show(errorMessage(error));
      await this.router.navigateByUrl('/');
    } else {
      this.errorState.set(errorMessage(error));
      this.phaseState.set('name');
    }
  }

  private async roomGone(): Promise<void> {
    this.flash.show(ROOM_NOT_FOUND_MESSAGE);
    await this.router.navigateByUrl('/');
  }

  /** Runs a hub command; returns false after showing why it failed. A result that arrives after close() is dropped. */
  private async send(gen: number, method: string, ...args: unknown[]): Promise<boolean> {
    try {
      await this.hub.invoke(method, ...args);
      return true;
    } catch (error) {
      if (gen === this.generation) {
        this.flash.show(errorMessage(error));
      }
      return false;
    }
  }

  private applyVoteStatus(status: VoteStatus): void {
    this.votesState.set(status.votes);
    this.everyoneVotedState.set(status.hasEveryoneVoted);
    this.votingFinishedState.set(status.votingFinished);
    this.countdownState.set(status.countdown);
    this.roundStartedAtState.set(parseStamp(status.roundStartedAt));
    this.statisticsState.set(status.statistics);
  }

  private votesShown(status: VoteStatus): void {
    this.applyVoteStatus(status);
    if (status.roundHistory) {
      this.roundHistoryState.set(status.roundHistory);
    }
    this.revealCountState.update((count) => count + 1);
  }

  private voteCast(event: VoteCastEvent): void {
    if (!this.votingFinishedState()) {
      this.votesState.update((votes) => ({ ...votes, [event.personGuid]: HIDDEN_MARK }));
    }
  }

  private countdownUpdate(event: CountdownEvent): void {
    if (!this.votingFinishedState()) {
      this.countdownState.set(event.countdown);
    }
  }

  private votesCleared(event: VotesClearedEvent): void {
    this.resultsView.reset();
    this.myMarkState.set(null);
    this.applyVoteStatus({
      votes: {},
      hasEveryoneVoted: false,
      votingFinished: false,
      countdown: null,
      roundStartedAt: event.roundStartedAt,
      statistics: null,
    });
  }

  private personJoined(event: PersonJoinedEvent): void {
    const { person, vote } = event;
    this.peopleState.update((people) => [...people.filter((p) => p.guid !== person.guid), person]);
    if (vote !== null) {
      this.votesState.update((votes) => ({ ...votes, [person.guid]: vote }));
    }
  }

  /** An offgrid or returning person keeps their place in the list and their vote; only their own entry changes. */
  private personReplaced(person: Person): void {
    this.peopleState.update((people) => people.map((p) => (p.guid === person.guid ? person : p)));
  }

  private personTypeChanged(event: PersonTypeChangedEvent): void {
    const { person } = event;
    this.peopleState.update((people) => people.map((p) => (p.guid === person.guid ? person : p)));
    this.applyVoteStatus(event.voteStatus);
  }

  private configurationUpdated(event: RoomConfigurationUpdatedEvent): void {
    this.configurationState.set(event.configuration);
    this.cardsState.set(event.cards);
    const mark = this.myMarkState();
    if (mark !== null && !event.cards.some((card) => card.value === mark)) {
      this.myMarkState.set(null);
    }
    this.applyVoteStatus(event.voteStatus);
  }

  private personExited(event: PersonExitedEvent): void {
    const guid = event.person.guid;
    // Only the removed person is told it was an operator; everyone else sees an ordinary exit.
    if (event.reason === EVENT_REASONS.removedByOperator && guid === this.meState()) {
      this.flash.show(REMOVED_BY_OPERATOR_MESSAGE);
      void this.leave();
      return;
    }

    this.peopleState.update((people) => people.filter((p) => p.guid !== guid));
    this.votesState.update((votes) => Object.fromEntries(Object.entries(votes).filter(([key]) => key !== guid)));
  }

  private async roomDisposed(event: RoomDisposedEvent): Promise<void> {
    if (event.roomId !== this.roomId) {
      return;
    }
    this.flash.show(event.reason === EVENT_REASONS.closedByOperator ? ROOM_CLOSED_BY_OPERATOR_MESSAGE : ROOM_DISPOSED_MESSAGE);
    await this.leave();
  }
}
