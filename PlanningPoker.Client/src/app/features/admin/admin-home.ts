import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';
import { AdminSessionService } from '../../core/admin-session.service';
import { AdminPerson, AdminRoom } from '../../models/admin-overview';
import { PERSON_TYPES } from '../../models/person';
import { AdminConfirmDialog, AdminConfirmDialogData } from './admin-confirm-dialog';
import { AdminDashboardStore } from './admin-dashboard.store';
import { formatUptime } from './admin-format';

/** The signed-in management page: stats, the live rooms with their people, and the issues log. Never shows votes. */
@Component({
  selector: 'app-admin-home',
  imports: [DatePipe],
  providers: [AdminDashboardStore],
  templateUrl: './admin-home.html',
  styleUrl: './admin-home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminHome implements OnInit {
  private readonly dialog = inject(MatDialog);

  protected readonly session = inject(AdminSessionService);
  protected readonly store = inject(AdminDashboardStore);

  ngOnInit(): void {
    this.store.start();
  }

  protected async signOut(): Promise<void> {
    this.store.stop();
    await this.session.signOut();
  }

  protected uptime(seconds: number): string {
    return formatUptime(seconds);
  }

  protected role(person: AdminPerson): string {
    return PERSON_TYPES.find((type) => type.value === person.personType)?.label ?? person.personType;
  }

  protected async closeRoom(room: AdminRoom): Promise<void> {
    const people = room.people.length;
    const confirmed = await this.confirm({
      title: `Close "${room.id}"?`,
      detail: `${people} ${people === 1 ? 'person' : 'people'} will be told the room was closed by an operator. The room is deleted and cannot be restored.`,
      confirmLabel: 'Close room',
    });
    if (confirmed) {
      await this.store.closeRoom(room.id);
    }
  }

  protected async removePerson(room: AdminRoom, person: AdminPerson): Promise<void> {
    const confirmed = await this.confirm({
      title: `Remove ${person.name}?`,
      detail: `${person.name} leaves "${room.id}" and loses their vote. They can join again as a new person.`,
      confirmLabel: 'Remove',
    });
    if (confirmed) {
      await this.store.removePerson(room.id, person.id);
    }
  }

  private async confirm(data: AdminConfirmDialogData): Promise<boolean> {
    return (await firstValueFrom(this.dialog.open(AdminConfirmDialog, { data }).afterClosed())) === true;
  }
}
