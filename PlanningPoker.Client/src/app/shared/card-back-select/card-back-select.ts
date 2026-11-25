import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CARD_BACKS, CardBackService } from '../../core/card-back.service';

/** The card back as a row of small swatches in one pill, next to the theme switch. Each swatch is drawn with its own design. */
@Component({
  selector: 'app-card-back-select',
  templateUrl: './card-back-select.html',
  styleUrl: './card-back-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardBackSelect {
  protected readonly cardBacks = inject(CardBackService);
  protected readonly options = CARD_BACKS;
}
