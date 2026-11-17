import { Injectable } from '@angular/core';
import { Fireworks } from 'fireworks-js';

/** A running fireworks display on one container. */
export interface FireworksDisplay {
  launch(count: number): void;
  /** Stops the display and removes everything it drew. */
  dispose(): void;
}

/** Wraps fireworks-js so tests can swap it for a fake, since it needs a real canvas and animation frames. */
@Injectable({ providedIn: 'root' })
export class FireworksLauncher {
  create(container: HTMLElement): FireworksDisplay {
    const fireworks = new Fireworks(container, { sound: { enabled: false } });
    fireworks.start();
    return {
      launch: (count) => fireworks.launch(count),
      dispose: () => {
        fireworks.stop(true);
        fireworks.clear();
      },
    };
  }
}
