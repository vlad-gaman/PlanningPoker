import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { PRODUCT_NAME } from './core/product';
import { AccentSelect } from './shared/accent-select/accent-select';
import { CardBackSelect } from './shared/card-back-select/card-back-select';
import { AlarmSoundSelect } from './shared/alarm-sound-select/alarm-sound-select';
import { ThemeSelect } from './shared/theme-select/theme-select';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet, AccentSelect, AlarmSoundSelect, CardBackSelect, ThemeSelect],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly productName = PRODUCT_NAME;
  protected readonly year = new Date().getFullYear();
}
