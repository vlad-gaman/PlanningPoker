import { DOCUMENT } from '@angular/common';
import { Injectable, effect, inject, signal } from '@angular/core';

/**
 * PROTOTYPE (throwaway). One styling variant for the whole app, kept in sessionStorage so it survives navigation:
 * N current Material look, A Bootstrap + old site.css, B Material bent with tokens, C custom CSS only.
 */
@Injectable({ providedIn: 'root' })
export class ProtoVariant {
  private readonly doc = inject(DOCUMENT);
  readonly variants = ['N', 'A', 'B', 'C'];
  readonly variant = signal(this.initial());

  constructor() {
    effect(() => {
      const v = this.variant();
      this.doc.documentElement.dataset['variant'] = v;
      sessionStorage.setItem('proto-variant', v);
      this.sheet('proto-bootstrap', v === 'A' ? '/prototype/bootstrap.min.css' : null);
      this.sheet('proto-oldsite', v === 'A' ? '/prototype/old-site.css' : null);
      this.sheet('proto-overlay', v === 'N' ? null : `/prototype/variant-${v.toLowerCase()}.css`);
    });
  }

  set(v: string): void {
    this.variant.set(v);
  }

  private initial(): string {
    const fromUrl = new URLSearchParams(location.search).get('variant');
    return fromUrl ?? sessionStorage.getItem('proto-variant') ?? 'N';
  }

  private sheet(id: string, href: string | null): void {
    this.doc.getElementById(id)?.remove();
    if (href) {
      const link = this.doc.createElement('link');
      link.id = id;
      link.rel = 'stylesheet';
      link.href = href;
      this.doc.head.appendChild(link);
    }
  }
}
