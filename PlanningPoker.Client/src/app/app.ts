import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink, RouterOutlet } from '@angular/router';
import { ThemePreference, ThemeService } from './core/theme.service';

const NEXT_THEME: Record<ThemePreference, ThemePreference> = {
  auto: 'light',
  light: 'dark',
  dark: 'auto',
};

@Component({
  selector: 'app-root',
  imports: [MatButtonModule, RouterLink, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly theme = inject(ThemeService);

  protected cycleTheme(): void {
    this.theme.set(NEXT_THEME[this.theme.preference()]);
  }
}
