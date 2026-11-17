import { Injectable, signal } from '@angular/core';

export const RESULTS_BY_ROLE_STORAGE_KEY = 'pp-results-by-role';

/**
 * Remembers, per browser, whether the revealed results show the votes split by role. One key serves every room, so a
 * new round in any room switches it off again for the whole browser.
 */
@Injectable({ providedIn: 'root' })
export class ResultsViewService {
  readonly byRole = signal(this.readStored());

  toggle(): void {
    this.write(!this.byRole());
  }

  /** Called when a new round starts; the by-role view goes back to off. */
  reset(): void {
    this.write(false);
  }

  private write(on: boolean): void {
    this.byRole.set(on);
    try {
      localStorage.setItem(RESULTS_BY_ROLE_STORAGE_KEY, on ? 'on' : 'off');
    } catch {
      // Storage can be unavailable (private mode); the view then lasts for the session only.
    }
  }

  private readStored(): boolean {
    try {
      return localStorage.getItem(RESULTS_BY_ROLE_STORAGE_KEY) === 'on';
    } catch {
      return false;
    }
  }
}
