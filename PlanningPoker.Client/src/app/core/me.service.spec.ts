import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Person } from '../models/person';
import { MeService } from './me.service';

describe('MeService', () => {
  let service: MeService;
  let http: HttpTestingController;
  const person: Person = { guid: 'g', name: 'Ann', personType: 'dev' };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(MeService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('gets the current person from /api/me', () => {
    let result: Person | undefined;
    service.get().subscribe((value) => (result = value));

    http.expectOne({ method: 'GET', url: '/api/me' }).flush({ data: person, errors: null });

    expect(result).toEqual(person);
  });

  it('puts the update to /api/me', () => {
    let result: Person | undefined;
    service.update({ name: 'Ann', personType: 'dev' }).subscribe((value) => (result = value));

    const request = http.expectOne({ method: 'PUT', url: '/api/me' });
    expect(request.request.body).toEqual({ name: 'Ann', personType: 'dev' });
    request.flush({ data: person, errors: null });

    expect(result).toEqual(person);
  });
});
