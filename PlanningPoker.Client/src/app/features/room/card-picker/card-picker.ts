import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CardOption } from '../../../models/room';

/** The image to show for a card whose display is a .png file name; null for text cards. */
export function cardImage(card: CardOption): string | null {
  return card.display?.endsWith('.png') ? `/${card.display}` : null;
}

/** The room's cards as a radio group; presentational, the room decides what a pick does. */
@Component({
  selector: 'app-card-picker',
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
}
