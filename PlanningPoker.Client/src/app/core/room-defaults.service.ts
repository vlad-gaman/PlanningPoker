import { Injectable } from '@angular/core';
import {
  COUNTDOWN_MAX_SECONDS,
  COUNTDOWN_MIN_SECONDS,
  CreateRoomRequest,
  DEFAULT_ROOM_CONFIGURATION,
  MAX_PARTICIPANTS_MIN,
  RoomConfiguration,
} from '../models/room';

export const ROOM_DEFAULTS_STORAGE_KEY = 'pp-room-defaults';

export const DEFAULT_CREATE_ROOM_REQUEST: CreateRoomRequest = {
  useFunRoomName: true,
  configuration: DEFAULT_ROOM_CONFIGURATION,
};

/** Remembers, per browser, what a new room is created with: the last saved room configuration and the fun-name choice. */
@Injectable({ providedIn: 'root' })
export class RoomDefaultsService {
  load(): CreateRoomRequest {
    try {
      const stored: unknown = JSON.parse(localStorage.getItem(ROOM_DEFAULTS_STORAGE_KEY) ?? 'null');
      return isRequest(stored) ? stored : DEFAULT_CREATE_ROOM_REQUEST;
    } catch {
      return DEFAULT_CREATE_ROOM_REQUEST;
    }
  }

  saveUseFunRoomName(useFunRoomName: boolean): void {
    this.store({ ...this.load(), useFunRoomName });
  }

  saveConfiguration(configuration: RoomConfiguration): void {
    this.store({ ...this.load(), configuration });
  }

  private store(request: CreateRoomRequest): void {
    try {
      localStorage.setItem(ROOM_DEFAULTS_STORAGE_KEY, JSON.stringify(request));
    } catch {
      // Storage can be unavailable (private mode); the defaults then apply to the next rooms of this page only.
    }
  }
}

function isRequest(value: unknown): value is CreateRoomRequest {
  if (typeof value !== 'object' || value === null) {
    return false;
  }
  const { useFunRoomName, configuration } = value as Partial<CreateRoomRequest>;
  return typeof useFunRoomName === 'boolean' && isConfiguration(configuration);
}

function isConfiguration(value: unknown): value is RoomConfiguration {
  if (typeof value !== 'object' || value === null) {
    return false;
  }
  const { cardSet, countdownSeconds, showFireworks, maxParticipants } = value as Partial<RoomConfiguration>;
  return (
    typeof cardSet === 'string' &&
    cardSet.length > 0 &&
    typeof showFireworks === 'boolean' &&
    Number.isInteger(countdownSeconds) &&
    countdownSeconds! >= COUNTDOWN_MIN_SECONDS &&
    countdownSeconds! <= COUNTDOWN_MAX_SECONDS &&
    Number.isInteger(maxParticipants) &&
    maxParticipants! >= MAX_PARTICIPANTS_MIN
  );
}
