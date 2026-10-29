import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FlashMessageService } from '../../core/flash-message.service';
import { RoomHubClient } from '../../core/room-hub.client';
import { Person } from '../../models/person';
import {
  JoinRoomResponse,
  OwnershipTransferredEvent,
  PersonExitedEvent,
  PersonJoinedEvent,
  RoomConfiguration,
  RoomDisposedEvent,
} from '../../models/room';

export const ROOM_DISPOSED_MESSAGE = 'The room was closed because everyone left.';

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

  readonly people = this.peopleState.asReadonly();
  readonly ownerGuid = this.ownerState.asReadonly();
  /** Marks are masked as "hide" until votes are revealed. */
  readonly votes = this.votesState.asReadonly();
  readonly configuration = this.configurationState.asReadonly();

  async join(roomId: string): Promise<void> {
    this.roomId = roomId;
    await this.hub.connect();
    this.bind();
    await this.joinRoom();
  }

  async leave(): Promise<void> {
    await this.close();
    await this.router.navigateByUrl('/');
  }

  /** Drops the hub connection and clears the room state; leaving the page triggers this too. */
  async close(): Promise<void> {
    this.bound = false;
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
    this.hub.on('PersonJoined', (event: PersonJoinedEvent) => this.personJoined(event));
    this.hub.on('PersonExited', (event: PersonExitedEvent) => this.personExited(event));
    this.hub.on('OwnershipTransferred', (event: OwnershipTransferredEvent) =>
      this.ownerState.set(event.newOwner.guid),
    );
    this.hub.on('RoomDisposed', (event: RoomDisposedEvent) => void this.roomDisposed(event));
    this.hub.onReconnected(() => void this.joinRoom());
  }

  private async joinRoom(): Promise<void> {
    const response = await this.hub.invoke<JoinRoomResponse>('JoinRoom', this.roomId);
    this.peopleState.set(response.people);
    this.ownerState.set(response.ownerGuid);
    this.votesState.set(response.voteStatus.votes);
    this.configurationState.set(response.configuration);
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
