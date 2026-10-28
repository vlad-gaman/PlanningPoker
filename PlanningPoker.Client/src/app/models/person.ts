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
