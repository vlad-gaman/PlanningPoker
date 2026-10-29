import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiRequestError, errorMessage } from '../../core/api-request-error';
import { FlashMessageService } from '../../core/flash-message.service';
import { RoomHubClient } from '../../core/room-hub.client';
import { ERROR_CODES, ROOM_ID_FIELD } from '../../models/api';
import { HUB_EVENTS } from '../../models/hub-events';
import { Person } from '../../models/person';
import {
  JoinRoomResponse,
  OwnershipTransferredEvent,
  PersonExitedEvent,
  PersonJoinedEvent,
  RoomConfiguration,
  RoomDisposedEvent,
} from '../../models/room';

export const ROOM_NOT_FOUND_MESSAGE = 'That room does not exist (any more).';
export const ROOM_DISPOSED_MESSAGE = 'The room was closed because everyone left.';

/** True when the server says the room cannot be joined because it does not exist (any more). */
export function isRoomGone(error: unknown): boolean {
  return (
    error instanceof ApiRequestError &&
    (error.hasCode(ERROR_CODES.roomNotFound) ||
      error.hasError(ERROR_CODES.validationFailed, { field: ROOM_ID_FIELD }))
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

  /** Drops the hub connection and clears the room state; leaving the page triggers this too. */
  async close(): Promise<void> {
    this.generation++;
    this.open = false;
    this.bound = false;
    this.buffered = null;
    await this.hub.disconnect();
    this.peopleState.set([]);
    this.ownerState.set(null);
    this.votesState.set({});
    this.configurationState.set(null);
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
    this.votesState.set(response.voteStatus.votes);
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
