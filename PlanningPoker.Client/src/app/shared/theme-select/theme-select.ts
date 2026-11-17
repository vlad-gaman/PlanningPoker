import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ThemePreference, ThemeService } from '../../core/theme.service';

/** Auto, light or dark as three icon buttons in one pill, like the room's role picker. */
@Component({
  selector: 'app-theme-select',
  templateUrl: './theme-select.html',
  styleUrl: './theme-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ThemeSelect {
  protected readonly theme = inject(ThemeService);

  protected readonly options: { value: ThemePreference; label: string }[] = [
    { value: 'auto', label: 'Auto' },
    { value: 'light', label: 'Light' },
    { value: 'dark', label: 'Dark' },
  ];
}
