import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Person, UpdateMeRequest } from '../models/person';
import { ApiClient } from './api-client';

@Injectable({ providedIn: 'root' })
export class MeService {
  private readonly api = inject(ApiClient);

  get(): Observable<Person> {
    return this.api.get<Person>('/api/me');
  }

  update(request: UpdateMeRequest): Observable<Person> {
    return this.api.put<Person>('/api/me', request);
  }
}
