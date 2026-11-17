import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CardOption } from '../../../models/room';
import { EsendexCardArt } from './esendex-card-art';

/** A card display of "art:NAME" draws the illustration NAME instead of text. */
const ART_PREFIX = 'art:';

/** The image to show for a card whose display is a .png file name; null for text cards. */
export function cardImage(card: CardOption): string | null {
  return card.display?.endsWith('.png') ? `/${card.display}` : null;
}

/** The illustration name for a card whose display is "art:NAME"; null for text and image cards. */
export function cardArt(card: CardOption): string | null {
  return card.display?.startsWith(ART_PREFIX) ? card.display.slice(ART_PREFIX.length) : null;
}

/** The text to show for a card: its display, or null when the display is an image or an illustration. */
export function cardText(card: CardOption): string | null {
  return card.display === null || cardImage(card) !== null || cardArt(card) !== null ? null : card.display;
}

/** The room's cards as a radio group; presentational, the room decides what a pick does. */
@Component({
  selector: 'app-card-picker',
  imports: [EsendexCardArt],
  templateUrl: './card-picker.html',
  styleUrl: './card-picker.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardPicker {
  readonly cards = input.required<CardOption[]>();
  readonly selected = input<string | null>(null);
  readonly disabled = input(false);
  readonly markSelected = output<string>();

  protected readonly image = cardImage;
  protected readonly art = cardArt;
  protected readonly text = cardText;
}
