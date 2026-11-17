import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiRequestError } from './api-request-error';
import { ADMIN_SESSION_EXPIRED_MESSAGE, ADMIN_SIGN_IN_URL, AdminSessionService } from './admin-session.service';
import { FlashMessageService } from './flash-message.service';
import { ERROR_CODES } from '../models/api';

const SIGNED_IN = { authenticated: true, username: 'operator', csrfToken: 't' };

describe('AdminSessionService', () => {
  let service: AdminSessionService;
  let http: HttpTestingController;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    service = TestBed.inject(AdminSessionService);
    http = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  });

  afterEach(() => http.verify());

  it('starts with no session', () => {
    expect(service.session()).toBeNull();
    expect(service.signedIn()).toBe(false);
    expect(service.username()).toBeNull();
  });

  it('loads the session from the server', async () => {
    const loading = service.load();
    http.expectOne({ method: 'GET', url: '/api/admin/session' }).flush({ data: SIGNED_IN, errors: null });

    expect(await loading).toEqual(SIGNED_IN);
    expect(service.signedIn()).toBe(true);
    expect(service.username()).toBe('operator');
  });

  it('asks the server only once when the session is wanted again', async () => {
    const first = service.ensureLoaded();
    http.expectOne('/api/admin/session').flush({ data: { authenticated: false }, errors: null });
    await first;

    await service.ensureLoaded();

    http.expectNone('/api/admin/session');
    expect(service.signedIn()).toBe(false);
  });

  it('posts the credentials and is signed in on success', async () => {
    const signingIn = service.signIn('operator', 'secret');
    const request = http.expectOne({ method: 'POST', url: '/api/admin/login' });
    expect(request.request.body).toEqual({ username: 'operator', password: 'secret' });
    request.flush({ data: SIGNED_IN, errors: null });

    expect(await signingIn).toEqual({ kind: 'signedIn' });
    expect(service.signedIn()).toBe(true);
  });

  it('hands back the challenge when the server asks for a code, and is not signed in', async () => {
    const signingIn = service.signIn('operator', 'secret');
    http.expectOne('/api/admin/login').flush(
      { data: null, errors: [{ code: ERROR_CODES.adminTotpRequired, message: 'Enter the code.', metadata: { challenge: 'abc.def' } }] },
      { status: 401, statusText: 'Unauthorized' },
    );

    expect(await signingIn).toEqual({ kind: 'code', challenge: 'abc.def' });
    expect(service.signedIn()).toBe(false);
  });

  it('rethrows any other failure of the password step', async () => {
    const signingIn = service.signIn('operator', 'nope');
    http.expectOne('/api/admin/login').flush(
      { data: null, errors: [{ code: ERROR_CODES.adminInvalidCredentials, message: 'Wrong.' }] },
      { status: 401, statusText: 'Unauthorized' },
    );

    await expect(signingIn).rejects.toMatchObject({ status: 401 });
    expect(service.signedIn()).toBe(false);
  });

  it('posts the challenge and the code to finish the sign-in', async () => {
    const completing = service.completeCode('abc.def', '123456');
    const request = http.expectOne({ method: 'POST', url: '/api/admin/login/totp' });
    expect(request.request.body).toEqual({ challenge: 'abc.def', code: '123456' });
    request.flush({ data: SIGNED_IN, errors: null });

    await completing;
    expect(service.signedIn()).toBe(true);
  });

  it('rejects a wrong code and stays signed out', async () => {
    const completing = service.completeCode('abc.def', '000000');
    http.expectOne('/api/admin/login/totp').flush(
      { data: null, errors: [{ code: ERROR_CODES.adminTotpInvalid, message: 'The code is incorrect.' }] },
      { status: 401, statusText: 'Unauthorized' },
    );

    await expect(completing).rejects.toBeInstanceOf(ApiRequestError);
    expect(service.signedIn()).toBe(false);
  });

  it('signs out with a JSON body, forgets the session and goes to the sign-in page', async () => {
    const loading = service.load();
    http.expectOne('/api/admin/session').flush({ data: SIGNED_IN, errors: null });
    await loading;

    const signingOut = service.signOut();
    const request = http.expectOne({ method: 'POST', url: '/api/admin/logout' });
    expect(request.request.body).toEqual({});
    request.flush({ data: { authenticated: false }, errors: null });
    await signingOut;

    expect(service.signedIn()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(ADMIN_SIGN_IN_URL);
  });

  it('forgets the session even when the server cannot sign it out', async () => {
    const loading = service.load();
    http.expectOne('/api/admin/session').flush({ data: SIGNED_IN, errors: null });
    await loading;

    const signingOut = service.signOut();
    http.expectOne('/api/admin/logout').flush(
      { data: null, errors: [{ code: ERROR_CODES.unauthorized, message: 'Sign in is required.' }] },
      { status: 401, statusText: 'Unauthorized' },
    );

    await expect(signingOut).rejects.toBeInstanceOf(ApiRequestError);
    expect(service.signedIn()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(ADMIN_SIGN_IN_URL);
  });

  describe('guardExpiry', () => {
    const unauthorized = new ApiRequestError(401, [{ code: ERROR_CODES.unauthorized, message: 'Sign in is required.' }]);

    it('passes a value through untouched', async () => {
      let received: number | undefined;
      service.guardExpiry(of(5)).subscribe((value) => (received = value));

      expect(received).toBe(5);
      expect(navigate).not.toHaveBeenCalled();
    });

    it('sends the visitor back to sign-in with a message when the session has gone', () => {
      let failure: unknown;
      service.guardExpiry(throwError(() => unauthorized)).subscribe({ error: (error: unknown) => (failure = error) });

      expect(failure).toBe(unauthorized);
      expect(service.signedIn()).toBe(false);
      expect(navigate).toHaveBeenCalledWith(ADMIN_SIGN_IN_URL);
      expect(TestBed.inject(FlashMessageService).message()).toBe(ADMIN_SESSION_EXPIRED_MESSAGE);
    });

    it('leaves other failures alone', () => {
      const other = new ApiRequestError(404, [{ code: ERROR_CODES.roomNotFound, message: 'Gone.' }]);
      let failure: unknown;
      service.guardExpiry(throwError(() => other)).subscribe({ error: (error: unknown) => (failure = error) });

      expect(failure).toBe(other);
      expect(navigate).not.toHaveBeenCalled();
      expect(TestBed.inject(FlashMessageService).message()).toBeNull();
    });

    it('does not treat a wrong sign-in as an expired session', () => {
      const wrong = new ApiRequestError(401, [{ code: ERROR_CODES.adminInvalidCredentials, message: 'Wrong.' }]);
      service.guardExpiry(throwError(() => wrong)).subscribe({ error: () => undefined });

      expect(navigate).not.toHaveBeenCalled();
    });
  });
});
