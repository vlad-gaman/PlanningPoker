import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { ICON_CREDITS, ICON_CREDIT_URL, LIBRARY_CREDITS } from './credits.data';

@Component({
  selector: 'app-credits',
  imports: [MatButtonModule, RouterLink],
  templateUrl: './credits.html',
  styleUrl: './credits.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Credits {
  protected readonly icons = ICON_CREDITS;
  protected readonly iconUrl = ICON_CREDIT_URL;
  protected readonly libraries = LIBRARY_CREDITS;
}
