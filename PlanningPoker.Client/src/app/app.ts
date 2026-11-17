import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { PRODUCT_NAME } from './core/product';
import { AccentSelect } from './shared/accent-select/accent-select';
import { ThemeSelect } from './shared/theme-select/theme-select';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet, AccentSelect, ThemeSelect],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly productName = PRODUCT_NAME;
  protected readonly year = new Date().getFullYear();
}
