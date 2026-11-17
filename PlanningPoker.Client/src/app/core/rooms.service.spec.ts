import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { RoomsService } from './rooms.service';

describe('RoomsService', () => {
  let service: RoomsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(RoomsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the card sets', () => {
    let result: unknown;
    service.cardSets().subscribe((value) => (result = value));

    http.expectOne('/api/card-sets').flush({ data: [{ value: 'linear', display: 'Linear (1-10)' }], errors: null });

    expect(result).toEqual([{ value: 'linear', display: 'Linear (1-10)' }]);
  });

  it('posts the room request and returns the new id', () => {
    const body = {
      useFunRoomName: false,
      configuration: { cardSet: 'linear', countdownSeconds: 5, showFireworks: true, maxParticipants: 50 },
    };
    let result: unknown;
    service.create(body).subscribe((value) => (result = value));

    const request = http.expectOne('/api/rooms');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush({ data: { id: 'abc' }, errors: null });

    expect(result).toEqual({ id: 'abc' });
  });

  it('checks that a room exists, encoding the id', () => {
    service.exists('a b').subscribe();

    http.expectOne('/api/rooms/a%20b').flush({ data: { id: 'a b', exists: true }, errors: null });
  });
});
