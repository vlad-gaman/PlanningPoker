import { Person } from './person';

export interface CardSet {
  value: string;
  display: string;
}

export interface RoomConfiguration {
  cardSet: string;
  countdownSeconds: number;
  showFireworks: boolean;
  maxParticipants: number;
}

export interface CreateRoomRequest {
  useFunRoomName: boolean;
  configuration: RoomConfiguration;
}

export interface RoomCreated {
  id: string;
}

export interface RoomExists {
  id: string;
  exists: boolean;
}

export interface CardOption {
  value: string;
  /** Text to show instead of the value, or the file name of an image in /public when it ends in .png. */
  display: string | null;
}

export interface VoteStatus {
  /** Person guid to mark; every mark is "hide" until the votes are revealed. */
  votes: Record<string, string>;
  hasEveryoneVoted: boolean;
  votingFinished: boolean;
  /** Seconds left before the votes are revealed; null when no countdown runs. */
  countdown: number | null;
}

export interface JoinRoomResponse {
  people: Person[];
  voteStatus: VoteStatus;
  cards: CardOption[];
  ownerGuid: string;
  configuration: RoomConfiguration;
}

export interface PersonJoinedEvent {
  person: Person;
  vote: string | null;
  personType: string;
}

export interface VoteCastEvent {
  personGuid: string;
}

export interface CountdownEvent {
  countdown: number;
  reset: boolean;
}

export interface PersonExitedEvent {
  person: Person;
}

export interface OwnershipTransferredEvent {
  newOwner: Person;
}

export interface RoomDisposedEvent {
  roomId: string;
}

export const DEFAULT_ROOM_CONFIGURATION: RoomConfiguration = {
  cardSet: 'modified-fibonacci',
  countdownSeconds: 5,
  showFireworks: true,
  maxParticipants: 50,
};

export const COUNTDOWN_MIN_SECONDS = 0;
export const COUNTDOWN_MAX_SECONDS = 30;
export const MAX_PARTICIPANTS_MIN = 1;
