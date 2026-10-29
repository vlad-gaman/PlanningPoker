import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CardSet, CreateRoomRequest, RoomCreated, RoomExists } from '../models/room';
import { ApiClient } from './api-client';

@Injectable({ providedIn: 'root' })
export class RoomsService {
  private readonly api = inject(ApiClient);

  cardSets(): Observable<CardSet[]> {
    return this.api.get<CardSet[]>('/api/card-sets');
  }

  create(request: CreateRoomRequest): Observable<RoomCreated> {
    return this.api.post<RoomCreated>('/api/rooms', request);
  }

  exists(id: string): Observable<RoomExists> {
    return this.api.get<RoomExists>(`/api/rooms/${encodeURIComponent(id)}`);
  }
}
