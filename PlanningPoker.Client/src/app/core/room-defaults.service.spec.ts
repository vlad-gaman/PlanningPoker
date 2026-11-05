import { TestBed } from '@angular/core/testing';
import { RoomConfiguration } from '../models/room';
import {
  DEFAULT_CREATE_ROOM_REQUEST,
  ROOM_DEFAULTS_STORAGE_KEY,
  RoomDefaultsService,
} from './room-defaults.service';

const CONFIGURATION: RoomConfiguration = { cardSet: 'linear', countdownSeconds: 7, showFireworks: false, maxParticipants: 9 };

describe('RoomDefaultsService', () => {
  beforeEach(() => localStorage.clear());

  it('falls back to the built-in defaults when nothing is stored', () => {
    expect(TestBed.inject(RoomDefaultsService).load()).toEqual(DEFAULT_CREATE_ROOM_REQUEST);
  });

  it('remembers a saved configuration across service instances', () => {
    TestBed.inject(RoomDefaultsService).saveConfiguration(CONFIGURATION);

    TestBed.resetTestingModule();

    expect(TestBed.inject(RoomDefaultsService).load()).toEqual({ useFunRoomName: true, configuration: CONFIGURATION });
  });

  it('remembers the fun room name choice without touching the configuration', () => {
    const service = TestBed.inject(RoomDefaultsService);
    service.saveConfiguration(CONFIGURATION);

    service.saveUseFunRoomName(false);

    expect(service.load()).toEqual({ useFunRoomName: false, configuration: CONFIGURATION });
  });

  it.each([
    ['not json', '{'],
    ['wrong shape', '{"useFunRoomName":true}'],
    ['countdown out of range', JSON.stringify({ useFunRoomName: true, configuration: { ...CONFIGURATION, countdownSeconds: 99 } })],
    ['no participants allowed', JSON.stringify({ useFunRoomName: true, configuration: { ...CONFIGURATION, maxParticipants: 0 } })],
  ])('ignores a stored value that is %s', (_, stored) => {
    localStorage.setItem(ROOM_DEFAULTS_STORAGE_KEY, stored);

    expect(TestBed.inject(RoomDefaultsService).load()).toEqual(DEFAULT_CREATE_ROOM_REQUEST);
  });

  it('keeps working when storage is unavailable', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('denied');
    });

    expect(() => TestBed.inject(RoomDefaultsService).saveConfiguration(CONFIGURATION)).not.toThrow();
    vi.restoreAllMocks();
  });
});
