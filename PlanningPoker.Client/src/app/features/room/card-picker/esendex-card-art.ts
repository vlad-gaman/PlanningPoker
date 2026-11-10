import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Side of one pixel of the invader sprite in the artwork's 100 by 140 units. */
const CELL = 2.6;

/**
 * Builds one path of the lit cells of a pixel sprite, with its top-left corner at (x, y). Every cell is a closed
 * square in a single path, so neighbouring cells join without the seams that separate rectangles would show.
 */
function pixelPath(rows: string[], lit: (cell: string) => boolean, x: number, y: number): string {
  return rows
    .flatMap((row, rowIndex) =>
      [...row].flatMap((cell, columnIndex) =>
        lit(cell)
          ? [`M${x + columnIndex * CELL} ${y + rowIndex * CELL}h${CELL}v${CELL}h-${CELL}z`]
          : [],
      ),
    )
    .join('');
}

/** A space invader: 'x' is purple, 'e' is the white of its eyes. Its mouth is a slit drawn over it. */
const INVADER = [
  '..x.....x..',
  '...x...x...',
  '..xxxxxxx..',
  '.xxexxxexx.',
  'xxxxxxxxxxx',
  'x.xxxxxxx.x',
  'x.x.....x.x',
  '...xx.xx...',
];

/** The three invaders across the top of their card, closer together than the cell grid alone would space them. */
const INVADER_X = [12, 40, 68];
const INVADER_Y = 40;

/** The thin light slit of each invader's mouth, on its row 5. */
function mouthPath(x: number, y: number): string {
  const top = y + 5 * CELL + (CELL - 1.2) / 2;
  return `M${x + CELL * 3} ${top}h${CELL * 5}v1.2h-${CELL * 5}z`;
}

/**
 * An original drawing of one Esendex Fibonacci card, in the card's purple, white and grey, coloured from the theme's
 * CSS variables (see styles.scss). Corner numbers are left out: the card face is the label around it. The number
 * drawn on the illustration is the value. The drawing fills its host, which the card or the table sizes.
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

  protected readonly invaderBody = INVADER_X.map((x) =>
    pixelPath(INVADER, (cell) => cell !== '.', x, INVADER_Y),
  ).join('');
  protected readonly invaderEyes = INVADER_X.map((x) =>
    pixelPath(INVADER, (cell) => cell === 'e', x, INVADER_Y),
  ).join('');
  protected readonly invaderMouth = INVADER_X.map((x) => mouthPath(x, INVADER_Y)).join('');
}
