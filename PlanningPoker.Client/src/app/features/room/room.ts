import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-room',
  template: '<section class="room"><h1>Room {{ id() }}</h1></section>',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Room {
  readonly id = input.required<string>();
}
