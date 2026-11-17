import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';

export interface AdminConfirmDialogData {
  title: string;
  /** What will happen, in plain words. */
  detail: string;
  confirmLabel: string;
}

/** Asks the Operator to confirm something that cannot be undone. Closes with true to go ahead, false to cancel. */
@Component({
  selector: 'app-admin-confirm-dialog',
  imports: [MatDialogModule],
  templateUrl: './admin-confirm-dialog.html',
  styleUrl: './admin-confirm-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminConfirmDialog {
  protected readonly data = inject<AdminConfirmDialogData>(MAT_DIALOG_DATA);
}
