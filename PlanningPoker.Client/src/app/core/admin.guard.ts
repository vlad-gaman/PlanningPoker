import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AdminSessionService } from './admin-session.service';

async function isSignedIn(session: AdminSessionService): Promise<boolean> {
  try {
    await session.ensureLoaded();
  } catch {
    // The server cannot be reached: treat the visitor as signed out; the next visit asks again.
  }
  return session.signedIn();
}

/** The management page itself: without a session the visitor is sent to sign in. */
export const adminGuard: CanActivateFn = async () => {
  const session = inject(AdminSessionService);
  const router = inject(Router);
  return (await isSignedIn(session)) ? true : router.createUrlTree(['/admin', 'sign-in']);
};

/** The sign-in page: someone who is already signed in goes straight to the management page. */
export const signedOutGuard: CanActivateFn = async () => {
  const session = inject(AdminSessionService);
  const router = inject(Router);
  return (await isSignedIn(session)) ? router.createUrlTree(['/admin']) : true;
};
