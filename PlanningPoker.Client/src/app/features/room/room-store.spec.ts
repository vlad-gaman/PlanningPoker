import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { FlashMessageService } from '../../core/flash-message.service';
import { RoomHubClient } from '../../core/room-hub.client';
import { RoomTabLock } from '../../core/room-tab-lock';
import { ApiRequestError } from '../../core/api-request-error';
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
import { ROOM_DISPOSED_MESSAGE, ROOM_OPENED_ELSEWHERE_MESSAGE, ROOM_NOT_FOUND_MESSAGE, RoomStore, isRoomGone } from './room-store';

const CLEARED_AT = '2026-01-01T12:10:00+00:00';

describe('RoomStore', () => {
  let hub: ReturnType<typeof fakeHub>;
  let store: RoomStore;
  let navigate: ReturnType<typeof vi.spyOn>;
  let taken: (() => void) | null;
  const tabLock = { claim: vi.fn(), onTaken: vi.fn() };

  beforeEach(async () => {
    hub = fakeHub(JOIN_RESPONSE);
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
        { provide: RoomHubClient, useValue: hub },
        { provide: RoomTabLock, useValue: tabLock },
      ],
    });
    store = TestBed.inject(RoomStore);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    await store.join('room-1');
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

    await second.join('room-1');

    expect(second.myMark()).toBe('5');
  });

  it('claims the room for this tab once joined', () => {
    expect(tabLock.claim).toHaveBeenCalledWith('room-1');
    expect(tabLock.onTaken).toHaveBeenCalledWith('room-1', expect.any(Function));
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

  it('binds the hub handlers only once across joins', async () => {
    await store.join('room-1');

    expect(hub.on).toHaveBeenCalledTimes(14);
  });

  it('aborts a join quietly when the store is closed while connecting', async () => {
    let finishConnect!: () => void;
    hub.connect.mockReturnValueOnce(new Promise<void>((resolve) => (finishConnect = resolve)));
    hub.invoke.mockClear();
    hub.on.mockClear();

    const joining = store.join('room-2');
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

    await store.join('room-1');

    expect(store.people()).toEqual([ANN]);
    expect(hub.on).toHaveBeenCalledTimes(28);
  });

  /** Starts a join whose JoinRoom response is held until respond() is called. */
  async function joinWithHeldResponse(snapshot: unknown) {
    let respond!: () => void;
    hub.invoke.mockImplementationOnce(() => new Promise((resolve) => (respond = () => resolve(snapshot))));
    const joining = store.join('room-2');
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

    const superseded = store.join('room-2');
    await store.join('room-3');
    finishConnect();
    await superseded;

    expect(hub.invoke).toHaveBeenCalledOnce();
    expect(hub.invoke).toHaveBeenCalledWith('JoinRoom', 'room-3');
  });

  it('does not apply the snapshot of a join that was superseded while its JoinRoom was in flight', async () => {
    let respond!: (response: unknown) => void;
    hub.invoke.mockImplementationOnce(() => new Promise((resolve) => (respond = resolve)));

    const superseded = store.join('room-2');
    await vi.waitFor(() => expect(hub.invoke).toHaveBeenCalledTimes(2));
    await store.join('room-3');
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

      await store.join('room-2');

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

      await store.join('room-2');

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

    it('replays voting events received during the join after the snapshot', async () => {
      let respond!: () => void;
      hub.invoke.mockImplementationOnce(() => new Promise((resolve) => (respond = () => resolve(JOIN_RESPONSE))));
      const joining = store.join('room-2');
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

      await store.join('room-2');

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
      const joining = store.join('room-2');
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
    TestBed.configureTestingModule({
      providers: [RoomStore, provideRouter([]), { provide: RoomHubClient, useValue: hub }],
    });
    store = TestBed.inject(RoomStore);
    await store.join('room-1');
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
