import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** The illustration files in public/esendex, by art name. */
const FILES: Record<string, string> = {
  'esendex-1': '1',
  'esendex-2': '2',
  'esendex-3': '3',
  'esendex-5': '5',
  'esendex-8': '8',
  'esendex-13': '13',
  'esendex-30': '30',
  'esendex-100': '100',
  'esendex-question': 'q',
  'esendex-coffee': 'coffee',
};

/**
 * One Esendex Fibonacci card drawing, traced from the physical cards (self-contained 750 by 1050 SVGs with a
 * transparent background, so the card face shows through). Corner numbers are left out: the card face is the label
 * around it. The drawing fills its host, which the card or the table sizes.
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

  /** The coffee card's bubble holds a small tilted copy of the cup instead of a number. */
  protected readonly isCoffee = computed(() => this.art() === 'esendex-coffee');

  protected readonly src = computed(() => {
    const file = FILES[this.art()];
    return file ? `esendex/${file}.svg` : null;
  });
}
