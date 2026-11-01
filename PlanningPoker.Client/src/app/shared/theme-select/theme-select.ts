import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ThemePreference, ThemeService } from '../../core/theme.service';

@Component({
  selector: 'app-theme-select',
  templateUrl: './theme-select.html',
  styleUrl: './theme-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ThemeSelect {
  protected readonly theme = inject(ThemeService);

  protected pick(event: Event): void {
    this.theme.set((event.target as HTMLSelectElement).value as ThemePreference);
  }
}
