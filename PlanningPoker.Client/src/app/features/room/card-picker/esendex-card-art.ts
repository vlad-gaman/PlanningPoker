import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Side of one pixel in the artwork's 100 by 140 units. */
const CELL = 2.4;

/** A lit pixel of a pixel-art sprite, by its top-left corner. */
interface Pixel {
  x: number;
  y: number;
}

/** Lays out the cells of a sprite that equal `lit`, with its top-left corner at (x, y). */
function pixels(rows: string[], lit: string, x: number, y: number): Pixel[] {
  return rows.flatMap((row, rowIndex) =>
    [...row].flatMap((cell, columnIndex) =>
      cell === lit ? [{ x: x + columnIndex * CELL, y: y + rowIndex * CELL }] : [],
    ),
  );
}

/** A space invader: 'x' is purple, 'e' is the white of its eyes and mouth. */
const INVADER = [
  '..x.....x..',
  '...x...x...',
  '..xxxxxxx..',
  '.xxexxxexx.',
  'xxxxxxxxxxx',
  'x.eeeeeee.x',
  'x.x.....x.x',
  '...xx.xx...',
];

/** The cannon at the foot of the space invaders card: 'x' is purple, 'e' the white band under it. */
const CANNON = [
  '....x....',
  '...xxx...',
  '..xxxxx..',
  '.xxxxxxx.',
  'xxxxxxxxx',
  'xxxxxxxxx',
  'xxxxxxxxx',
  'eeeeeeeee',
];

/**
 * An original drawing of one Esendex Fibonacci card, in the card's purple, white and grey, coloured from the theme's
 * CSS variables (see styles.scss). Corner numbers are left out: the card face is the label around it. The number
 * drawn on the illustration is the value.
 */
@Component({
  selector: 'app-esendex-card-art',
  templateUrl: './esendex-card-art.html',
  styleUrl: './esendex-card-art.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EsendexCardArt {
  readonly art = input.required<string>();
  /** The card's value, read out as the image's name. */
  readonly label = input.required<string>();

  protected readonly invaders = [9, 37, 65].flatMap((x) => pixels(INVADER, 'x', x, 24));
  protected readonly invaderEyes = [9, 37, 65].flatMap((x) => pixels(INVADER, 'e', x, 24));
  protected readonly cannon = pixels(CANNON, 'x', 39.2, 94);
  protected readonly cannonBand = pixels(CANNON, 'e', 39.2, 94);
  protected readonly cell = CELL;
}
