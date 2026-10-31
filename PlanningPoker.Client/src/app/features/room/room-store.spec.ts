import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { FlashMessageService } from '../../core/flash-message.service';
import { RoomHubClient } from '../../core/room-hub.client';
import { ApiRequestError } from '../../core/api-request-error';
import { HUB_EVENTS } from '../../models/hub-events';
import {
  ANN,
  BOB,
  CONSENSUS_STATISTICS,
  JOIN_RESPONSE,
  STATISTICS,
  fakeHub,
  revealed,
} from '../../testing/fake-hub.testing';
import { ROOM_DISPOSED_MESSAGE, ROOM_NOT_FOUND_MESSAGE, RoomStore, isRoomGone } from './room-store';

describe('RoomStore', () => {
  let hub: ReturnType<typeof fakeHub>;
  let store: RoomStore;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(async () => {
    hub = fakeHub(JOIN_RESPONSE);
    TestBed.configureTestingModule({
      providers: [RoomStore, provideRouter([]), { provide: RoomHubClient, useValue: hub }],
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

  it('follows ownership transfers', () => {
    hub.emit(HUB_EVENTS.OwnershipTransferred, { newOwner: BOB });

    expect(store.ownerGuid()).toBe(BOB.guid);
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

  it('leave disconnects, clears the state and navigates home', async () => {
    await store.leave();

    expect(hub.disconnect).toHaveBeenCalledOnce();
    expect(navigate).toHaveBeenCalledWith('/');
    expect(store.people()).toEqual([]);
    expect(store.ownerGuid()).toBeNull();
  });

  it('binds the hub handlers only once across joins', async () => {
    await store.join('room-1');

    expect(hub.on).toHaveBeenCalledTimes(10);
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
    expect(hub.on).toHaveBeenCalledTimes(20);
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

      hub.emit(HUB_EVENTS.VotesCleared, undefined);

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
      hub.emit(HUB_EVENTS.VotesCleared, undefined);
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

      hub.emit(HUB_EVENTS.VotesCleared, undefined);

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
