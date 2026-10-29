import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiRequestError, errorMessage } from '../../core/api-request-error';
import { FlashMessageService } from '../../core/flash-message.service';
import { RoomHubClient } from '../../core/room-hub.client';
import { ERROR_CODES, ROOM_ID_FIELD } from '../../models/api';
import { HUB_EVENTS } from '../../models/hub-events';
import { Person } from '../../models/person';
import {
  CardOption,
  CountdownEvent,
  JoinRoomResponse,
  OwnershipTransferredEvent,
  PersonExitedEvent,
  PersonJoinedEvent,
  RoomConfiguration,
  RoomDisposedEvent,
  VoteCastEvent,
  VoteStatistics,
  VoteStatus,
} from '../../models/room';

export const ROOM_NOT_FOUND_MESSAGE = 'That room does not exist (any more).';
/** The mark the server sends for a vote that is not revealed yet. */
export const HIDDEN_MARK = 'hide';
export const ROOM_DISPOSED_MESSAGE = 'The room was closed because everyone left.';

/** True when the server says the room cannot be joined because it does not exist (any more). */
export function isRoomGone(error: unknown): boolean {
  return (
    error instanceof ApiRequestError &&
    (error.hasCode(ERROR_CODES.roomNotFound) || error.hasError(ERROR_CODES.validationFailed, { field: ROOM_ID_FIELD }))
  );
}

/** Live state of the room the user is in, fed by the join response and the hub events. */
@Injectable()
export class RoomStore {
  private readonly hub = inject(RoomHubClient);
  private readonly router = inject(Router);
  private readonly flash = inject(FlashMessageService);

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

  private roomId = '';
  private bound = false;
  /** False after close(); a reconnect only rejoins while the store is open. */
  private open = false;
  /**
   * Advances on every join() and close(). A join or rejoin that resumes after an await and finds a different value was
   * superseded by a newer join or by close(), so it stops quietly instead of touching the state.
   */
  private generation = 0;
  /** While a JoinRoom call is in flight, hub events are queued here and replayed after its snapshot is applied. */
  private buffered: (() => void)[] | null = null;

  readonly people = this.peopleState.asReadonly();
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
  /**
   * Counts the reveals that arrived as a live VotesShown event. A join snapshot of an already revealed room does not
   * count, so the fireworks only follow a reveal the user watched happen.
   */
  readonly revealCount = this.revealCountState.asReadonly();

  async join(roomId: string): Promise<void> {
    const gen = ++this.generation;
    this.roomId = roomId;
    this.open = true;
    await this.hub.connect();
    if (gen !== this.generation) {
      return;
    }
    this.bind();
    await this.joinRoom(gen);
  }

  async leave(): Promise<void> {
    await this.close();
    await this.router.navigateByUrl('/');
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

  /** Drops the hub connection and clears the room state; leaving the page triggers this too. */
  async close(): Promise<void> {
    this.generation++;
    this.open = false;
    this.bound = false;
    this.buffered = null;
    await this.hub.disconnect();
    this.peopleState.set([]);
    this.ownerState.set(null);
    this.configurationState.set(null);
    this.cardsState.set([]);
    this.myMarkState.set(null);
    this.revealCountState.set(0);
    this.applyVoteStatus({
      votes: {},
      hasEveryoneVoted: false,
      votingFinished: false,
      countdown: null,
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
    this.hub.on(HUB_EVENTS.OwnershipTransferred, (event: OwnershipTransferredEvent) =>
      this.apply(() => this.ownerState.set(event.newOwner.guid)),
    );
    this.hub.on(HUB_EVENTS.RoomDisposed, (event: RoomDisposedEvent) => this.apply(() => void this.roomDisposed(event)));
    this.hub.on(HUB_EVENTS.VoteCast, (event: VoteCastEvent) => this.apply(() => this.voteCast(event)));
    this.hub.on(HUB_EVENTS.VoteStatusUpdate, (status: VoteStatus) => this.apply(() => this.applyVoteStatus(status)));
    this.hub.on(HUB_EVENTS.VotesShown, (status: VoteStatus) => this.apply(() => this.votesShown(status)));
    this.hub.on(HUB_EVENTS.CountdownUpdate, (event: CountdownEvent) => this.apply(() => this.countdownUpdate(event)));
    this.hub.on(HUB_EVENTS.CountdownReset, () => this.apply(() => this.countdownState.set(null)));
    this.hub.on(HUB_EVENTS.VotesCleared, () => this.apply(() => this.votesCleared()));
    this.hub.onReconnected(() => void this.rejoin());
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
    this.configurationState.set(response.configuration);
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
      await this.abandon(error, gen);
    }
  }

  /** Explains why the room cannot be used any more and leaves it. */
  private async abandon(error: unknown, gen: number): Promise<void> {
    if (gen !== this.generation) {
      return;
    }
    this.flash.show(isRoomGone(error) ? ROOM_NOT_FOUND_MESSAGE : errorMessage(error));
    await this.leave();
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
    this.statisticsState.set(status.statistics);
  }

  private votesShown(status: VoteStatus): void {
    this.applyVoteStatus(status);
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

  private votesCleared(): void {
    this.myMarkState.set(null);
    this.applyVoteStatus({
      votes: {},
      hasEveryoneVoted: false,
      votingFinished: false,
      countdown: null,
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

  private personExited(event: PersonExitedEvent): void {
    const guid = event.person.guid;
    this.peopleState.update((people) => people.filter((p) => p.guid !== guid));
    this.votesState.update((votes) => Object.fromEntries(Object.entries(votes).filter(([key]) => key !== guid)));
  }

  private async roomDisposed(event: RoomDisposedEvent): Promise<void> {
    if (event.roomId !== this.roomId) {
      return;
    }
    this.flash.show(ROOM_DISPOSED_MESSAGE);
    await this.leave();
  }
}
