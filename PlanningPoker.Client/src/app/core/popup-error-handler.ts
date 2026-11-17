import { ErrorHandler, Injectable, inject } from '@angular/core';
import { ErrorPopupService } from './error-popup.service';

/** Keeps Angular's default logging and also tells the user, since an uncaught error otherwise looks like nothing happened. */
@Injectable()
export class PopupErrorHandler implements ErrorHandler {
  private readonly popup = inject(ErrorPopupService);

  handleError(error: unknown): void {
    console.error(error);
    const detail = error instanceof Error ? error.message : String(error);
    this.popup.show(`Something went wrong: ${detail}`);
  }
}
