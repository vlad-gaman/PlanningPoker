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
  /** Whether the room's Alarm can be started. */
  alarmEnabled: boolean;
  /** How many minutes the Alarm runs once started. */
  alarmMinutes: number;
}

/** Where a room's Alarm is: not started, counting down, or run out and waiting for a reset. */
export type AlarmStatus = 'idle' | 'running' | 'elapsed';

/** The Alarm as the server sends it (PlanningPoker.Controller/Models/AlarmResponse.cs). */
export interface AlarmResponse {
  status: AlarmStatus;
  /** When the Alarm elapses or elapsed (ISO 8601); null while idle. */
  endsAt: string | null;
}

export interface CreateRoomRequest {
  useFunRoomName: boolean;
  configuration: RoomConfiguration;
}

export interface RoomCreated {
  id: string;
}

/** One word of a fun room name; a word that is not linkable is shown without a search link. */
export interface RoomNameWord {
  text: string;
  linkable: boolean;
}

export interface RoomExists {
  id: string;
  exists: boolean;
  /** The words of a fun room name, or null for any other name. */
  nameWords?: RoomNameWord[] | null;
}

export interface CardOption {
  value: string;
  /**
   * Text to show instead of the value; the file name of an image in /public when it ends in .png; or "art:NAME" for an
   * illustration the client draws.
   */
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
  /** How many votes the group cast, observers excluded; 0 when none. */
  voteCount: number;
}

export interface VoteStatistics {
  all: GroupStatistics;
  developers: GroupStatistics;
  testers: GroupStatistics;
  sres: GroupStatistics;
  /** How many votes were counted at the reveal; later joins and role changes do not move it. */
  voteCount: number;
  /** At least two votes, all the same mark, and everyone had voted. */
  consensus: boolean;
}

/** One revealed round in the room's history (PlanningPoker.Controller/Models/RoundResultResponse.cs). */
export interface RoundResult {
  /** Which revealed round of the room this was: 1 for the first reveal. */
  round: number;
  /** The mean of the numeric votes; null when no vote was numeric. */
  averageMark: number | null;
  /** When the votes were revealed (ISO 8601). */
  revealedAt: string;
}

export interface VoteStatus {
  /** Person guid to mark; every mark is "hide" until the votes are revealed. */
  votes: Record<string, string>;
  hasEveryoneVoted: boolean;
  votingFinished: boolean;
  /** Seconds left before the votes are revealed; null when no countdown runs. */
  countdown: number | null;
  /** When the current round began, in UTC (ISO 8601). */
  roundStartedAt: string;
  /** Present once the votes are revealed, until they are cleared. */
  statistics: VoteStatistics | null;
  /** The room's revealed rounds, newest first, sent with a reveal; absent while voting is open. */
  roundHistory?: RoundResult[] | null;
}

export interface JoinRoomResponse {
  people: Person[];
  voteStatus: VoteStatus;
  cards: CardOption[];
  myMark: string | null;
  ownerGuid: string;
  configuration: RoomConfiguration;
  alarm: AlarmResponse;
  /** The room's revealed rounds, newest first. */
  roundHistory: RoundResult[];
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

export interface VotesClearedEvent {
  /** When the round that the clear started began, in UTC (ISO 8601). */
  roundStartedAt: string;
}

export interface PersonExitedEvent {
  person: Person;
  /** Set only when the Operator removed the person (`removedByOperator`). */
  reason?: string | null;
}

/** The person is still in the room with their vote, but has been without a connection for longer than the grace period. */
export interface PersonOffgridEvent {
  person: Person;
}

/** A person who was announced offgrid has a connection again. */
export interface PersonBackEvent {
  person: Person;
}

export interface OwnershipTransferredEvent {
  newOwner: Person;
}

export interface PersonTypeChangedEvent {
  person: Person;
  voteStatus: VoteStatus;
}

export interface RoomConfigurationUpdatedEvent {
  configuration: RoomConfiguration;
  cards: CardOption[];
  updatedBy: string;
  voteStatus: VoteStatus;
  alarm: AlarmResponse;
}

export interface RoomDisposedEvent {
  roomId: string;
  /** Set only when the Operator closed the room (`closedByOperator`). */
  reason?: string | null;
}

export const DEFAULT_ROOM_CONFIGURATION: RoomConfiguration = {
  cardSet: 'modified-fibonacci',
  countdownSeconds: 5,
  showFireworks: true,
  maxParticipants: 50,
  alarmEnabled: true,
  alarmMinutes: 5,
};

export const ALARM_MINUTES_MIN = 1;
export const ALARM_MINUTES_MAX = 60;

export const COUNTDOWN_MIN_SECONDS = 0;
export const COUNTDOWN_MAX_SECONDS = 30;
export const MAX_PARTICIPANTS_MIN = 1;
