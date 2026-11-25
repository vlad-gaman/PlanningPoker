export type PersonType = 'dev' | 'test' | 'sre' | 'obs';

export interface Person {
  guid: string;
  name: string;
  personType: PersonType;
  /** The design on the person's face-down card; draw it with resolveCardBack, since an id may be newer than this client. */
  cardBack?: string;
  /** True when the person is still in the room but has had no connection for longer than the grace period. */
  isOffgrid?: boolean;
}

export interface UpdateMeRequest {
  name: string;
  personType: PersonType;
  /** Absent keeps the person's current card back. */
  cardBack?: string;
}

export const NAME_MIN_LENGTH = 2;
export const NAME_MAX_LENGTH = 20;

export const PERSON_TYPES: { value: PersonType; label: string }[] = [
  { value: 'dev', label: 'Developer' },
  { value: 'test', label: 'Tester' },
  { value: 'sre', label: 'SRE' },
  { value: 'obs', label: 'Observer' },
];
