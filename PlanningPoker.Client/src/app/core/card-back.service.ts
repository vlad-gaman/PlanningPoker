import { Injectable, signal } from '@angular/core';

/** The card back designs. The artwork lives in `features/room/card-backs.scss`, keyed by the same ids; the ids match PlanningPoker.Common/Constants/CardBacks.cs. */
export const CARD_BACKS = [
  { id: 'classic', label: 'Classic' },
  { id: 'stripes', label: 'Stripes' },
  { id: 'dots', label: 'Dots' },
  { id: 'geometric', label: 'Geometric' },
  { id: 'waves', label: 'Waves' },
  { id: 'checker', label: 'Checker' },
] as const;

export type CardBackId = (typeof CARD_BACKS)[number]['id'];

export const DEFAULT_CARD_BACK: CardBackId = 'classic';
export const CARD_BACK_STORAGE_KEY = 'pp-card-back';

export function isCardBackId(value: unknown): value is CardBackId {
  return CARD_BACKS.some((back) => back.id === value);
}

/** The design to draw for an id from the server: one this client does not know falls back to classic. */
export function resolveCardBack(value: string | null | undefined): CardBackId {
  return isCardBackId(value) ? value : DEFAULT_CARD_BACK;
}

/** The picked card back, per browser. The room sends it to the server so the others see it on this person's face-down card. */
@Injectable({ providedIn: 'root' })
export class CardBackService {
  readonly cardBack = signal<CardBackId>(this.readStored());

  set(cardBack: CardBackId): void {
    this.cardBack.set(cardBack);
    try {
      localStorage.setItem(CARD_BACK_STORAGE_KEY, cardBack);
    } catch {
      // Storage can be unavailable (private mode); the choice then lasts for the session only.
    }
  }

  private readStored(): CardBackId {
    try {
      return resolveCardBack(localStorage.getItem(CARD_BACK_STORAGE_KEY));
    } catch {
      return DEFAULT_CARD_BACK;
    }
  }
}
