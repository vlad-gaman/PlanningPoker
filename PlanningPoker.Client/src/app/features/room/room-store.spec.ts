import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { FlashMessageService } from '../../core/flash-message.service';
import { RoomHubClient } from '../../core/room-hub.client';
import { ANN, BOB, JOIN_RESPONSE, fakeHub } from './fake-hub.testing';
import { ROOM_DISPOSED_MESSAGE, RoomStore } from './room-store';

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
    hub.emit('PersonJoined', { person: BOB, vote: 'hide', personType: 'test' });

    expect(store.people()).toEqual([ANN, BOB]);
    expect(store.votes()).toEqual({ [BOB.guid]: 'hide' });
  });

  it('does not duplicate a person who joins twice', () => {
    hub.emit('PersonJoined', { person: BOB, vote: null, personType: 'test' });
    hub.emit('PersonJoined', { person: BOB, vote: null, personType: 'test' });

    expect(store.people()).toEqual([ANN, BOB]);
    expect(store.votes()).toEqual({});
  });

  it('removes a person who exited together with their vote', () => {
    hub.emit('PersonJoined', { person: BOB, vote: 'hide', personType: 'test' });

    hub.emit('PersonExited', { person: BOB });

    expect(store.people()).toEqual([ANN]);
    expect(store.votes()).toEqual({});
  });

  it('follows ownership transfers', () => {
    hub.emit('OwnershipTransferred', { newOwner: BOB });

    expect(store.ownerGuid()).toBe(BOB.guid);
  });

  it('joins again after the hub reconnects', async () => {
    hub.invoke.mockResolvedValue({ ...JOIN_RESPONSE, people: [ANN, BOB] });

    hub.reconnect();
    await vi.waitFor(() => expect(store.people()).toEqual([ANN, BOB]));

    expect(hub.invoke).toHaveBeenCalledTimes(2);
  });

  it('goes home with a message when the room is disposed', async () => {
    hub.emit('RoomDisposed', { roomId: 'room-1' });

    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith('/'));
    expect(TestBed.inject(FlashMessageService).message()).toBe(ROOM_DISPOSED_MESSAGE);
    expect(hub.disconnect).toHaveBeenCalledOnce();
    expect(store.people()).toEqual([]);
  });

  it('ignores a disposal of another room', () => {
    hub.emit('RoomDisposed', { roomId: 'other' });

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

    expect(hub.on).toHaveBeenCalledTimes(4);
  });
});
