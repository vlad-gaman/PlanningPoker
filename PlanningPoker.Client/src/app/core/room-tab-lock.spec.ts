import { RoomTabLock } from './room-tab-lock';

describe('RoomTabLock', () => {
  it('tells another tab showing the same room that it was taken', async () => {
    const oldTab = new RoomTabLock();
    const newTab = new RoomTabLock();
    const taken = vi.fn();
    const stop = oldTab.onTaken('room-1', taken);

    newTab.claim('room-1');
    await vi.waitFor(() => expect(taken).toHaveBeenCalledOnce());
    stop();
  });

  it('ignores a claim of another room, a claim by the same tab and a stopped listener', async () => {
    const oldTab = new RoomTabLock();
    const newTab = new RoomTabLock();
    const taken = vi.fn();
    const stop = oldTab.onTaken('room-1', taken);

    newTab.claim('room-2');
    oldTab.claim('room-1');
    stop();
    newTab.claim('room-1');
    await new Promise((resolve) => setTimeout(resolve, 50));

    expect(taken).not.toHaveBeenCalled();
  });

  it('still tells tabs apart where crypto.randomUUID is missing, as over plain HTTP', async () => {
    vi.stubGlobal('crypto', {});
    const oldTab = new RoomTabLock();
    const newTab = new RoomTabLock();
    vi.unstubAllGlobals();
    const taken = vi.fn();
    const stop = oldTab.onTaken('room-1', taken);

    newTab.claim('room-1');
    await vi.waitFor(() => expect(taken).toHaveBeenCalledOnce());
    stop();
  });
});
