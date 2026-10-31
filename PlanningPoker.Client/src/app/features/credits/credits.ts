import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LIBRARY_CREDITS } from './credits.data';

@Component({
  selector: 'app-credits',
  templateUrl: './credits.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Credits {
  protected readonly libraries = LIBRARY_CREDITS;
}
