import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { PRODUCT_NAME } from './core/product';
import { ThemeSelect } from './shared/theme-select/theme-select';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet, ThemeSelect],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly productName = PRODUCT_NAME;
  protected readonly year = new Date().getFullYear();

  // PROTOTYPE: which compact (small window / phone) layout to show. `?compact=a|b|c` picks one and it is remembered.
  protected readonly compactOptions = ['a', 'b', 'c'] as const;
  protected readonly compact = signal<string>(this.initialCompact());

  protected chooseCompact(option: string): void {
    this.compact.set(option);
    localStorage.setItem('compact-layout', option);
    document.documentElement.dataset['compact'] = option;
  }

  private initialCompact(): string {
    const asked = new URLSearchParams(location.search).get('compact');
    const option = asked ?? localStorage.getItem('compact-layout') ?? 'a';
    const chosen = ['a', 'b', 'c'].includes(option) ? option : 'a';
    localStorage.setItem('compact-layout', chosen);
    document.documentElement.dataset['compact'] = chosen;
    return chosen;
  }
}
