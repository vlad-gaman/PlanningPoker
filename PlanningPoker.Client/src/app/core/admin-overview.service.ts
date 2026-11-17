import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminOverview } from '../models/admin-overview';
import { AdminSessionService } from './admin-session.service';
import { ApiClient } from './api-client';

/** The management page's calls to the server. Each one goes through the session guard, so a lost session sends the Operator back to sign-in. */
@Injectable({ providedIn: 'root' })
export class AdminOverviewService {
  private readonly api = inject(ApiClient);
  private readonly session = inject(AdminSessionService);

  get(): Observable<AdminOverview> {
    return this.session.guardExpiry(this.api.get<AdminOverview>('/api/admin/overview'));
  }

  /** Closes the room and deletes it; the people in it are told it was closed by an operator. */
  closeRoom(roomId: string): Observable<unknown> {
    // The server only accepts JSON bodies on POST, so an empty object stands in for no body.
    return this.session.guardExpiry(this.api.post<unknown>(`/api/admin/rooms/${encodeURIComponent(roomId)}/close`, {}));
  }

  /** Removes a person (online or offgrid) from the room; their vote is discarded. */
  removePerson(roomId: string, personId: string): Observable<unknown> {
    return this.session.guardExpiry(
      this.api.post<unknown>(`/api/admin/rooms/${encodeURIComponent(roomId)}/people/${encodeURIComponent(personId)}/remove`, {}),
    );
  }
}
