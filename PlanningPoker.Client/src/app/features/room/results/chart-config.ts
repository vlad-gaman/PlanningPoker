import type { ChartConfiguration, Plugin } from 'chart.js';
import { CardOption, GroupStatistics } from '../../../models/room';
import { cardImage } from '../card-picker/card-picker';

export interface ChartColors {
  primary: string;
  highest: string;
  text: string;
}

/** Height reserved under the axis for a card image label. */
export const IMAGE_LABEL_SIZE = 24;
const IMAGE_LABEL_GAP = 4;
const LABEL_FONT_SIZE = 16;

/** The chart colours as the current theme defines them, so a theme change only needs a re-read. */
export function readChartColors(element: Element): ChartColors {
  const style = getComputedStyle(element);
  return {
    primary: style.getPropertyValue('--chart-color-primary').trim(),
    highest: style.getPropertyValue('--chart-color-success').trim(),
    text: style.getPropertyValue('--text-color').trim(),
  };
}

/** The text a screen reader gets in place of the chart, since the canvas itself has no text. */
export function chartSummary(title: string, group: GroupStatistics): string {
  if (group.marks.length === 0) {
    return `${title}: no votes`;
  }
  const shares = group.marks.map((share) => `${share.mark} ${share.percentage}%`).join(', ');
  return `${title}: ${shares}`;
}

/** Redraws a black line-art image in one colour: the colour is filled over the image and only its shape is kept. */
function tint(image: HTMLImageElement, color: string): HTMLCanvasElement {
  const canvas = document.createElement('canvas');
  canvas.width = image.naturalWidth;
  canvas.height = image.naturalHeight;
  const context = canvas.getContext('2d');
  if (context) {
    context.drawImage(image, 0, 0);
    context.globalCompositeOperation = 'source-in';
    context.fillStyle = color;
    context.fillRect(0, 0, canvas.width, canvas.height);
  }
  return canvas;
}

/** Draws the card images that stand in for the text label of a bar, tinted with the theme's text colour. */
function imageLabels(sources: (string | null)[], color: string, redraw: () => void): Plugin<'bar'> {
  const cache = new Map<string, HTMLImageElement>();
  const tinted = new Map<string, HTMLCanvasElement>();
  return {
    id: 'cardImageLabels',
    afterDraw(chart) {
      const scale = chart.scales['x'];
      sources.forEach((source, index) => {
        if (!source || !scale) {
          return;
        }
        let image = cache.get(source);
        if (!image) {
          image = new Image();
          image.onload = redraw;
          image.src = source;
          cache.set(source, image);
        }
        if (image.complete && image.naturalWidth > 0) {
          const x = scale.getPixelForTick(index) - IMAGE_LABEL_SIZE / 2;
          const key = `${source}|${color}`;
          let label = tinted.get(key);
          if (!label) {
            label = tint(image, color);
            tinted.set(key, label);
          }
          chart.ctx.drawImage(
            label,
            x,
            scale.top + IMAGE_LABEL_GAP,
            IMAGE_LABEL_SIZE,
            IMAGE_LABEL_SIZE,
          );
        }
      });
    },
  };
}

/**
 * The bar chart of one group: a bar per cast mark, in card-set order, with the share as a percentage. The single
 * highest mark gets its own colour. `redraw` is called when a card image finishes loading.
 */
export function buildChartConfig(
  group: GroupStatistics,
  cards: CardOption[],
  colors: ChartColors,
  redraw: () => void = () => undefined,
): ChartConfiguration<'bar'> {
  const options = group.marks.map((share) => cards.find((option) => option.value === share.mark));
  const sources = options.map((card) => (card ? cardImage(card) : null));
  const labels = group.marks.map((share, index) =>
    sources[index] ? '' : (options[index]?.display ?? share.mark),
  );
  const hasImages = sources.some((source) => source !== null);

  return {
    type: 'bar',
    data: {
      labels,
      datasets: [
        {
          data: group.marks.map((share) => share.percentage),
          backgroundColor: group.marks.map((share) =>
            share.mark === group.highestMark ? colors.highest : colors.primary,
          ),
        },
      ],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      animation: false,
      layout: { padding: { bottom: hasImages ? IMAGE_LABEL_SIZE + IMAGE_LABEL_GAP : 0 } },
      scales: {
        x: {
          ticks: { color: colors.text, font: { size: LABEL_FONT_SIZE } },
          grid: { display: false },
        },
        y: { min: 0, max: 100, display: false },
      },
      plugins: {
        legend: { display: false },
        tooltip: { callbacks: { label: (item) => `${item.parsed.y}%` } },
      },
    },
    plugins: [imageLabels(sources, colors.text, redraw)],
  };
}
