import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/** Shows a failure as a popup, so it is seen even when the inline message is off screen (on a phone, say). */
@Injectable({ providedIn: 'root' })
export class ErrorPopupService {
  private readonly snackBar = inject(MatSnackBar);

  show(message: string): void {
    this.snackBar.open(message, 'Dismiss', { duration: 10000, politeness: 'assertive', panelClass: 'error-popup' });
  }
}
