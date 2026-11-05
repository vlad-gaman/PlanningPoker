import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { PRODUCT_NAME } from './product';

/** Tab title "<page> - Planning Poker", like the Razor layout's `ViewData["Title"]`. */
@Injectable({ providedIn: 'root' })
export class PageTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const page = this.buildTitle(snapshot);
    this.title.setTitle(page ? `${page} - ${PRODUCT_NAME}` : PRODUCT_NAME);
  }
}
