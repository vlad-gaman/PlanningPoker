// PROTOTYPE (issue 78): three layouts of the management page on one throwaway route, /admin-prototype?variant=A|B|C.
// Stub data only; nothing here talks to the API. Wipe me.
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';
import { PrototypeStore } from './prototype-store';
import { PrototypeSwitcher } from './prototype-switcher';
import { VariantA } from './variant-a';
import { VariantB } from './variant-b';
import { VariantC } from './variant-c';

const NAMES: Record<string, string> = { A: 'Tabbed console', B: 'Dashboard with room cards', C: 'Master-detail' };

@Component({
  selector: 'app-admin-prototype',
  imports: [PrototypeSwitcher, VariantA, VariantB, VariantC],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @switch (variant()) {
      @case ('B') {
        <app-variant-b [store]="store" />
      }
      @case ('C') {
        <app-variant-c [store]="store" />
      }
      @default {
        <app-variant-a [store]="store" />
      }
    }
    <app-prototype-switcher [keys]="keys" [names]="names" [current]="variant()" />
  `,
})
export class AdminPrototype {
  protected readonly store = new PrototypeStore();
  protected readonly keys = ['A', 'B', 'C'];
  protected readonly names = NAMES;
  private readonly param = toSignal(inject(ActivatedRoute).queryParamMap.pipe(map((q) => q.get('variant'))), {
    initialValue: null,
  });
  protected readonly variant = computed(() => (this.keys.includes(this.param() ?? '') ? (this.param() as string) : 'A'));
}
