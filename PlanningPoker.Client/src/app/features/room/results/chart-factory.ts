import { Injectable } from '@angular/core';
import { BarController, BarElement, CategoryScale, Chart, ChartConfiguration, LinearScale, Tooltip } from 'chart.js';

Chart.register(BarController, BarElement, CategoryScale, LinearScale, Tooltip);

/** A drawn chart that can be redrawn or destroyed. */
export interface ChartHandle {
  draw(): void;
  destroy(): void;
}

/** Wraps Chart.js so component tests can swap it for a fake, since a canvas chart cannot be inspected in jsdom. */
@Injectable({ providedIn: 'root' })
export class ChartFactory {
  create(canvas: HTMLCanvasElement, config: ChartConfiguration<'bar'>): ChartHandle {
    const chart = new Chart(canvas, config);
    return { draw: () => chart.draw(), destroy: () => chart.destroy() };
  }
}
