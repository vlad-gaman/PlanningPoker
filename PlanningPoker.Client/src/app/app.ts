import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { ThemeSelect } from './shared/theme-select/theme-select';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet, ThemeSelect],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly year = new Date().getFullYear();
}
