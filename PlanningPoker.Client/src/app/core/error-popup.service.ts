import { Injectable, inject } from '@angular/core';
import { MatSnackBar, MatSnackBarRef, TextOnlySnackBar } from '@angular/material/snack-bar';

/** Shows a failure as a popup, so it is seen even when the inline message is off screen (on a phone, say). */
@Injectable({ providedIn: 'root' })
export class ErrorPopupService {
  private readonly snackBar = inject(MatSnackBar);
  private current: MatSnackBarRef<TextOnlySnackBar> | null = null;

  /** Shows the message for ten seconds, or until dismissed when it is sticky. */
  show(message: string, sticky = false): void {
    this.current = this.snackBar.open(message, 'Dismiss', {
      duration: sticky ? undefined : 10000,
      politeness: 'assertive',
      panelClass: 'error-popup',
    });
  }

  /** Closes the popup that is showing, if any. */
  dismiss(): void {
    this.current?.dismiss();
    this.current = null;
  }
}
