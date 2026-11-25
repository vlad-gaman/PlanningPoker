import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ACCENT_PRESETS, AccentId, AccentService, isAccentId } from '../../core/accent.service';

/**
 * The accent colour as a dropdown, placed beside the theme selector. A custom select-only combobox, so the dot can be
 * coloured apart from the label: the button keeps focus and points at the highlighted option with aria-activedescendant.
 */
@Component({
  selector: 'app-accent-select',
  templateUrl: './accent-select.html',
  styleUrl: './accent-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccentSelect {
  protected readonly accent = inject(AccentService);
  protected readonly presets = ACCENT_PRESETS;

  protected readonly open = signal(false);
  /** Index of the highlighted option while the list is open. */
  protected readonly active = signal(0);
  protected readonly selected = computed(
    () => this.presets.find((preset) => preset.id === this.accent.accent()) ?? this.presets[0],
  );

  /** Applies a value picked in the dropdown. Anything that is not a preset id is ignored. */
  pick(value: string): void {
    if (isAccentId(value)) {
      this.accent.set(value);
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

  protected choose(id: AccentId): void {
    this.pick(id);
    this.close();
  }

  protected onKeydown(event: KeyboardEvent): void {
    const last = this.presets.length - 1;
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
          this.choose(this.presets[this.active()].id);
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
    this.active.set(Math.max(0, this.presets.findIndex((preset) => preset.id === this.accent.accent())));
    this.open.set(true);
  }
}
