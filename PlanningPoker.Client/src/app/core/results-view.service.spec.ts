import { TestBed } from '@angular/core/testing';
import { RESULTS_BY_ROLE_STORAGE_KEY, ResultsViewService } from './results-view.service';

describe('ResultsViewService', () => {
  beforeEach(() => localStorage.clear());

  afterEach(() => vi.restoreAllMocks());

  it('is off by default', () => {
    expect(TestBed.inject(ResultsViewService).byRole()).toBe(false);
  });

  it('starts on when the stored value is on', () => {
    localStorage.setItem(RESULTS_BY_ROLE_STORAGE_KEY, 'on');

    expect(TestBed.inject(ResultsViewService).byRole()).toBe(true);
  });

  it('treats any other stored value as off', () => {
    localStorage.setItem(RESULTS_BY_ROLE_STORAGE_KEY, 'yes');

    expect(TestBed.inject(ResultsViewService).byRole()).toBe(false);
  });

  it('toggles the state and stores it under the one global key', () => {
    const service = TestBed.inject(ResultsViewService);

    service.toggle();

    expect(service.byRole()).toBe(true);
    expect(localStorage.getItem(RESULTS_BY_ROLE_STORAGE_KEY)).toBe('on');

    service.toggle();

    expect(service.byRole()).toBe(false);
    expect(localStorage.getItem(RESULTS_BY_ROLE_STORAGE_KEY)).toBe('off');
  });

  it('keeps the state across a reload', () => {
    TestBed.inject(ResultsViewService).toggle();
    TestBed.resetTestingModule();

    expect(TestBed.inject(ResultsViewService).byRole()).toBe(true);
  });

  it('reset turns the state off and stores off', () => {
    const service = TestBed.inject(ResultsViewService);
    service.toggle();

    service.reset();

    expect(service.byRole()).toBe(false);
    expect(localStorage.getItem(RESULTS_BY_ROLE_STORAGE_KEY)).toBe('off');
  });

  it('still toggles when storage is unavailable', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('storage denied');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('storage denied');
    });
    const service = TestBed.inject(ResultsViewService);

    expect(service.byRole()).toBe(false);
    service.toggle();
    expect(service.byRole()).toBe(true);
  });
});
