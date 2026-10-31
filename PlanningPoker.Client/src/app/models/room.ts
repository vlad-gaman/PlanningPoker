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

export interface MarkShare {
  mark: string;
  /** Share of the group's votes, 0 to 100, one decimal. */
  percentage: number;
}

export interface GroupStatistics {
  /** The marks that were cast, in card-set order. */
  marks: MarkShare[];
  /** The mark with the strictly highest share; null for a tie or an empty group. */
  highestMark: string | null;
  /** Mean of the numeric marks, two decimals; null when none is numeric. */
  averageMark: number | null;
}

export interface VoteStatistics {
  all: GroupStatistics;
  developers: GroupStatistics;
  testers: GroupStatistics;
  /** At least two votes, all the same mark, and everyone had voted. */
  consensus: boolean;
}

export interface VoteStatus {
  /** Person guid to mark; every mark is "hide" until the votes are revealed. */
  votes: Record<string, string>;
  hasEveryoneVoted: boolean;
  votingFinished: boolean;
  /** Seconds left before the votes are revealed; null when no countdown runs. */
  countdown: number | null;
  /** Present once the votes are revealed, until they are cleared. */
  statistics: VoteStatistics | null;
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
