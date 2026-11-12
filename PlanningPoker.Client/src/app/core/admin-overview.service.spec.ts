import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { ERROR_CODES } from '../models/api';
import { OVERVIEW } from '../testing/admin-overview.testing';
import { ADMIN_SIGN_IN_URL } from './admin-session.service';
import { AdminOverviewService } from './admin-overview.service';

describe('AdminOverviewService', () => {
  let service: AdminOverviewService;
  let http: HttpTestingController;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    service = TestBed.inject(AdminOverviewService);
    http = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  });

  afterEach(() => http.verify());

  it('gets the overview', () => {
    let result: unknown;
    service.get().subscribe((value) => (result = value));

    http.expectOne({ method: 'GET', url: '/api/admin/overview' }).flush({ data: OVERVIEW, errors: null });

    expect(result).toEqual(OVERVIEW);
  });

  it('posts an empty JSON body to close a room', () => {
    service.closeRoom('blue-fox').subscribe();

    const request = http.expectOne({ method: 'POST', url: '/api/admin/rooms/blue-fox/close' });
    expect(request.request.body).toEqual({});
    request.flush({ data: null, errors: null });
  });

  it('encodes the room id in the path', () => {
    service.closeRoom('a b/c').subscribe();

    http.expectOne({ method: 'POST', url: '/api/admin/rooms/a%20b%2Fc/close' }).flush({ data: null, errors: null });
  });

  it('posts an empty JSON body to remove a person', () => {
    service.removePerson('blue-fox', 'p-bob').subscribe();

    const request = http.expectOne({ method: 'POST', url: '/api/admin/rooms/blue-fox/people/p-bob/remove' });
    expect(request.request.body).toEqual({});
    request.flush({ data: null, errors: null });
  });

  it.each([
    ['the overview', () => service.get()],
    ['closing a room', () => service.closeRoom('blue-fox')],
    ['removing a person', () => service.removePerson('blue-fox', 'p-bob')],
  ])('sends the Operator back to sign-in when the session is gone while %s', (_name, call) => {
    call().subscribe({ error: () => undefined });

    http
      .expectOne(() => true)
      .flush({ data: null, errors: [{ code: ERROR_CODES.unauthorized, message: 'Sign in is required.' }] }, { status: 401, statusText: 'Unauthorized' });

    expect(navigate).toHaveBeenCalledWith(ADMIN_SIGN_IN_URL);
  });

  it('does not treat a missing room as a lost session', () => {
    service.closeRoom('nowhere').subscribe({ error: () => undefined });

    http
      .expectOne(() => true)
      .flush({ data: null, errors: [{ code: ERROR_CODES.roomNotFound, message: 'Gone.' }] }, { status: 404, statusText: 'Not Found' });

    expect(navigate).not.toHaveBeenCalled();
  });
});
