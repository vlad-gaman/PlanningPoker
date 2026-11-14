import { TestBed } from '@angular/core/testing';
import { PERSON_DEFAULTS_STORAGE_KEY, PersonDefaultsService } from './person-defaults.service';

describe('PersonDefaultsService', () => {
  beforeEach(() => localStorage.clear());

  it('has nothing when nothing is stored', () => {
    expect(TestBed.inject(PersonDefaultsService).load()).toBeNull();
  });

  it('remembers a saved person across service instances', () => {
    TestBed.inject(PersonDefaultsService).save({ name: 'Zoe', personType: 'obs' });

    TestBed.resetTestingModule();

    expect(TestBed.inject(PersonDefaultsService).load()).toEqual({ name: 'Zoe', personType: 'obs' });
  });

  it.each([
    ['not json', '{'],
    ['wrong shape', '{"name":"Zoe"}'],
    ['unknown role', '{"name":"Zoe","personType":"king"}'],
    ['name too short', '{"name":"Z","personType":"dev"}'],
    ['name too long', JSON.stringify({ name: 'x'.repeat(21), personType: 'dev' })],
  ])('ignores a stored value that is %s', (_, stored) => {
    localStorage.setItem(PERSON_DEFAULTS_STORAGE_KEY, stored);

    expect(TestBed.inject(PersonDefaultsService).load()).toBeNull();
  });

  it('keeps working when storage is unavailable', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('denied');
    });

    expect(() => TestBed.inject(PersonDefaultsService).save({ name: 'Zoe', personType: 'dev' })).not.toThrow();
    vi.restoreAllMocks();
  });
});
