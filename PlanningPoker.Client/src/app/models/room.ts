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

export interface VoteStatus {
  votes: Record<string, string>;
  hasEveryoneVoted: boolean;
}

export interface JoinRoomResponse {
  people: Person[];
  voteStatus: VoteStatus;
  ownerGuid: string;
  configuration: RoomConfiguration;
}

export interface PersonJoinedEvent {
  person: Person;
  vote: string | null;
  personType: string;
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

export const COUNTDOWN_MIN_SECONDS = 1;
export const COUNTDOWN_MAX_SECONDS = 30;
export const MAX_PARTICIPANTS_MIN = 1;
