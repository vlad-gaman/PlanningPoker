// PROTOTYPE (issue 78): floating variant bar. Wipe me.
import { Component, HostListener, inject, input } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-prototype-switcher',
  template: `
    <div class="bar" role="toolbar" aria-label="Prototype variant">
      <button type="button" (click)="step(-1)" aria-label="Previous variant">&larr;</button>
      <span>{{ current() }} ({{ names()[current()] }})</span>
      <button type="button" (click)="step(1)" aria-label="Next variant">&rarr;</button>
    </div>
  `,
  styles: `
    .bar {
      position: fixed; left: 50%; bottom: 1rem; transform: translateX(-50%); z-index: 1000; display: flex; gap: 0.75rem;
      align-items: center; padding: 0.4rem 0.9rem; border-radius: 999px; background: #111; color: #fff;
      font: 600 14px system-ui; box-shadow: 0 4px 16px rgba(0, 0, 0, 0.4);
    }
    button { background: none; border: 0; color: inherit; font-size: 18px; cursor: pointer; }
  `,
})
export class PrototypeSwitcher {
  readonly keys = input.required<string[]>();
  readonly names = input.required<Record<string, string>>();
  readonly current = input.required<string>();
  private readonly router = inject(Router);

  step(delta: number): void {
    const keys = this.keys();
    const next = keys[(keys.indexOf(this.current()) + delta + keys.length) % keys.length];
    void this.router.navigate([], { queryParams: { variant: next }, queryParamsHandling: 'merge', replaceUrl: true });
  }

  @HostListener('document:keydown', ['$event'])
  onKey(event: KeyboardEvent): void {
    const el = event.target as HTMLElement | null;
    if (el && (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || el.isContentEditable)) return;
    if (event.key === 'ArrowLeft') this.step(-1);
    if (event.key === 'ArrowRight') this.step(1);
  }
}
