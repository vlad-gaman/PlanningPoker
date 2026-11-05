import { Injectable } from '@angular/core';

const CHANNEL_NAME = 'planning-poker-room-tab';

function noop(): void {
  // Nothing to stop: there is no channel to listen on.
}

/**
 * Lets only one tab of a browser show a room at a time. The tab that opens a room announces it on a same-origin
 * channel, and every other tab showing that room hears it and gives the room up.
 */
@Injectable({ providedIn: 'root' })
export class RoomTabLock {
  private readonly tabId = crypto.randomUUID();
  private readonly channel = typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel(CHANNEL_NAME);

  /** Tells the other tabs of this browser that this tab now shows the room. */
  claim(roomId: string): void {
    this.channel?.postMessage({ tabId: this.tabId, roomId });
  }

  /** Calls back when another tab claims the room; returns the function that stops listening. */
  onTaken(roomId: string, callback: () => void): () => void {
    const channel = this.channel;
    if (!channel) {
      return noop;
    }
    const listener = (event: MessageEvent<{ tabId: string; roomId: string }>) => {
      if (event.data.tabId !== this.tabId && event.data.roomId === roomId) {
        callback();
      }
    };
    channel.addEventListener('message', listener);
    return () => channel.removeEventListener('message', listener);
  }
}
