import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApiClient } from './api-client';
import { ApiRequestError } from './api-request-error';

describe('ApiClient', () => {
  let client: ApiClient;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    client = TestBed.inject(ApiClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('unwraps data from a successful ApiResponse', () => {
    let result: unknown;
    client.get<{ a: number }>('/api/x').subscribe((value) => (result = value));

    http.expectOne('/api/x').flush({ data: { a: 1 }, errors: null });

    expect(result).toEqual({ a: 1 });
  });

  it('sends the body on put', () => {
    client.put('/api/x', { n: 1 }).subscribe();

    const request = http.expectOne('/api/x');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ n: 1 });
    request.flush({ data: null, errors: null });
  });

  it('sends the body on post', () => {
    client.post('/api/x', { n: 2 }).subscribe();

    const request = http.expectOne('/api/x');
    expect(request.request.method).toBe('POST');
    request.flush({ data: null, errors: null });
  });

  it('turns an error ApiResponse into an ApiRequestError', () => {
    let error: unknown;
    client.get('/api/x').subscribe({ error: (e: unknown) => (error = e) });

    http.expectOne('/api/x').flush(
      { data: null, errors: [{ code: 'no_session', message: 'No session.' }] },
      { status: 401, statusText: 'Unauthorized' },
    );

    expect(error).toBeInstanceOf(ApiRequestError);
    const requestError = error as ApiRequestError;
    expect(requestError.status).toBe(401);
    expect(requestError.message).toBe('No session.');
    expect(requestError.hasCode('no_session')).toBe(true);
    expect(requestError.hasCode('other')).toBe(false);
  });

  it('produces an ApiRequestError with no errors when the body is not an ApiResponse', () => {
    let error: unknown;
    client.get('/api/x').subscribe({ error: (e: unknown) => (error = e) });

    http.expectOne('/api/x').flush('boom', { status: 502, statusText: 'Bad Gateway' });

    const requestError = error as ApiRequestError;
    expect(requestError.status).toBe(502);
    expect(requestError.errors).toEqual([]);
    expect(requestError.message).toContain('502');
  });
});
