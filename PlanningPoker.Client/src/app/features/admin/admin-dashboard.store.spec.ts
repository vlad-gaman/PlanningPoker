import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { AdminOverviewService } from '../../core/admin-overview.service';
import { ApiRequestError } from '../../core/api-request-error';
import { AdminOverview } from '../../models/admin-overview';
import { ERROR_CODES } from '../../models/api';
import { OVERVIEW } from '../../testing/admin-overview.testing';
import { AdminDashboardStore, POLL_INTERVAL_MS } from './admin-dashboard.store';

function setHidden(hidden: boolean): void {
  Object.defineProperty(document, 'hidden', { configurable: true, get: () => hidden });
  document.dispatchEvent(new Event('visibilitychange'));
}

describe('AdminDashboardStore', () => {
  const service = { get: vi.fn(), closeRoom: vi.fn(), removePerson: vi.fn() };
  let store: AdminDashboardStore;

  beforeEach(() => {
    vi.useFakeTimers();
    setHidden(false);
    service.get.mockReset().mockReturnValue(of(OVERVIEW));
    service.closeRoom.mockReset().mockReturnValue(of(null));
    service.removePerson.mockReset().mockReturnValue(of(null));
    TestBed.configureTestingModule({ providers: [AdminDashboardStore, { provide: AdminOverviewService, useValue: service }] });
    store = TestBed.inject(AdminDashboardStore);
  });

  afterEach(() => {
    store.stop();
    setHidden(false);
    vi.useRealTimers();
  });

  it('has no overview before the first answer', () => {
    expect(store.overview()).toBeNull();
    expect(store.errorCount()).toBe(0);
  });

  it('asks at once when it starts', () => {
    store.start();

    expect(service.get).toHaveBeenCalledTimes(1);
    expect(store.overview()).toEqual(OVERVIEW);
  });

  it('asks again every five seconds', () => {
    store.start();

    vi.advanceTimersByTime(POLL_INTERVAL_MS);
    vi.advanceTimersByTime(POLL_INTERVAL_MS);

    expect(service.get).toHaveBeenCalledTimes(3);
  });

  it('picks up what changed', () => {
    store.start();
    const changed: AdminOverview = { ...OVERVIEW, rooms: [] };
    service.get.mockReturnValue(of(changed));

    vi.advanceTimersByTime(POLL_INTERVAL_MS);

    expect(store.overview()?.rooms).toEqual([]);
  });

  it('stops asking once stopped', () => {
    store.start();
    store.stop();

    vi.advanceTimersByTime(POLL_INTERVAL_MS * 3);

    expect(service.get).toHaveBeenCalledTimes(1);
  });

  it('stops asking when the page is destroyed', () => {
    store.start();

    store.ngOnDestroy();
    vi.advanceTimersByTime(POLL_INTERVAL_MS * 3);

    expect(service.get).toHaveBeenCalledTimes(1);
  });

  it('starting twice does not double the polling', () => {
    store.start();
    store.start();

    vi.advanceTimersByTime(POLL_INTERVAL_MS);

    expect(service.get).toHaveBeenCalledTimes(3);
  });

  it('pauses while the tab is hidden and asks again at once when it is shown', () => {
    store.start();

    setHidden(true);
    vi.advanceTimersByTime(POLL_INTERVAL_MS * 4);
    expect(service.get).toHaveBeenCalledTimes(1);

    setHidden(false);
    expect(service.get).toHaveBeenCalledTimes(2);
    vi.advanceTimersByTime(POLL_INTERVAL_MS);
    expect(service.get).toHaveBeenCalledTimes(3);
  });

  it('does not start a second request while one is still waiting', () => {
    const slow = new Subject<AdminOverview>();
    service.get.mockReturnValue(slow);
    store.start();

    vi.advanceTimersByTime(POLL_INTERVAL_MS * 3);

    expect(service.get).toHaveBeenCalledTimes(1);
    slow.next(OVERVIEW);
    slow.complete();
    expect(store.overview()).toEqual(OVERVIEW);
  });

  it('keeps the last overview and says so when a refresh fails, then recovers', () => {
    store.start();
    service.get.mockReturnValue(throwError(() => new Error('offline')));

    vi.advanceTimersByTime(POLL_INTERVAL_MS);
    expect(store.refreshFailed()).toBe(true);
    expect(store.overview()).toEqual(OVERVIEW);

    service.get.mockReturnValue(of(OVERVIEW));
    vi.advanceTimersByTime(POLL_INTERVAL_MS);
    expect(store.refreshFailed()).toBe(false);
  });

  it('counts the errors in the issues', () => {
    store.start();

    expect(store.errorCount()).toBe(1);
  });

  describe('actions', () => {
    it('closes a room and refreshes at once', async () => {
      const done = await store.closeRoom('blue-fox');

      expect(done).toBe(true);
      expect(service.closeRoom).toHaveBeenCalledWith('blue-fox');
      expect(service.get).toHaveBeenCalledTimes(1);
      expect(store.actionError()).toBeNull();
    });

    it('removes a person and refreshes at once', async () => {
      const done = await store.removePerson('blue-fox', 'p-bob');

      expect(done).toBe(true);
      expect(service.removePerson).toHaveBeenCalledWith('blue-fox', 'p-bob');
      expect(service.get).toHaveBeenCalledTimes(1);
    });

    it('shows the server message when an action fails, and still refreshes', async () => {
      service.closeRoom.mockReturnValue(throwError(() => new ApiRequestError(500, [{ code: 'internal_error', message: 'An unexpected error occurred.' }])));

      const done = await store.closeRoom('blue-fox');

      expect(done).toBe(false);
      expect(store.actionError()).toBe('An unexpected error occurred.');
      expect(service.get).toHaveBeenCalledTimes(1);
    });

    it('treats a room that is already gone as done', async () => {
      service.closeRoom.mockReturnValue(throwError(() => new ApiRequestError(404, [{ code: ERROR_CODES.roomNotFound, message: 'The room does not exist.' }])));

      expect(await store.closeRoom('blue-fox')).toBe(true);
      expect(store.actionError()).toBeNull();
    });

    it('treats a person who is already gone as done', async () => {
      service.removePerson.mockReturnValue(throwError(() => new ApiRequestError(404, [{ code: ERROR_CODES.personNotFound, message: 'The person is not in the room.' }])));

      expect(await store.removePerson('blue-fox', 'p-bob')).toBe(true);
    });

    it('clears an earlier error when the next action starts', async () => {
      service.closeRoom.mockReturnValueOnce(throwError(() => new Error('boom')));
      await store.closeRoom('blue-fox');
      expect(store.actionError()).toBe('Something went wrong.');

      await store.closeRoom('blue-fox');

      expect(store.actionError()).toBeNull();
    });
  });
});
