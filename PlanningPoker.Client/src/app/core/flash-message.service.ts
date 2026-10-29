import { Injectable, signal } from '@angular/core';

/** A one-shot message shown on the next page that asks for it (for example after being sent home). */
@Injectable({ providedIn: 'root' })
export class FlashMessageService {
  private readonly current = signal<string | null>(null);

  readonly message = this.current.asReadonly();

  show(message: string): void {
    this.current.set(message);
  }

  clear(): void {
    this.current.set(null);
  }
}
