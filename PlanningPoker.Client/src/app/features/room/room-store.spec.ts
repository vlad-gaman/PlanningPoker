import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { ClosingBeacon } from '../../core/closing-beacon';
import { FlashMessageService } from '../../core/flash-message.service';
import { MeService } from '../../core/me.service';
import { PersonDefaultsService } from '../../core/person-defaults.service';
import { RoomHubClient } from '../../core/room-hub.client';
import { RoomTabLock } from '../../core/room-tab-lock';
import { RoomsService } from '../../core/rooms.service';
import { ApiRequestError } from '../../core/api-request-error';
import { RESULTS_BY_ROLE_STORAGE_KEY } from '../../core/results-view.service';
import { ERROR_CODES } from '../../models/api';
import { HUB_EVENTS } from '../../models/hub-events';
import {
  ANN,
  BOB,
  CONSENSUS_STATISTICS,
  JOIN_RESPONSE,
  ROUND_STARTED_AT,
  STATISTICS,
  fakeHub,
  revealed,
} from '../../testing/fake-hub.testing';
import {
  REMOVED_BY_OPERATOR_MESSAGE,
  ROOM_CLOSED_BY_OPERATOR_MESSAGE,
  ROOM_DISPOSED_MESSAGE,
  ROOM_OPENED_ELSEWHERE_MESSAGE,
  ROOM_NOT_FOUND_MESSAGE,
  RoomStore,
  isRoomGone,
} from './room-store';

const CLEARED_AT = '2026-01-01T12:10:00+00:00';

/** Stand-ins for the services the store enters a room with; a spec sets what `me.get` and `rooms.exists` answer. */
function enterServices() {
  const me = { get: vi.fn(), update: vi.fn() };
  const rooms = { exists: vi.fn() };
  const personDefaults = { load: vi.fn(), save: vi.fn() };
  return {
    me,
    rooms,
    personDefaults,
    providers: [
      { provide: MeService, useValue: me },
      { provide: RoomsService, useValue: rooms },
      { provide: PersonDefaultsService, useValue: personDefaults },
    ],
  };
}

