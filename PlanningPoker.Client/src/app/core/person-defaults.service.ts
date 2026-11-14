import { Injectable } from '@angular/core';
import { NAME_MAX_LENGTH, NAME_MIN_LENGTH, PERSON_TYPES, UpdateMeRequest } from '../models/person';

export const PERSON_DEFAULTS_STORAGE_KEY = 'pp-person';

/**
 * Remembers, per browser, the name and role last used. The session cookie ends with the browser and the server keeps
 * people in memory, so this is what pre-fills the name when there is no session to read it from.
 */
@Injectable({ providedIn: 'root' })
export class PersonDefaultsService {
  load(): UpdateMeRequest | null {
    try {
      const stored: unknown = JSON.parse(localStorage.getItem(PERSON_DEFAULTS_STORAGE_KEY) ?? 'null');
      return isRequest(stored) ? stored : null;
    } catch {
      return null;
    }
  }

  save(request: UpdateMeRequest): void {
    try {
      localStorage.setItem(PERSON_DEFAULTS_STORAGE_KEY, JSON.stringify(request));
    } catch {
      // Storage can be unavailable (private mode); the name is then only remembered by the session.
    }
  }
}

function isRequest(value: unknown): value is UpdateMeRequest {
  if (typeof value !== 'object' || value === null) {
    return false;
  }
  const { name, personType } = value as Partial<UpdateMeRequest>;
  return (
    typeof name === 'string' &&
    name.length >= NAME_MIN_LENGTH &&
    name.length <= NAME_MAX_LENGTH &&
    PERSON_TYPES.some((option) => option.value === personType)
  );
}
