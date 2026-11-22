import { Injectable } from '@angular/core';

export const CLOSING_URL = '/api/presence/closing';

/**
 * Tells the server when the page is going away. The `pagehide` event is the last one a closing tab reliably fires, but it
 * fires for a reload too, so the server only starts a short countdown that a rejoin cancels. The beacon outlives the page:
 * an ordinary request would be cancelled with it.
 */
@Injectable({ providedIn: 'root' })
export class ClosingBeacon {
  /**
   * Sends the hub connection's id when the page is hidden for good; the id is read then, not now, because the connection
   * changes with every reconnect. Returns the function that stops listening.
   */
  watch(connectionId: () => string | null): () => void {
    const listener = () => {
      const id = connectionId();
      if (id) {
        this.send(id);
      }
    };
    window.addEventListener('pagehide', listener);
    return () => window.removeEventListener('pagehide', listener);
  }

  private send(connectionId: string): void {
    // The JSON content type is what the API accepts; a Blob is how a beacon sets one.
    navigator.sendBeacon?.(CLOSING_URL, new Blob([JSON.stringify({ connectionId })], { type: 'application/json' }));
  }
}