describe('RoomStore', () => {
  let hub: ReturnType<typeof fakeHub>;
  let store: RoomStore;
  let navigate: ReturnType<typeof vi.spyOn>;
  let taken: (() => void) | null;
  let connectionIdOfClosingPage: (() => string | null) | null;
  const tabLock = { claim: vi.fn(), onTaken: vi.fn() };
  const stopWatchingPage = vi.fn();
  const closingBeacon = { watch: vi.fn() };
  const services = enterServices();

  beforeEach(async () => {
    hub = fakeHub(JOIN_RESPONSE);
    connectionIdOfClosingPage = null;
    stopWatchingPage.mockClear();
    closingBeacon.watch.mockReset().mockImplementation((connectionId: () => string | null) => {
      connectionIdOfClosingPage = connectionId;
      return stopWatchingPage;
    });
    services.me.get.mockReset().mockReturnValue(of(ANN));
    services.rooms.exists.mockReset().mockReturnValue(of({ id: 'room-1', exists: true }));
    taken = null;
    tabLock.claim.mockClear();
    tabLock.onTaken.mockImplementation((_roomId: string, callback: () => void) => {
      taken = callback;
      return () => {
        taken = null;
      };
    });
    TestBed.configureTestingModule({
      providers: [
        RoomStore,
        provideRouter([]),
        ...services.providers,
        { provide: RoomHubClient, useValue: hub },
        { provide: RoomTabLock, useValue: tabLock },
        { provide: ClosingBeacon, useValue: closingBeacon },
      ],
    });
    store = TestBed.inject(RoomStore);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    await store.enter('room-1');
  });

  it('connects, joins the room and applies the response', () => {
    expect(hub.connect).toHaveBeenCalledOnce();
    expect(hub.invoke).toHaveBeenCalledWith('JoinRoom', 'room-1');
    expect(store.people()).toEqual([ANN]);
    expect(store.ownerGuid()).toBe(ANN.guid);
    expect(store.configuration()?.cardSet).toBe('linear');
  });

  it('takes the round start from the join snapshot', () => {
    expect(store.roundStartedAt()).toBe(Date.parse(ROUND_STARTED_AT));
  });

  it('keeps the round start when a vote is cast, and restarts it on reveal and on clear', async () => {
    await store.vote('5');
    hub.emit(HUB_EVENTS.VoteStatusUpdate, { ...JOIN_RESPONSE.voteStatus, votes: { [ANN.guid]: 'hide' } });
    expect(store.roundStartedAt()).toBe(Date.parse(ROUND_STARTED_AT));

    hub.emit(HUB_EVENTS.VotesShown, revealed(STATISTICS));
    expect(store.roundStartedAt()).toBe(Date.parse('2026-01-01T12:05:00+00:00'));

    hub.emit(HUB_EVENTS.VotesCleared, { roundStartedAt: CLEARED_AT });
    expect(store.roundStartedAt()).toBe(Date.parse(CLEARED_AT));
  });

  it('has no round start when the stamp is missing or unreadable', () => {
    hub.emit(HUB_EVENTS.VoteStatusUpdate, { ...JOIN_RESPONSE.voteStatus, roundStartedAt: 'nonsense' });

    expect(store.roundStartedAt()).toBeNull();
  });

  it('forgets the round start on close', async () => {
    await store.close();

    expect(store.roundStartedAt()).toBeNull();
  });

  it('shows the pick of a person who already voted in another tab', async () => {
    const second = TestBed.inject(RoomStore);
    hub.invoke.mockResolvedValueOnce({ ...JOIN_RESPONSE, myMark: '5' });

    await second.enter('room-1');

    expect(second.myMark()).toBe('5');
  });

  it('claims the room for this tab once joined', () => {
    expect(tabLock.claim).toHaveBeenCalledWith('room-1');
    expect(tabLock.onTaken).toHaveBeenCalledWith('room-1', expect.any(Function));
  });

  it('tells the server with the hub connection id when the page closes, once joined', () => {
    expect(closingBeacon.watch).toHaveBeenCalledOnce();
    expect(connectionIdOfClosingPage!()).toBe('connection-1');

    hub.connectionId = 'connection-2';

    expect(connectionIdOfClosingPage!()).toBe('connection-2');
  });

  it('stops watching the page once the room is closed', async () => {
    await store.close();

    expect(stopWatchingPage).toHaveBeenCalledOnce();
  });

  it('leaves for the home page when another tab takes the room', async () => {
    taken!();

    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith('/'));
    expect(hub.disconnect).toHaveBeenCalled();
    expect(TestBed.inject(FlashMessageService).message()).toBe(ROOM_OPENED_ELSEWHERE_MESSAGE);
  });

  it('stops listening for other tabs after the room is closed', async () => {
    await store.close();

    expect(taken).toBeNull();
  });

  it('adds a person who joined, together with a masked vote', () => {
    hub.emit(HUB_EVENTS.PersonJoined, { person: BOB, vote: 'hide', personType: 'test' });

    expect(store.people()).toEqual([ANN, BOB]);
    expect(store.votes()).toEqual({ [BOB.guid]: 'hide' });
  });

  it('does not duplicate a person who joins twice', () => {
    hub.emit(HUB_EVENTS.PersonJoined, { person: BOB, vote: null, personType: 'test' });
    hub.emit(HUB_EVENTS.PersonJoined, { person: BOB, vote: null, personType: 'test' });

    expect(store.people()).toEqual([ANN, BOB]);
    expect(store.votes()).toEqual({});
  });

  it('removes a person who exited together with their vote', () => {
    hub.emit(HUB_EVENTS.PersonJoined, { person: BOB, vote: 'hide', personType: 'test' });

    hub.emit(HUB_EVENTS.PersonExited, { person: BOB });

    expect(store.people()).toEqual([ANN]);
    expect(store.votes()).toEqual({});
  });

  it('marks a person who went offgrid and keeps their place and vote', () => {
    hub.emit(HUB_EVENTS.PersonJoined, { person: BOB, vote: 'hide', personType: 'test' });

    hub.emit(HUB_EVENTS.PersonOffgrid, { person: { ...BOB, isOffgrid: true } });

    expect(store.people()).toEqual([ANN, { ...BOB, isOffgrid: true }]);
    expect(store.votes()).toEqual({ [BOB.guid]: 'hide' });
  });

  it('shows a person who came back as online again', () => {
    hub.emit(HUB_EVENTS.PersonJoined, { person: BOB, vote: null, personType: 'test' });
    hub.emit(HUB_EVENTS.PersonOffgrid, { person: { ...BOB, isOffgrid: true } });

    hub.emit(HUB_EVENTS.PersonBack, { person: { ...BOB, isOffgrid: false } });

    expect(store.people()).toEqual([ANN, { ...BOB, isOffgrid: false }]);
  });

  it('ignores an offgrid event for a person the room does not list', () => {
    hub.emit(HUB_EVENTS.PersonOffgrid, { person: { ...BOB, isOffgrid: true } });

    expect(store.people()).toEqual([ANN]);
  });

  it('follows ownership transfers', () => {
    hub.emit(HUB_EVENTS.OwnershipTransferred, { newOwner: BOB });

    expect(store.ownerGuid()).toBe(BOB.guid);
  });

  it('is disconnected while the hub reconnects, and connected again after, and when it is closed on purpose', async () => {
    expect(store.connected()).toBe(true);

    hub.dropConnection();
    expect(store.connected()).toBe(false);

    hub.reconnect();
    expect(store.connected()).toBe(true);

    hub.closeConnection();
    expect(store.connected()).toBe(false);

    await store.close();
    expect(store.connected()).toBe(true);
  });

  it('joins again after the hub reconnects', async () => {
    hub.invoke.mockResolvedValue({ ...JOIN_RESPONSE, people: [ANN, BOB] });

    hub.reconnect();
    await vi.waitFor(() => expect(store.people()).toEqual([ANN, BOB]));

    expect(hub.invoke).toHaveBeenCalledTimes(2);
  });

  it('leaves with a message when rejoining after a reconnect says the room is gone', async () => {
    hub.invoke.mockRejectedValue(new ApiRequestError(0, [{ code: 'room_not_found', message: 'Gone.' }]));

    hub.reconnect();

    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith('/'));
    expect(TestBed.inject(FlashMessageService).message()).toBe(ROOM_NOT_FOUND_MESSAGE);
    expect(hub.disconnect).toHaveBeenCalledOnce();
  });

  it('leaves with the server message when rejoining after a reconnect fails otherwise', async () => {
    hub.invoke.mockRejectedValue(new ApiRequestError(0, [{ code: 'internal_error', message: 'Server down.' }]));

    hub.reconnect();

    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith('/'));
    expect(TestBed.inject(FlashMessageService).message()).toBe('Server down.');
    expect(hub.disconnect).toHaveBeenCalledOnce();
  });

  it('shows a generic message when rejoining after a reconnect fails unexpectedly', async () => {
    hub.invoke.mockRejectedValue(new Error('socket reset'));

    hub.reconnect();

    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith('/'));
    expect(TestBed.inject(FlashMessageService).message()).toBe('Something went wrong.');
  });

  it('goes home with a message when the room is disposed', async () => {
    hub.emit(HUB_EVENTS.RoomDisposed, { roomId: 'room-1' });

    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith('/'));
    expect(TestBed.inject(FlashMessageService).message()).toBe(ROOM_DISPOSED_MESSAGE);
    expect(hub.disconnect).toHaveBeenCalledOnce();
    expect(store.people()).toEqual([]);
  });

  it('goes home with the operator message when an operator closed the room', async () => {
    hub.emit(HUB_EVENTS.RoomDisposed, { roomId: 'room-1', reason: 'closedByOperator' });

    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith('/'));
    expect(TestBed.inject(FlashMessageService).message()).toBe(ROOM_CLOSED_BY_OPERATOR_MESSAGE);
    expect(hub.disconnect).toHaveBeenCalledOnce();
  });

  it('keeps the ordinary message when the disposal has a null reason', async () => {
    hub.emit(HUB_EVENTS.RoomDisposed, { roomId: 'room-1', reason: null });

    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith('/'));
    expect(TestBed.inject(FlashMessageService).message()).toBe(ROOM_DISPOSED_MESSAGE);
  });

  it('ignores an operator closure of another room', () => {
    hub.emit(HUB_EVENTS.RoomDisposed, { roomId: 'other', reason: 'closedByOperator' });

    expect(navigate).not.toHaveBeenCalled();
    expect(TestBed.inject(FlashMessageService).message()).toBeNull();
  });

  it('sends the removed person home with the operator message', async () => {
    services.me.get.mockReturnValue(of(ANN));
    await store.enter('room-1');

    hub.emit(HUB_EVENTS.PersonExited, { person: ANN, reason: 'removedByOperator' });

    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith('/'));
    expect(TestBed.inject(FlashMessageService).message()).toBe(REMOVED_BY_OPERATOR_MESSAGE);
    expect(hub.disconnect).toHaveBeenCalled();
  });

  it('shows another person an ordinary exit when the operator removed someone else', async () => {
    services.me.get.mockReturnValue(of(ANN));
    await store.enter('room-1');
    hub.emit(HUB_EVENTS.PersonJoined, { person: BOB, vote: null, personType: 'test' });
    expect(store.people().map((person) => person.guid)).toContain(BOB.guid);

    hub.emit(HUB_EVENTS.PersonExited, { person: BOB, reason: 'removedByOperator' });

    expect(store.people().map((person) => person.guid)).not.toContain(BOB.guid);
    expect(navigate).not.toHaveBeenCalled();
    expect(TestBed.inject(FlashMessageService).message()).toBeNull();
  });

  it('shows nothing about an operator when an ordinary exit has no reason', async () => {
    services.me.get.mockReturnValue(of(ANN));
    await store.enter('room-1');
    hub.emit(HUB_EVENTS.PersonJoined, { person: BOB, vote: null, personType: 'test' });

    hub.emit(HUB_EVENTS.PersonExited, { person: BOB });
    hub.emit(HUB_EVENTS.PersonExited, { person: BOB, reason: null });

    expect(navigate).not.toHaveBeenCalled();
    expect(TestBed.inject(FlashMessageService).message()).toBeNull();
  });

  describe('round history', () => {
    const FIRST = { round: 1, averageMark: 3, revealedAt: '2026-01-01T12:05:00+00:00' };
    const SECOND = { round: 2, averageMark: null, revealedAt: '2026-01-01T12:20:00+00:00' };

    it('starts with the history from the join snapshot', async () => {
      hub.invoke.mockResolvedValueOnce({ ...JOIN_RESPONSE, roundHistory: [SECOND, FIRST] });
      const second = TestBed.inject(RoomStore);

      await second.enter('room-1');

      expect(second.roundHistory()).toEqual([SECOND, FIRST]);
    });

    it('is empty for a room that has revealed nothing', () => {
      expect(store.roundHistory()).toEqual([]);
    });

    it('is empty when an older server sends no history', async () => {
      const withoutHistory: Partial<typeof JOIN_RESPONSE> = { ...JOIN_RESPONSE };
      delete withoutHistory.roundHistory;
      hub.invoke.mockResolvedValueOnce(withoutHistory);
      const second = TestBed.inject(RoomStore);

      await second.enter('room-1');

      expect(second.roundHistory()).toEqual([]);
    });

    it('is renewed, for everyone, by a reveal', () => {
      hub.emit(HUB_EVENTS.VotesShown, { ...revealed(STATISTICS), roundHistory: [FIRST] });

      expect(store.roundHistory()).toEqual([FIRST]);

      hub.emit(HUB_EVENTS.VotesCleared, { roundStartedAt: CLEARED_AT });
      hub.emit(HUB_EVENTS.VotesShown, { ...revealed(STATISTICS), roundHistory: [SECOND, FIRST] });

      expect(store.roundHistory()).toEqual([SECOND, FIRST]);
    });

    it('keeps the history when the votes are cleared or a vote status arrives without one', () => {
      hub.emit(HUB_EVENTS.VotesShown, { ...revealed(STATISTICS), roundHistory: [FIRST] });

      hub.emit(HUB_EVENTS.VotesCleared, { roundStartedAt: CLEARED_AT });
      hub.emit(HUB_EVENTS.VoteStatusUpdate, { ...JOIN_RESPONSE.voteStatus, votes: { [ANN.guid]: 'hide' } });

      expect(store.roundHistory()).toEqual([FIRST]);
    });

    it('keeps the history when a reveal arrives without one', () => {
      hub.emit(HUB_EVENTS.VotesShown, { ...revealed(STATISTICS), roundHistory: [FIRST] });

      hub.emit(HUB_EVENTS.VotesShown, revealed(STATISTICS));

      expect(store.roundHistory()).toEqual([FIRST]);
    });

    it('is forgotten on close', async () => {
      hub.emit(HUB_EVENTS.VotesShown, { ...revealed(STATISTICS), roundHistory: [FIRST] });

      await store.close();

      expect(store.roundHistory()).toEqual([]);
    });
  });

  it('ignores a disposal of another room', () => {
    hub.emit(HUB_EVENTS.RoomDisposed, { roomId: 'other' });

    expect(navigate).not.toHaveBeenCalled();
    expect(store.people()).toEqual([ANN]);
  });

  it('leaveRoom tells the server to remove the person before it disconnects and navigates home', async () => {
    await store.leaveRoom();

    expect(hub.invoke).toHaveBeenLastCalledWith('LeaveRoom');
    expect(hub.disconnect).toHaveBeenCalledOnce();
    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('leaveRoom still leaves when the server refuses', async () => {
    hub.invoke.mockRejectedValueOnce(new ApiRequestError(0, [{ code: 'not_in_room', message: 'Not in the room.' }]));

    await store.leaveRoom();

    expect(hub.disconnect).toHaveBeenCalledOnce();
    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('leave disconnects, clears the state and navigates home', async () => {
    await store.leave();

    expect(hub.disconnect).toHaveBeenCalledOnce();
    expect(navigate).toHaveBeenCalledWith('/');
    expect(store.people()).toEqual([]);
    expect(store.ownerGuid()).toBeNull();
  });

  it('aborts a join quietly when the store is closed while connecting', async () => {
    let finishConnect!: () => void;
    hub.connect.mockReturnValueOnce(new Promise<void>((resolve) => (finishConnect = resolve)));
    hub.invoke.mockClear();
    hub.on.mockClear();

    const joining = store.enter('room-2');
    await store.close();
    finishConnect();
    await joining;

    expect(hub.invoke).not.toHaveBeenCalled();
    expect(hub.on).not.toHaveBeenCalled();
    expect(TestBed.inject(FlashMessageService).message()).toBeNull();
    expect(store.people()).toEqual([]);
  });

  it('joins normally again after a closed join was restarted', async () => {
    await store.close();

    await store.enter('room-1');

    expect(store.people()).toEqual([ANN]);
    expect(hub.on).toHaveBeenCalledTimes(28);
  });

  /** Starts a join whose JoinRoom response is held until respond() is called. */
  async function joinWithHeldResponse(snapshot: unknown) {
    let respond!: () => void;
    hub.invoke.mockImplementationOnce(() => new Promise((resolve) => (respond = () => resolve(snapshot))));
    const joining = store.enter('room-2');
    await vi.waitFor(() => expect(hub.invoke).toHaveBeenCalledTimes(2));
    return { joining, respond };
  }

  it('keeps a person who joined while the join was still in flight', async () => {
    const { joining, respond } = await joinWithHeldResponse(JOIN_RESPONSE);

    hub.emit(HUB_EVENTS.PersonJoined, { person: BOB, vote: null, personType: 'test' });
    respond();
    await joining;

    expect(store.people()).toEqual([ANN, BOB]);
  });

  it('replays events received during the join in arrival order', async () => {
    const { joining, respond } = await joinWithHeldResponse({
      ...JOIN_RESPONSE,
      people: [ANN, BOB],
    });

    hub.emit(HUB_EVENTS.PersonExited, { person: BOB });
    hub.emit(HUB_EVENTS.PersonJoined, { person: BOB, vote: 'hide', personType: 'test' });
    respond();
    await joining;

    expect(store.people()).toEqual([ANN, BOB]);
    expect(store.votes()).toEqual({ [BOB.guid]: 'hide' });
  });

  it('does not duplicate a person who is in the snapshot and also joined during the join', async () => {
    const { joining, respond } = await joinWithHeldResponse({
      ...JOIN_RESPONSE,
      people: [ANN, BOB],
    });

    hub.emit(HUB_EVENTS.PersonJoined, { person: BOB, vote: null, personType: 'test' });
    respond();
    await joining;

    expect(store.people()).toEqual([ANN, BOB]);
  });

  it('applies an ownership transfer received during the join', async () => {
    const { joining, respond } = await joinWithHeldResponse({
      ...JOIN_RESPONSE,
      people: [ANN, BOB],
    });

    hub.emit(HUB_EVENTS.OwnershipTransferred, { newOwner: BOB });
    respond();
    await joining;

    expect(store.ownerGuid()).toBe(BOB.guid);
  });

  it('applies nothing after a disposal received during the join', async () => {
    const { joining, respond } = await joinWithHeldResponse(JOIN_RESPONSE);

    hub.emit(HUB_EVENTS.RoomDisposed, { roomId: 'room-2' });
    hub.emit(HUB_EVENTS.PersonJoined, { person: BOB, vote: null, personType: 'test' });
    respond();
    await joining;

    expect(store.people()).toEqual([]);
    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('does not run a join that was superseded while it was connecting', async () => {
    let finishConnect!: () => void;
    hub.connect.mockReturnValueOnce(new Promise<void>((resolve) => (finishConnect = resolve)));
    hub.invoke.mockClear();

    const superseded = store.enter('room-2');
    await vi.waitFor(() => expect(hub.connect).toHaveBeenCalledTimes(2));
    await store.enter('room-3');
    finishConnect();
    await superseded;

    expect(hub.invoke).toHaveBeenCalledOnce();
    expect(hub.invoke).toHaveBeenCalledWith('JoinRoom', 'room-3');
  });

  it('does not apply the snapshot of a join that was superseded while its JoinRoom was in flight', async () => {
    let respond!: (response: unknown) => void;
    hub.invoke.mockImplementationOnce(() => new Promise((resolve) => (respond = resolve)));

    const superseded = store.enter('room-2');
    await vi.waitFor(() => expect(hub.invoke).toHaveBeenCalledTimes(2));
    await store.enter('room-3');
    respond({ ...JOIN_RESPONSE, people: [ANN, BOB], ownerGuid: BOB.guid });
    await superseded;

    expect(store.people()).toEqual([ANN]);
    expect(store.ownerGuid()).toBe(ANN.guid);
  });

  describe('voting', () => {
    const hidden = {
      votes: { [BOB.guid]: 'hide' },
      hasEveryoneVoted: false,
      votingFinished: false,
      countdown: null,
      statistics: null,
    };

    it('exposes the cards and an open, empty vote state from the join response', () => {
      expect(store.cards().map((card) => card.value)).toEqual(['1', '5', 'coffee']);
      expect(store.votingFinished()).toBe(false);
      expect(store.countdown()).toBeNull();
      expect(store.hasEveryoneVoted()).toBe(false);
      expect(store.myMark()).toBeNull();
    });

    it('applies a revealed snapshot with a running countdown', async () => {
      hub.invoke.mockResolvedValue({
        ...JOIN_RESPONSE,
        voteStatus: {
          votes: { [ANN.guid]: '5' },
          hasEveryoneVoted: true,
          votingFinished: false,
          countdown: 3,
          statistics: null,
        },
      });

      await store.enter('room-2');

      expect(store.votes()).toEqual({ [ANN.guid]: '5' });
      expect(store.hasEveryoneVoted()).toBe(true);
      expect(store.countdown()).toBe(3);
    });

    it('applies a snapshot of revealed votes', async () => {
      hub.invoke.mockResolvedValue({
        ...JOIN_RESPONSE,
        voteStatus: {
          votes: { [ANN.guid]: '5' },
          hasEveryoneVoted: true,
          votingFinished: true,
          countdown: null,
          statistics: null,
        },
      });

      await store.enter('room-2');

      expect(store.votingFinished()).toBe(true);
    });

    it('adds a hidden entry when somebody votes', () => {
      hub.emit(HUB_EVENTS.VoteCast, { personGuid: BOB.guid });

      expect(store.votes()).toEqual({ [BOB.guid]: 'hide' });
    });

    it('does not hide a mark that was already revealed when a late VoteCast arrives', () => {
      hub.emit(HUB_EVENTS.VotesShown, {
        votes: { [BOB.guid]: '5' },
        hasEveryoneVoted: true,
        votingFinished: true,
        countdown: null,
        statistics: null,
      });

      hub.emit(HUB_EVENTS.VoteCast, { personGuid: BOB.guid });

      expect(store.votes()).toEqual({ [BOB.guid]: '5' });
    });

    it('replaces the vote state on VoteStatusUpdate', () => {
      hub.emit(HUB_EVENTS.VoteStatusUpdate, { ...hidden, hasEveryoneVoted: true });

      expect(store.votes()).toEqual(hidden.votes);
      expect(store.hasEveryoneVoted()).toBe(true);
    });

    it('shows the real marks and closes voting on VotesShown, ending the countdown', () => {
      hub.emit(HUB_EVENTS.CountdownUpdate, { countdown: 1, reset: false });

      hub.emit(HUB_EVENTS.VotesShown, {
        votes: { [BOB.guid]: '8' },
        hasEveryoneVoted: true,
        votingFinished: true,
        countdown: null,
        statistics: null,
      });

      expect(store.votes()).toEqual({ [BOB.guid]: '8' });
      expect(store.votingFinished()).toBe(true);
      expect(store.countdown()).toBeNull();
    });

    it('follows CountdownUpdate and clears the countdown on CountdownReset', () => {
      hub.emit(HUB_EVENTS.CountdownUpdate, { countdown: 4, reset: false });
      expect(store.countdown()).toBe(4);

      hub.emit(HUB_EVENTS.CountdownReset, { countdown: 5, reset: true });

      expect(store.countdown()).toBeNull();
    });

    it('ignores a CountdownUpdate once the votes are revealed', () => {
      hub.emit(HUB_EVENTS.VotesShown, {
        votes: {},
        hasEveryoneVoted: true,
        votingFinished: true,
        countdown: null,
        statistics: null,
      });

      hub.emit(HUB_EVENTS.CountdownUpdate, { countdown: 1, reset: false });

      expect(store.countdown()).toBeNull();
    });

    it('clears votes, countdown, selection and reopens voting on VotesCleared', async () => {
      await store.vote('5');
      hub.emit(HUB_EVENTS.VotesShown, {
        votes: { [ANN.guid]: '5' },
        hasEveryoneVoted: true,
        votingFinished: true,
        countdown: null,
        statistics: null,
      });

      hub.emit(HUB_EVENTS.VotesCleared, { roundStartedAt: CLEARED_AT });

      expect(store.votes()).toEqual({});
      expect(store.votingFinished()).toBe(false);
      expect(store.hasEveryoneVoted()).toBe(false);
      expect(store.myMark()).toBeNull();
    });

    it('switches the by-role view off for every room when a new round starts', async () => {
      localStorage.setItem(RESULTS_BY_ROLE_STORAGE_KEY, 'on');

      hub.emit(HUB_EVENTS.VotesCleared, { roundStartedAt: CLEARED_AT });

      expect(localStorage.getItem(RESULTS_BY_ROLE_STORAGE_KEY)).toBe('off');
    });

    it('replays voting events received during the join after the snapshot', async () => {
      let respond!: () => void;
      hub.invoke.mockImplementationOnce(() => new Promise((resolve) => (respond = () => resolve(JOIN_RESPONSE))));
      const joining = store.enter('room-2');
      await vi.waitFor(() => expect(hub.invoke).toHaveBeenCalledTimes(2));

      hub.emit(HUB_EVENTS.VoteCast, { personGuid: BOB.guid });
      hub.emit(HUB_EVENTS.CountdownUpdate, { countdown: 5, reset: false });
      respond();
      await joining;

      expect(store.votes()).toEqual({ [BOB.guid]: 'hide' });
      expect(store.countdown()).toBe(5);
    });

    it('vote sends the mark and selects it at once', async () => {
      const voting = store.vote('5');

      expect(store.myMark()).toBe('5');
      await voting;
      expect(hub.invoke).toHaveBeenLastCalledWith('Vote', '5');
    });

    it('vote restores the previous selection and shows the message when the hub refuses', async () => {
      await store.vote('1');
      hub.invoke.mockRejectedValueOnce(
        new ApiRequestError(0, [{ code: 'voting_closed', message: 'Voting is closed.' }]),
      );

      await store.vote('5');

      expect(store.myMark()).toBe('1');
      expect(TestBed.inject(FlashMessageService).message()).toBe('Voting is closed.');
    });

    it('vote leaves the state alone when the room was closed while it was in flight', async () => {
      let fail!: (error: unknown) => void;
      hub.invoke.mockImplementationOnce(() => new Promise((_, reject) => (fail = reject)));
      const voting = store.vote('5');

      await store.close();
      fail(new ApiRequestError(0, [{ code: 'voting_closed', message: 'Voting is closed.' }]));
      await voting;

      expect(store.myMark()).toBeNull();
      expect(TestBed.inject(FlashMessageService).message()).toBeNull();
    });

    it('clearVotes and showVotes invoke the hub', async () => {
      await store.clearVotes();
      expect(hub.invoke).toHaveBeenLastCalledWith('ClearVotes');

      await store.showVotes();
      expect(hub.invoke).toHaveBeenLastCalledWith('ForceShowVotes');
    });

    it('clearVotes and showVotes show the message when the hub refuses', async () => {
      hub.invoke.mockRejectedValueOnce(new ApiRequestError(0, [{ code: 'not_in_room', message: 'Not in the room.' }]));

      await store.showVotes();

      expect(TestBed.inject(FlashMessageService).message()).toBe('Not in the room.');
    });
  });

  describe('results', () => {
    it('has no statistics while voting is open', () => {
      expect(store.statistics()).toBeNull();
      expect(store.revealCount()).toBe(0);
    });

    it('takes the statistics of a live reveal and counts it', () => {
      hub.emit(HUB_EVENTS.VotesShown, revealed(STATISTICS));

      expect(store.statistics()).toEqual(STATISTICS);
      expect(store.revealCount()).toBe(1);
    });

    it('counts every live reveal', () => {
      hub.emit(HUB_EVENTS.VotesShown, revealed(STATISTICS));
      hub.emit(HUB_EVENTS.VotesCleared, { roundStartedAt: CLEARED_AT });
      hub.emit(HUB_EVENTS.VotesShown, revealed(CONSENSUS_STATISTICS));

      expect(store.revealCount()).toBe(2);
      expect(store.statistics()).toEqual(CONSENSUS_STATISTICS);
    });

    it('takes the statistics of the join snapshot without counting a reveal', async () => {
      hub.invoke.mockResolvedValue({
        ...JOIN_RESPONSE,
        voteStatus: revealed(CONSENSUS_STATISTICS),
      });

      await store.enter('room-2');

      expect(store.statistics()).toEqual(CONSENSUS_STATISTICS);
      expect(store.revealCount()).toBe(0);
    });

    it('keeps the statistics through a reconnect that rejoins a revealed room, without counting', async () => {
      hub.emit(HUB_EVENTS.VotesShown, revealed(STATISTICS));
      hub.invoke.mockResolvedValue({ ...JOIN_RESPONSE, voteStatus: revealed(STATISTICS) });

      hub.reconnect();
      await vi.waitFor(() => expect(hub.invoke).toHaveBeenCalledTimes(2));

      expect(store.statistics()).toEqual(STATISTICS);
      expect(store.revealCount()).toBe(1);
    });

    it('clears the statistics when the votes are cleared', () => {
      hub.emit(HUB_EVENTS.VotesShown, revealed(STATISTICS));

      hub.emit(HUB_EVENTS.VotesCleared, { roundStartedAt: CLEARED_AT });

      expect(store.statistics()).toBeNull();
    });

    it('has no statistics in an open-voting status update', () => {
      hub.emit(HUB_EVENTS.VotesShown, revealed(STATISTICS));

      hub.emit(HUB_EVENTS.VoteStatusUpdate, {
        votes: {},
        hasEveryoneVoted: false,
        votingFinished: false,
        countdown: null,
        statistics: null,
      });

      expect(store.statistics()).toBeNull();
    });

    it('counts a reveal that arrived while the join was in flight, after applying the snapshot', async () => {
      let finishJoin: (value: unknown) => void = () => undefined;
      hub.invoke.mockReturnValue(new Promise((resolve) => (finishJoin = resolve)));
      const joining = store.enter('room-2');
      await vi.waitFor(() => expect(hub.invoke).toHaveBeenCalledTimes(2));

      hub.emit(HUB_EVENTS.VotesShown, revealed(STATISTICS));
      expect(store.statistics()).toBeNull();
      finishJoin(JOIN_RESPONSE);
      await joining;

      expect(store.statistics()).toEqual(STATISTICS);
      expect(store.revealCount()).toBe(1);
    });

    it('forgets the statistics and the reveal count when the store closes', async () => {
      hub.emit(HUB_EVENTS.VotesShown, revealed(STATISTICS));

      await store.close();

      expect(store.statistics()).toBeNull();
      expect(store.revealCount()).toBe(0);
    });
  });

  it('ignores a reconnect after the store was closed', async () => {
    await store.close();
    hub.invoke.mockClear();

    hub.reconnect();

    expect(hub.invoke).not.toHaveBeenCalled();
    expect(navigate).not.toHaveBeenCalled();
  });
});

describe('RoomStore room management', () => {
  const management = enterServices();
  let hub: ReturnType<typeof fakeHub>;
  let store: RoomStore;
  const status = (votes: Record<string, string>, extra = {}) => ({
    votes,
    hasEveryoneVoted: false,
    votingFinished: false,
    countdown: null,
    statistics: null,
    ...extra,
  });

  beforeEach(async () => {
    hub = fakeHub(JOIN_RESPONSE);
    management.me.get.mockReset().mockReturnValue(of(ANN));
    management.rooms.exists.mockReset().mockReturnValue(of({ id: 'room-1', exists: true }));
    TestBed.configureTestingModule({
      providers: [
        RoomStore,
        provideRouter([]),
        ...management.providers,
        { provide: RoomHubClient, useValue: hub },
      ],
    });
    store = TestBed.inject(RoomStore);
    await store.enter('room-1');
  });

  it('replaces the person and the vote status when a person type changed', () => {
    hub.emit(HUB_EVENTS.PersonTypeChanged, {
      person: { ...ANN, personType: 'obs' },
      voteStatus: status({}, { hasEveryoneVoted: true, countdown: 3 }),
    });

    expect(store.people()[0].personType).toBe('obs');
    expect(store.hasEveryoneVoted()).toBe(true);
    expect(store.countdown()).toBe(3);
  });

  it('does not count a person type change as a reveal', () => {
    hub.emit(HUB_EVENTS.PersonTypeChanged, { person: ANN, voteStatus: revealed(STATISTICS) });

    expect(store.revealCount()).toBe(0);
  });

  it('replaces configuration and cards when the configuration is updated', () => {
    const cards = [{ value: 'S', display: null }];
    const configuration = { cardSet: 'tshirt', countdownSeconds: 9, showFireworks: false, maxParticipants: 4 };

    hub.emit(HUB_EVENTS.RoomConfigurationUpdated, { configuration, cards, updatedBy: 'Ann', voteStatus: status({}) });

    expect(store.configuration()).toEqual(configuration);
    expect(store.cards()).toEqual(cards);
  });

  it('drops the selected card when it is not in the new card set', async () => {
    await store.vote('5');

    hub.emit(HUB_EVENTS.RoomConfigurationUpdated, {
      configuration: JOIN_RESPONSE.configuration,
      cards: [{ value: 'S', display: null }],
      updatedBy: 'Ann',
      voteStatus: status({}),
    });

    expect(store.myMark()).toBeNull();
  });

  it('keeps the selected card when it is still in the card set', async () => {
    await store.vote('5');

    hub.emit(HUB_EVENTS.RoomConfigurationUpdated, {
      configuration: JOIN_RESPONSE.configuration,
      cards: JOIN_RESPONSE.cards,
      updatedBy: 'Ann',
      voteStatus: status({ [ANN.guid]: 'hide' }),
    });

    expect(store.myMark()).toBe('5');
  });

  it('sends the management commands to the hub', async () => {
    await store.changePersonType('obs');
    await store.updateConfiguration(JOIN_RESPONSE.configuration);
    await store.transferOwnership(BOB.guid);

    expect(hub.invoke).toHaveBeenCalledWith('ChangePersonType', 'obs');
    expect(hub.invoke).toHaveBeenCalledWith('UpdateRoomConfiguration', JOIN_RESPONSE.configuration);
    expect(hub.invoke).toHaveBeenCalledWith('TransferOwnership', BOB.guid);
  });

  it('flashes a refused role change', async () => {
    hub.invoke.mockRejectedValueOnce(new ApiRequestError(0, [{ code: 'x', message: 'nope', metadata: {} }]));

    expect(await store.changePersonType('obs')).toBe(false);
    expect(TestBed.inject(FlashMessageService).message()).toBe('nope');
  });

  it('lets a refused configuration update reach the caller', async () => {
    hub.invoke.mockRejectedValueOnce(new ApiRequestError(0, [{ code: 'not_room_owner', message: 'no', metadata: {} }]));

    await expect(store.updateConfiguration(JOIN_RESPONSE.configuration)).rejects.toBeInstanceOf(ApiRequestError);
  });
});

describe('isRoomGone', () => {
  it('is true for room_not_found', () => {
    expect(isRoomGone(new ApiRequestError(404, [{ code: 'room_not_found', message: 'Gone.' }]))).toBe(true);
  });

  it('is true for a malformed room id', () => {
    const error = new ApiRequestError(400, [
      {
        code: 'validation_failed',
        message: 'Bad id.',
        metadata: { field: 'roomId', reason: 'invalid_characters' },
      },
    ]);

    expect(isRoomGone(error)).toBe(true);
  });

  it('is false for other validation failures', () => {
    const error = new ApiRequestError(400, [
      { code: 'validation_failed', message: 'Bad name.', metadata: { field: 'name' } },
    ]);

    expect(isRoomGone(error)).toBe(false);
  });

  it('is false for other errors and plain errors', () => {
    expect(isRoomGone(new ApiRequestError(500, [{ code: 'internal_error', message: 'Boom.' }]))).toBe(false);
    expect(isRoomGone(new Error('Hub is not connected'))).toBe(false);
  });
});

describe('RoomStore entering a room', () => {
  const OBS = { guid: 'g-obs', name: 'Olga', personType: 'obs' } as const;
  const services = enterServices();
  let hub: ReturnType<typeof fakeHub>;
  let store: RoomStore;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    hub = fakeHub(JOIN_RESPONSE);
    services.me.get.mockReset().mockReturnValue(of(ANN));
    services.me.update.mockReset().mockReturnValue(of(ANN));
    services.rooms.exists.mockReset().mockReturnValue(of({ id: 'room-1', exists: true }));
    services.personDefaults.save.mockReset();
    TestBed.configureTestingModule({
      providers: [
        RoomStore,
        provideRouter([]),
        ...services.providers,
        { provide: RoomHubClient, useValue: hub },
        { provide: RoomTabLock, useValue: { claim: vi.fn(), onTaken: vi.fn().mockReturnValue(() => undefined) } },
      ],
    });
    store = TestBed.inject(RoomStore);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  });

  function noSession() {
    return throwError(() => new ApiRequestError(401, [{ code: ERROR_CODES.noSession, message: 'No session.' }]));
  }

  const flashed = () => TestBed.inject(FlashMessageService).message();

  describe('as someone with a name', () => {
    it('joins straight away', async () => {
      expect(store.phase()).toBe('loading');

      await store.enter('room-1');

      expect(hub.invoke).toHaveBeenCalledWith('JoinRoom', 'room-1');
      expect(store.phase()).toBe('joined');
      expect(store.me()).toBe(ANN.guid);
      expect(store.error()).toBeNull();
    });

    it('takes the words of a fun room name from the existence check', async () => {
      const nameWords = [{ text: 'Adoring', linkable: true }];
      services.rooms.exists.mockReturnValue(of({ id: '42_Adoring', exists: true, nameWords }));

      await store.enter('42_Adoring');

      expect(store.nameWords()).toEqual(nameWords);
    });

    it('has no name words for an ordinary room name', async () => {
      await store.enter('room-1');

      expect(store.nameWords()).toBeNull();
    });

    it('goes ahead when the existence check fails for another reason, as joining reports it', async () => {
      services.rooms.exists.mockReturnValue(throwError(() => new ApiRequestError(500, [{ code: 'x', message: 'x' }])));

      await store.enter('room-1');

      expect(store.phase()).toBe('joined');
    });
  });

  describe('as someone without a name', () => {
    beforeEach(() => services.me.get.mockReturnValue(noSession()));

    it('asks for a name and joins nothing yet', async () => {
      await store.enter('room-1');

      expect(store.phase()).toBe('name');
      expect(store.error()).toBeNull();
      expect(hub.invoke).not.toHaveBeenCalled();
    });

    it('saves the name, remembers it, and joins', async () => {
      await store.enter('room-1');

      await store.submitName({ name: 'Ann', personType: 'dev' });

      expect(services.me.update).toHaveBeenCalledWith({ name: 'Ann', personType: 'dev' });
      expect(services.personDefaults.save).toHaveBeenCalledWith({ name: 'Ann', personType: 'dev' });
      expect(hub.invoke).toHaveBeenCalledWith('JoinRoom', 'room-1');
      expect(store.me()).toBe(ANN.guid);
      expect(store.phase()).toBe('joined');
    });

    it('shows why the name was refused and stays on the name step', async () => {
      services.me.update.mockReturnValue(
        throwError(() => new ApiRequestError(400, [{ code: 'validation_failed', message: 'Bad name.' }])),
      );
      await store.enter('room-1');

      await store.submitName({ name: 'Ann', personType: 'dev' });

      expect(store.error()).toContain('Bad name.');
      expect(store.phase()).toBe('name');
      expect(store.me()).toBeNull();
      expect(hub.invoke).not.toHaveBeenCalled();
    });

    it('lets the visitor try again after a refused join', async () => {
      hub.invoke.mockRejectedValueOnce(new ApiRequestError(500, [{ code: 'internal_error', message: 'Server down.' }]));
      await store.enter('room-1');
      await store.submitName({ name: 'Ann', personType: 'dev' });
      expect(store.phase()).toBe('name');

      await store.submitName({ name: 'Ann', personType: 'dev' });

      expect(store.phase()).toBe('joined');
      expect(store.error()).toBeNull();
    });

    it('does not join when the visitor leaves while the name is being saved', async () => {
      const saving = new Subject<typeof ANN>();
      services.me.update.mockReturnValue(saving);
      await store.enter('room-1');
      const submitting = store.submitName({ name: 'Ann', personType: 'dev' });

      const leaving = store.leaveRoom();
      saving.next(ANN);
      saving.complete();
      await Promise.all([submitting, leaving]);

      expect(hub.invoke).not.toHaveBeenCalledWith('JoinRoom', 'room-1');
      expect(store.phase()).not.toBe('joined');
      expect(navigate).toHaveBeenCalledWith('/');
    });
  });

  describe('when the room is gone', () => {
    it('goes home with a message when joining says it does not exist', async () => {
      hub.invoke.mockRejectedValue(new ApiRequestError(0, [{ code: ERROR_CODES.roomNotFound, message: 'Gone.' }]));

      await store.enter('room-1');

      expect(navigate).toHaveBeenCalledWith('/');
      expect(flashed()).toBe(ROOM_NOT_FOUND_MESSAGE);
      expect(hub.disconnect).toHaveBeenCalled();
    });

    it('goes home with a message when the room id is malformed', async () => {
      hub.invoke.mockRejectedValue(
        new ApiRequestError(400, [
          { code: ERROR_CODES.validationFailed, message: 'Bad id.', metadata: { field: 'roomId' } },
        ]),
      );

      await store.enter('room-1');

      expect(navigate).toHaveBeenCalledWith('/');
      expect(flashed()).toBe(ROOM_NOT_FOUND_MESSAGE);
    });

    it('goes home before asking for a name when the existence check says so', async () => {
      services.me.get.mockReturnValue(noSession());
      services.rooms.exists.mockReturnValue(of({ id: 'room-1', exists: false }));

      await store.enter('room-1');

      expect(navigate).toHaveBeenCalledWith('/');
      expect(flashed()).toBe(ROOM_NOT_FOUND_MESSAGE);
      expect(store.phase()).toBe('loading');
      expect(services.me.get).not.toHaveBeenCalled();
      expect(hub.invoke).not.toHaveBeenCalled();
    });

    it('goes home when the existence check reports the room as not found', async () => {
      services.rooms.exists.mockReturnValue(
        throwError(() => new ApiRequestError(404, [{ code: ERROR_CODES.roomNotFound, message: 'Gone.' }])),
      );

      await store.enter('room-1');

      expect(navigate).toHaveBeenCalledWith('/');
      expect(services.me.get).not.toHaveBeenCalled();
    });

    it('goes home when the room is gone by the time a name is submitted', async () => {
      services.me.get.mockReturnValue(noSession());
      await store.enter('room-1');
      hub.invoke.mockRejectedValue(new ApiRequestError(0, [{ code: ERROR_CODES.roomNotFound, message: 'Gone.' }]));

      await store.submitName({ name: 'Ann', personType: 'dev' });

      expect(navigate).toHaveBeenCalledWith('/');
      expect(flashed()).toBe(ROOM_NOT_FOUND_MESSAGE);
    });
  });

  describe('when something else fails', () => {
    it('shows a refused join on the name step, closes the hub and stays', async () => {
      hub.invoke.mockRejectedValue(new ApiRequestError(500, [{ code: 'internal_error', message: 'Server down.' }]));

      await store.enter('room-1');

      expect(store.error()).toContain('Server down.');
      expect(store.phase()).toBe('name');
      expect(hub.disconnect).toHaveBeenCalled();
      expect(navigate).not.toHaveBeenCalled();
    });

    it('shows another validation failure instead of going home', async () => {
      hub.invoke.mockRejectedValue(
        new ApiRequestError(400, [
          { code: ERROR_CODES.validationFailed, message: 'Bad name.', metadata: { field: 'name' } },
        ]),
      );

      await store.enter('room-1');

      expect(store.error()).toContain('Bad name.');
      expect(navigate).not.toHaveBeenCalled();
    });

    it('shows a generic message for an unexpected error and closes the hub', async () => {
      hub.invoke.mockRejectedValue(new Error('socket reset'));

      await store.enter('room-1');

      expect(store.error()).toContain('Something went wrong.');
      expect(hub.disconnect).toHaveBeenCalled();
    });

    it('shows a failed name lookup on the name step', async () => {
      services.me.get.mockReturnValue(
        throwError(() => new ApiRequestError(500, [{ code: 'x', message: 'Lookup failed.' }])),
      );

      await store.enter('room-1');

      expect(store.error()).toContain('Lookup failed.');
      expect(store.phase()).toBe('name');
      expect(hub.invoke).not.toHaveBeenCalled();
    });
  });

  describe('when the room changes under it', () => {
    it('leaves the old room and joins the new one', async () => {
      hub.invoke.mockResolvedValueOnce({ ...JOIN_RESPONSE, people: [ANN, BOB] });
      await store.enter('room-1');
      // The second JoinRoom (for room-2) is held, so the switch can be seen while it is loading.
      let respond!: (response: unknown) => void;
      hub.invoke.mockImplementationOnce(() => new Promise((resolve) => (respond = resolve)));

      const switching = store.enter('room-2');
      await vi.waitFor(() => expect(hub.invoke).toHaveBeenLastCalledWith('JoinRoom', 'room-2'));

      expect(hub.disconnect).toHaveBeenCalled();
      expect(store.people()).toEqual([]);
      respond(JOIN_RESPONSE);
      await switching;

      expect(store.people()).toEqual([ANN]);
      expect(store.phase()).toBe('joined');
    });

    it('does not join the old room when it changes while the name check is pending', async () => {
      const pendingCheck = new Subject<typeof ANN>();
      services.me.get.mockReturnValueOnce(pendingCheck).mockReturnValue(of(ANN));
      const entering = store.enter('room-1');
      await vi.waitFor(() => expect(services.me.get).toHaveBeenCalledTimes(1));

      await store.enter('room-2');
      pendingCheck.next(ANN);
      pendingCheck.complete();
      await entering;

      expect(hub.invoke).toHaveBeenCalledOnce();
      expect(hub.invoke).toHaveBeenCalledWith('JoinRoom', 'room-2');
    });

    it('ignores the snapshot of the old room when its JoinRoom answers after the change', async () => {
      let respondOld!: (response: unknown) => void;
      hub.invoke.mockImplementationOnce(() => new Promise((resolve) => (respondOld = resolve)));
      const entering = store.enter('room-1');
      await vi.waitFor(() => expect(hub.invoke).toHaveBeenCalledTimes(1));

      await store.enter('room-2');
      respondOld({ ...JOIN_RESPONSE, people: [ANN, BOB] });
      await entering;

      expect(store.people()).toEqual([ANN]);
      expect(store.phase()).toBe('joined');
    });

    it('does not join when the store is closed while it is still connecting', async () => {
      let finishConnect!: () => void;
      hub.connect.mockReturnValueOnce(new Promise<void>((resolve) => (finishConnect = resolve)));
      const entering = store.enter('room-1');
      await vi.waitFor(() => expect(hub.connect).toHaveBeenCalled());

      await store.close();
      finishConnect();
      await entering;

      expect(hub.invoke).not.toHaveBeenCalled();
      expect(store.phase()).not.toBe('joined');
    });

    it('does not act on a refusal that arrives after the store was closed', async () => {
      let refuse!: (error: unknown) => void;
      hub.invoke.mockImplementationOnce(() => new Promise((_, reject) => (refuse = reject)));
      const entering = store.enter('room-1');
      await vi.waitFor(() => expect(hub.invoke).toHaveBeenCalled());

      await store.close();
      refuse(new ApiRequestError(0, [{ code: ERROR_CODES.roomNotFound, message: 'Gone.' }]));
      await entering;

      expect(navigate).not.toHaveBeenCalled();
      expect(store.error()).toBeNull();
    });
  });

  describe('what it knows about the person using it', () => {
    it('knows nothing before entering', () => {
      expect(store.me()).toBeNull();
      expect(store.isOwner()).toBe(false);
      expect(store.isObserver()).toBe(false);
      expect(store.myPersonType()).toBeNull();
    });

    it('knows it is the owner when its guid owns the room', async () => {
      await store.enter('room-1');

      expect(store.isOwner()).toBe(true);
    });

    it('is not the owner when someone else owns the room, until ownership is transferred to it', async () => {
      services.me.get.mockReturnValue(of(BOB));
      hub.invoke.mockResolvedValue({ ...JOIN_RESPONSE, people: [ANN, BOB] });
      await store.enter('room-1');
      expect(store.isOwner()).toBe(false);

      hub.emit(HUB_EVENTS.OwnershipTransferred, { newOwner: BOB });

      expect(store.isOwner()).toBe(true);
    });

    it('follows its own role', async () => {
      await store.enter('room-1');
      expect(store.myPersonType()).toBe('dev');
      expect(store.isObserver()).toBe(false);

      hub.emit(HUB_EVENTS.PersonTypeChanged, {
        person: { ...ANN, personType: 'obs' },
        voteStatus: JOIN_RESPONSE.voteStatus,
      });

      expect(store.myPersonType()).toBe('obs');
      expect(store.isObserver()).toBe(true);
    });

    it('counts the people who voted out of the people who can vote, never counting observers', async () => {
      hub.invoke.mockResolvedValue({ ...JOIN_RESPONSE, people: [ANN, BOB, OBS] });
      await store.enter('room-1');
      expect(store.voterCount()).toBe(0);
      expect(store.voterTotal()).toBe(2);

      hub.emit(HUB_EVENTS.VoteCast, { personGuid: BOB.guid });
      hub.emit(HUB_EVENTS.VoteCast, { personGuid: OBS.guid });

      expect(store.voterCount()).toBe(1);

      hub.emit(HUB_EVENTS.VotesCleared, { roundStartedAt: CLEARED_AT });

      expect(store.voterCount()).toBe(0);
    });
  });
});
