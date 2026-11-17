import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { adminGuard, signedOutGuard } from './admin.guard';
import { AdminSessionService } from './admin-session.service';

describe('admin guards', () => {
  let signedIn = false;
  let ensureLoaded: ReturnType<typeof vi.fn>;
  const route = {} as ActivatedRouteSnapshot;
  const state = {} as RouterStateSnapshot;

  beforeEach(() => {
    signedIn = false;
    ensureLoaded = vi.fn().mockResolvedValue({});
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AdminSessionService, useValue: { ensureLoaded, signedIn: () => signedIn } }],
    });
  });

  const run = (guard: typeof adminGuard) => TestBed.runInInjectionContext(() => guard(route, state));
  const urlOf = (tree: unknown) => TestBed.inject(Router).serializeUrl(tree as UrlTree);

  it('lets a signed-in Operator into the management page', async () => {
    signedIn = true;

    expect(await run(adminGuard)).toBe(true);
  });

  it('sends a visitor without a session to the sign-in page', async () => {
    expect(urlOf(await run(adminGuard))).toBe('/admin/sign-in');
  });

  it('treats an unreachable server as signed out', async () => {
    ensureLoaded.mockRejectedValue(new Error('offline'));

    expect(urlOf(await run(adminGuard))).toBe('/admin/sign-in');
  });

  it('lets a visitor without a session see the sign-in page', async () => {
    expect(await run(signedOutGuard)).toBe(true);
  });

  it('sends a signed-in Operator past the sign-in page', async () => {
    signedIn = true;

    expect(urlOf(await run(signedOutGuard))).toBe('/admin');
  });
});
