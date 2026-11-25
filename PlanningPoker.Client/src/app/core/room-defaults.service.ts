import { Injectable } from '@angular/core';
import {
  ALARM_MINUTES_MAX,
  ALARM_MINUTES_MIN,
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
      return isRequest(stored) ? withAlarm(stored) : DEFAULT_CREATE_ROOM_REQUEST;
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

/** Defaults saved before the Alarm existed carry no Alarm fields: they get the default Alarm. */
function withAlarm(request: CreateRoomRequest): CreateRoomRequest {
  const { alarmEnabled, alarmMinutes } = request.configuration as Partial<RoomConfiguration>;
  return {
    ...request,
    configuration: {
      ...request.configuration,
      alarmEnabled: alarmEnabled ?? DEFAULT_ROOM_CONFIGURATION.alarmEnabled,
      alarmMinutes: alarmMinutes ?? DEFAULT_ROOM_CONFIGURATION.alarmMinutes,
    },
  };
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
  const { cardSet, countdownSeconds, showFireworks, maxParticipants, alarmEnabled, alarmMinutes } =
    value as Partial<RoomConfiguration>;
  const alarmValid =
    (alarmEnabled === undefined || typeof alarmEnabled === 'boolean') &&
    (alarmMinutes === undefined ||
      (Number.isInteger(alarmMinutes) && alarmMinutes >= ALARM_MINUTES_MIN && alarmMinutes <= ALARM_MINUTES_MAX));
  return (
    alarmValid &&
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
