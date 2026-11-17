import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideRouter } from '@angular/router';
import { NEVER, of, throwError } from 'rxjs';
import { AdminOverviewService } from '../../core/admin-overview.service';
import { AdminSessionService } from '../../core/admin-session.service';
import { ApiRequestError } from '../../core/api-request-error';
import { AdminOverview } from '../../models/admin-overview';
import { OVERVIEW, VOTING_ROOM } from '../../testing/admin-overview.testing';
import { AdminConfirmDialog } from './admin-confirm-dialog';
import { POLL_INTERVAL_MS } from './admin-dashboard.store';
import { AdminHome } from './admin-home';

describe('AdminHome', () => {
  const session = { username: () => 'operator', signOut: vi.fn() };
  const overview = { get: vi.fn(), closeRoom: vi.fn(), removePerson: vi.fn() };
  const dialog = { open: vi.fn() };

  beforeEach(() => {
    session.signOut.mockReset().mockResolvedValue(undefined);
    overview.get.mockReset().mockReturnValue(of(OVERVIEW));
    overview.closeRoom.mockReset().mockReturnValue(of(null));
    overview.removePerson.mockReset().mockReturnValue(of(null));
    dialog.open.mockReset().mockReturnValue({ afterClosed: () => of(true) });
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AdminSessionService, useValue: session },
        { provide: AdminOverviewService, useValue: overview },
        { provide: MatDialog, useValue: dialog },
      ],
    });
  });

  async function render(): Promise<{ fixture: ComponentFixture<AdminHome>; element: HTMLElement }> {
    const fixture = TestBed.createComponent(AdminHome);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  async function settle(fixture: ComponentFixture<unknown>): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve));
    await fixture.whenStable();
    fixture.detectChanges();
  }

  const text = (element: HTMLElement) => (element.textContent ?? '').replace(/\s+/g, ' ');
  const clickIn = (element: HTMLElement, selector: string) => element.querySelector<HTMLButtonElement>(selector)!.click();

  it('shows who is signed in', async () => {
    const { element } = await render();

    expect(text(element)).toContain('Signed in as operator');
  });

  it('says it is loading until the first overview arrives', () => {
    overview.get.mockReturnValue(NEVER);
    const fixture = TestBed.createComponent(AdminHome);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Loading');
  });

  /** The tiles as label to value: the value is the bold part, the label the plain text next to it. */
  function tiles(element: HTMLElement): Record<string, string> {
    return Object.fromEntries(
      Array.from(element.querySelectorAll('.tile')).map((tile) => {
        const value = tile.querySelector('b')!.textContent!.trim();
        const label = Array.from(tile.childNodes)
          .filter((node) => node.nodeType === Node.TEXT_NODE)
          .map((node) => node.textContent!.trim())
          .join(' ')
          .trim();
        return [label, value];
      }),
    );
  }

  it('shows the stat tiles', async () => {
    const { element } = await render();

    expect(tiles(element)).toMatchObject({
      Rooms: '2',
      Online: '2',
      Offgrid: '2',
      Uptime: '3 d 4 h',
      'Restored at start': '1',
      'Swept (24 h)': '6',
      'Failed sign-ins (24 h)': '3',
    });
    expect(text(element)).toContain('1 voting · 0 revealed · 1 restored');
    expect(Object.keys(tiles(element))).toContain('Last store write');
    expect(text(element)).toContain('saved');
  });

  it('flags a failed store write', async () => {
    const failing: AdminOverview = {
      ...OVERVIEW,
      stats: { ...OVERVIEW.stats, lastStoreWrite: { at: '2026-03-01T10:44:00+00:00', succeeded: false, failedRooms: 2 } },
    };
    overview.get.mockReturnValue(of(failing));

    const { element } = await render();

    expect(element.querySelector('.tile.store.warn')).not.toBeNull();
    expect(text(element)).toContain('2 failed');
  });

  it('says so when nothing has been written yet', async () => {
    const fresh: AdminOverview = {
      ...OVERVIEW,
      stats: { ...OVERVIEW.stats, lastStoreWrite: { at: null, succeeded: null, failedRooms: 0 } },
    };
    overview.get.mockReturnValue(of(fresh));

    const { element } = await render();

    expect(tiles(element)['Last store write']).toBe('none yet');
  });

  it('lists each room as a card with its state and people', async () => {
    const { element } = await render();
    const cards = Array.from(element.querySelectorAll('.card'));

    expect(cards).toHaveLength(2);
    expect(cards[0].textContent).toContain('blue-fox');
    expect(cards[0].querySelector('.state')?.textContent).toBe('voting');
    expect(cards[1].querySelector('.state')?.classList).toContain('restored');
    expect(cards[0].querySelectorAll('.chips li')).toHaveLength(3);
  });

  it('marks the owner and shows roles', async () => {
    const { element } = await render();
    const chips = Array.from(element.querySelectorAll('.card')[0].querySelectorAll('.chips li'));

    expect(chips[0].textContent).toContain('Developer, owner');
    expect(chips[1].textContent).toContain('Tester');
    expect(chips[1].textContent).not.toContain('owner');
    expect(chips[2].textContent).toContain('SRE');
  });

  it('styles an offgrid person differently and says since when', async () => {
    const { element } = await render();
    const chips = Array.from(element.querySelectorAll('.card')[0].querySelectorAll('.chips li'));

    expect(chips[0].classList).not.toContain('offgrid');
    expect(chips[2].classList).toContain('offgrid');
    expect(chips[2].textContent).toContain('offgrid since');
  });

  it('lists the issues with an errors count', async () => {
    const { element } = await render();
    const feed = element.querySelector('.feed')!;

    expect(feed.querySelector('h2')?.textContent).toContain('1 error');
    expect(feed.querySelectorAll('.item')).toHaveLength(3);
    expect(feed.querySelector('.item.error')?.textContent).toContain('Persisting room failed');
    expect(feed.querySelector('.item.warning')?.textContent).toContain('Swept 2 rooms');
  });

  it('says so when there are no rooms or issues', async () => {
    overview.get.mockReturnValue(of({ ...OVERVIEW, rooms: [], issues: [] }));

    const { element } = await render();

    expect(text(element)).toContain('No live rooms.');
    expect(text(element)).toContain('Nothing logged.');
  });

  it('never shows anything about votes', async () => {
    const { element } = await render();

    expect(text(element).toLowerCase()).not.toContain('vote');
  });

  describe('closing a room', () => {
    it('asks first and says what will happen', async () => {
      const { element } = await render();

      clickIn(element, '.card .head button');

      expect(dialog.open).toHaveBeenCalledWith(AdminConfirmDialog, {
        data: {
          title: 'Close "blue-fox"?',
          detail: '3 people will be told the room was closed by an operator. The room is deleted and cannot be restored.',
          confirmLabel: 'Close room',
        },
      });
    });

    it('closes the room and refreshes once confirmed', async () => {
      const { element } = await render();
      overview.get.mockClear();

      clickIn(element, '.card .head button');

      await vi.waitFor(() => expect(overview.closeRoom).toHaveBeenCalledWith('blue-fox'));
      await vi.waitFor(() => expect(overview.get).toHaveBeenCalled());
    });

    it('does nothing when cancelled', async () => {
      dialog.open.mockReturnValue({ afterClosed: () => of(false) });
      const { element } = await render();

      clickIn(element, '.card .head button');
      await new Promise((resolve) => setTimeout(resolve));

      expect(overview.closeRoom).not.toHaveBeenCalled();
    });

    it('does nothing when the dialog is dismissed without an answer', async () => {
      dialog.open.mockReturnValue({ afterClosed: () => of(undefined) });
      const { element } = await render();

      clickIn(element, '.card .head button');
      await new Promise((resolve) => setTimeout(resolve));

      expect(overview.closeRoom).not.toHaveBeenCalled();
    });

    it('says "1 person" for a room with one person', async () => {
      overview.get.mockReturnValue(of({ ...OVERVIEW, rooms: [{ ...VOTING_ROOM, people: [VOTING_ROOM.people[0]] }] }));
      const { element } = await render();

      clickIn(element, '.card .head button');

      expect(dialog.open.mock.calls[0][1].data.detail).toContain('1 person will be told');
    });

    it('shows the server message when closing fails', async () => {
      overview.closeRoom.mockReturnValue(throwError(() => new ApiRequestError(500, [{ code: 'internal_error', message: 'An unexpected error occurred.' }])));
      const { fixture, element } = await render();

      clickIn(element, '.card .head button');
      await vi.waitFor(() => expect(overview.closeRoom).toHaveBeenCalled());
      await settle(fixture);

      expect(element.querySelector('.banner.error')?.textContent).toContain('An unexpected error occurred.');
    });
  });

  describe('removing a person', () => {
    it('asks first and says what will happen', async () => {
      const { element } = await render();

      clickIn(element, 'button[aria-label="Remove Bob"]');

      expect(dialog.open).toHaveBeenCalledWith(AdminConfirmDialog, {
        data: {
          title: 'Remove Bob?',
          detail: 'Bob leaves "blue-fox" and loses their vote. They can join again as a new person.',
          confirmLabel: 'Remove',
        },
      });
    });

    it('removes the person once confirmed', async () => {
      const { element } = await render();

      clickIn(element, 'button[aria-label="Remove Bob"]');

      await vi.waitFor(() => expect(overview.removePerson).toHaveBeenCalledWith('blue-fox', 'p-bob'));
    });

    it('can remove an offgrid person of a restored room', async () => {
      const { element } = await render();

      clickIn(element, 'button[aria-label="Remove Dan"]');

      await vi.waitFor(() => expect(overview.removePerson).toHaveBeenCalledWith('old-owl', 'p-dan'));
    });

    it('does nothing when cancelled', async () => {
      dialog.open.mockReturnValue({ afterClosed: () => of(false) });
      const { element } = await render();

      clickIn(element, 'button[aria-label="Remove Bob"]');
      await new Promise((resolve) => setTimeout(resolve));

      expect(overview.removePerson).not.toHaveBeenCalled();
    });
  });

  it('says when it could not refresh, and keeps showing what it has', async () => {
    vi.useFakeTimers();
    try {
      const { fixture, element } = await render();
      overview.get.mockReturnValue(throwError(() => new Error('offline')));

      vi.advanceTimersByTime(POLL_INTERVAL_MS);
      fixture.detectChanges();

      expect(text(element)).toContain('Could not refresh.');
      expect(element.querySelectorAll('.card')).toHaveLength(2);
    } finally {
      vi.useRealTimers();
    }
  });

  it('signs out from the button', async () => {
    const { element } = await render();

    clickIn(element, 'header button');

    await vi.waitFor(() => expect(session.signOut).toHaveBeenCalledOnce());
  });

  it('stops polling when the page is left', async () => {
    vi.useFakeTimers();
    try {
      const { fixture } = await render();
      const calls = overview.get.mock.calls.length;

      fixture.destroy();
      vi.advanceTimersByTime(POLL_INTERVAL_MS * 3);

      expect(overview.get.mock.calls.length).toBe(calls);
    } finally {
      vi.useRealTimers();
    }
  });
});
