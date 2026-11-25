import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CARD_BACKS, CardBackId, CardBackService, isCardBackId } from '../../core/card-back.service';

/**
 * The card back as a dropdown, placed beside the accent selector. A custom select-only combobox, so each option can show
 * a swatch drawn with its own design: the button keeps focus and points at the highlighted option with aria-activedescendant.
 */
@Component({
  selector: 'app-card-back-select',
  templateUrl: './card-back-select.html',
  styleUrl: './card-back-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardBackSelect {
  protected readonly cardBacks = inject(CardBackService);
  protected readonly options = CARD_BACKS;

  protected readonly open = signal(false);
  /** Index of the highlighted option while the list is open. */
  protected readonly active = signal(0);
  protected readonly selected = computed(
    () => this.options.find((option) => option.id === this.cardBacks.cardBack()) ?? this.options[0],
  );

  /** Applies a value picked in the dropdown. Anything that is not a card back id is ignored. */
  pick(value: string): void {
    if (isCardBackId(value)) {
      this.cardBacks.set(value);
    }
  }

  protected toggle(): void {
    if (this.open()) {
      this.close();
    } else {
      this.show();
    }
  }

  protected close(): void {
    this.open.set(false);
  }

  protected choose(id: CardBackId): void {
    this.pick(id);
    this.close();
  }

  protected onKeydown(event: KeyboardEvent): void {
    const last = this.options.length - 1;
    switch (event.key) {
      case 'ArrowDown':
      case 'ArrowUp': {
        event.preventDefault();
        if (!this.open()) {
          this.show();
          break;
        }
        const step = event.key === 'ArrowDown' ? 1 : -1;
        this.active.update((index) => Math.min(last, Math.max(0, index + step)));
        break;
      }
      case 'Home':
      case 'End':
        if (this.open()) {
          event.preventDefault();
          this.active.set(event.key === 'Home' ? 0 : last);
        }
        break;
      case 'Enter':
      case ' ':
        event.preventDefault();
        if (this.open()) {
          this.choose(this.options[this.active()].id);
        } else {
          this.show();
        }
        break;
      case 'Escape':
        if (this.open()) {
          event.preventDefault();
          this.close();
        }
        break;
    }
  }

  private show(): void {
    this.active.set(Math.max(0, this.options.findIndex((option) => option.id === this.cardBacks.cardBack())));
    this.open.set(true);
  }
}
