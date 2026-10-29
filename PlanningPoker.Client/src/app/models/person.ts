export type PersonType = 'dev' | 'test' | 'obs';

export interface Person {
  guid: string;
  name: string;
  personType: PersonType;
}

export interface UpdateMeRequest {
  name: string;
  personType: PersonType;
}

export const NAME_MIN_LENGTH = 2;
export const NAME_MAX_LENGTH = 20;

export const PERSON_TYPES: { value: PersonType; label: string }[] = [
  { value: 'dev', label: 'Developer' },
  { value: 'test', label: 'Tester' },
  { value: 'obs', label: 'Observer' },
];
