import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ACCENT_PRESETS, AccentService, isAccentId } from '../../core/accent.service';

/** The accent colour as a dropdown, placed beside the theme selector. The picked accent shows in its own colour. */
@Component({
  selector: 'app-accent-select',
  templateUrl: './accent-select.html',
  styleUrl: './accent-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccentSelect {
  protected readonly accent = inject(AccentService);
  protected readonly presets = ACCENT_PRESETS;

  /** Applies a value picked in the dropdown. Anything that is not a preset id is ignored. */
  pick(value: string): void {
    if (isAccentId(value)) {
      this.accent.set(value);
    }
  }
}
