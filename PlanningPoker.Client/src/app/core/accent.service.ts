import { DOCUMENT } from '@angular/common';
import { Injectable, effect, inject, signal } from '@angular/core';

/** The accent presets. The colour values live in `src/accents.scss`, keyed by the same ids. */
export const ACCENT_PRESETS = [
  { id: 'blue', label: 'Blue' },
  { id: 'indigo', label: 'Indigo' },
  { id: 'violet', label: 'Violet' },
  { id: 'magenta', label: 'Magenta' },
  { id: 'cyan', label: 'Cyan' },
  { id: 'graphite', label: 'Graphite' },
  { id: 'red', label: 'Red' },
  { id: 'orange', label: 'Orange' },
  { id: 'amber', label: 'Amber' },
  { id: 'green', label: 'Green' },
  { id: 'teal', label: 'Teal' },
  { id: 'brown', label: 'Brown' },
  { id: 'lime', label: 'Lime' },
  { id: 'sky', label: 'Sky' },
  { id: 'rose', label: 'Rose' },
  { id: 'plum', label: 'Plum' },
  { id: 'slate', label: 'Slate' },
  { id: 'gold', label: 'Gold' },
] as const;

export type AccentId = (typeof ACCENT_PRESETS)[number]['id'];

export const DEFAULT_ACCENT: AccentId = 'blue';
export const ACCENT_STORAGE_KEY = 'pp-accent';

export function isAccentId(value: unknown): value is AccentId {
  return ACCENT_PRESETS.some((preset) => preset.id === value);
}

/** The picked accent colour, per browser. The page reads it from the `data-accent` attribute on the html element. */
@Injectable({ providedIn: 'root' })
export class AccentService {
  private readonly document = inject(DOCUMENT);

  readonly accent = signal<AccentId>(this.readStored());

  constructor() {
    effect(() => this.document.documentElement.setAttribute('data-accent', this.accent()));
  }

  set(accent: AccentId): void {
    this.accent.set(accent);
    try {
      localStorage.setItem(ACCENT_STORAGE_KEY, accent);
    } catch {
      // Storage can be unavailable (private mode); the accent then lasts for the session only.
    }
  }

  private readStored(): AccentId {
    try {
      const stored = localStorage.getItem(ACCENT_STORAGE_KEY);
      return isAccentId(stored) ? stored : DEFAULT_ACCENT;
    } catch {
      return DEFAULT_ACCENT;
    }
  }
}
