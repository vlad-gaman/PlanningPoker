import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from './app.routes';
import { AdminSessionService } from './core/admin-session.service';
import { AdminHome } from './features/admin/admin-home';
import { AdminSignIn } from './features/admin/admin-sign-in';
import { Credits } from './features/credits/credits';
import { Home } from './features/home/home';
import { Room } from './features/room/room';

const session = { ensureLoaded: vi.fn(), signedIn: vi.fn(), username: () => 'operator' };

describe('routes', () => {
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    session.ensureLoaded.mockReset().mockResolvedValue({});
    session.signedIn.mockReset().mockReturnValue(false);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter(routes, withComponentInputBinding()),
        { provide: AdminSessionService, useValue: session },
      ],
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

  describe('admin', () => {
    it('is a lazily loaded route, so it is not part of the public bundle', () => {
      const admin = routes.find((route) => route.path === 'admin');

      expect(admin?.loadChildren).toBeDefined();
      expect(admin?.component).toBeUndefined();
    });

    it('sends a visitor without a session from /admin to the sign-in page', async () => {
      const page = await harness.navigateByUrl('/admin');

      expect(TestBed.inject(Router).url).toBe('/admin/sign-in');
      expect(page).toBeInstanceOf(AdminSignIn);
    });

    it('shows the management page to a signed-in Operator', async () => {
      session.signedIn.mockReturnValue(true);

      expect(await harness.navigateByUrl('/admin')).toBeInstanceOf(AdminHome);
    });

    it('sends a signed-in Operator past the sign-in page', async () => {
      session.signedIn.mockReturnValue(true);

      expect(await harness.navigateByUrl('/admin/sign-in')).toBeInstanceOf(AdminHome);
      expect(TestBed.inject(Router).url).toBe('/admin');
    });

    it('sends unknown admin paths to the management page, and from there to sign-in', async () => {
      await harness.navigateByUrl('/admin/whatever');

      expect(TestBed.inject(Router).url).toBe('/admin/sign-in');
    });
  });
});
