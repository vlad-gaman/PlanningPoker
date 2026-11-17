import { DOCUMENT } from '@angular/common';
import { Injectable, computed, effect, inject, signal } from '@angular/core';

export type ThemePreference = 'auto' | 'light' | 'dark';
export type EffectiveTheme = 'light' | 'dark';

export const THEME_STORAGE_KEY = 'pp-theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly media = this.document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)');

  readonly preference = signal<ThemePreference>(this.readStored());
  private readonly systemDark = signal(this.media?.matches ?? false);

  readonly effective = computed<EffectiveTheme>(() => {
    const preference = this.preference();
    if (preference === 'auto') {
      return this.systemDark() ? 'dark' : 'light';
    }
    return preference;
  });

  constructor() {
    this.media?.addEventListener('change', (event) => this.systemDark.set(event.matches));
    effect(() => this.document.documentElement.setAttribute('data-theme', this.effective()));
  }

  set(preference: ThemePreference): void {
    this.preference.set(preference);
    try {
      localStorage.setItem(THEME_STORAGE_KEY, preference);
    } catch {
      // Storage can be unavailable (private mode); the preference then lasts for the session only.
    }
  }

  private readStored(): ThemePreference {
    try {
      const stored = localStorage.getItem(THEME_STORAGE_KEY);
      return stored === 'light' || stored === 'dark' ? stored : 'auto';
    } catch {
      return 'auto';
    }
  }
}
