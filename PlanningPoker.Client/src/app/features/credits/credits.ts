import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-credits',
  template: '<section class="credits"><h1>Credits</h1></section>',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Credits {}
