import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from './app.routes';
import { Credits } from './features/credits/credits';
import { Home } from './features/home/home';
import { Room } from './features/room/room';

describe('routes', () => {
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes, withComponentInputBinding())],
    });
    harness = await RouterTestingHarness.create();
  });

  it('renders Home at the root', async () => {
    expect(await harness.navigateByUrl('/')).toBeInstanceOf(Home);
  });

  it('renders Room with the id bound from the URL', async () => {
    const room = await harness.navigateByUrl('/room/abc');

    expect(room).toBeInstanceOf(Room);
    expect((room as Room).id()).toBe('abc');
  });

  it('redirects capitalised Room paths to lowercase', async () => {
    await harness.navigateByUrl('/Room/abc');

    expect(TestBed.inject(Router).url).toBe('/room/abc');
  });

  it('renders Credits and redirects the capitalised path', async () => {
    expect(await harness.navigateByUrl('/credits')).toBeInstanceOf(Credits);
    await harness.navigateByUrl('/Credits');
    expect(TestBed.inject(Router).url).toBe('/credits');
  });
});
