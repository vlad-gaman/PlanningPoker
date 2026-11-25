import { TestBed } from '@angular/core/testing';
import { AlarmSoundService, ALARM_SOUND_STORAGE_KEY } from './alarm-sound.service';

describe('AlarmSoundService', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => vi.restoreAllMocks());

  it('defaults to the bell', () => {
    const service = TestBed.inject(AlarmSoundService);

    expect(service.sound()).toBe('bell');
    expect(service.icon()).toBe('🔔');
  });

  it('reads the sound stored in this browser', () => {
    localStorage.setItem(ALARM_SOUND_STORAGE_KEY, 'duck');

    const service = TestBed.inject(AlarmSoundService);

    expect(service.sound()).toBe('duck');
    expect(service.icon()).toBe('🦆');
  });

  it('ignores a stored value that is not a sound', () => {
    localStorage.setItem(ALARM_SOUND_STORAGE_KEY, 'trombone');

    expect(TestBed.inject(AlarmSoundService).sound()).toBe('bell');
  });

  it('remembers a picked sound, silent included', () => {
    const service = TestBed.inject(AlarmSoundService);

    service.set('silent');

    expect(service.sound()).toBe('silent');
    expect(service.icon()).toBe('🔕');
    expect(localStorage.getItem(ALARM_SOUND_STORAGE_KEY)).toBe('silent');
  });

  it('keeps the pick for the session when storage is unavailable', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('denied');
    });
    const service = TestBed.inject(AlarmSoundService);

    expect(() => service.set('gong')).not.toThrow();
    expect(service.sound()).toBe('gong');
  });
});
