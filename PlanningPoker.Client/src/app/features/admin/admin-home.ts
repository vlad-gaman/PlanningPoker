import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AdminSessionService } from '../../core/admin-session.service';

/** The signed-in management page. The dashboard replaces this body; the sign-out control stays. */
@Component({
  selector: 'app-admin-home',
  templateUrl: './admin-home.html',
  styleUrl: './admin-home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminHome {
  protected readonly session = inject(AdminSessionService);

  protected async signOut(): Promise<void> {
    await this.session.signOut();
  }
}
