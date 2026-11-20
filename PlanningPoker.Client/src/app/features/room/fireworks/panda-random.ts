import { InjectionToken } from '@angular/core';

/** Source of the random numbers (0 up to, not including, 1) that place the red panda; tests swap it for fixed values. */
export const PANDA_RANDOM = new InjectionToken<() => number>('PANDA_RANDOM', {
  providedIn: 'root',
  factory: () => Math.random,
});
