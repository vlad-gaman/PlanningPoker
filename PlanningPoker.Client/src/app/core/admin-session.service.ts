import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, firstValueFrom, throwError } from 'rxjs';
import { AdminSession, SignInStep } from '../models/admin';
import { ERROR_CODES } from '../models/api';
import { ApiClient } from './api-client';
import { ApiRequestError } from './api-request-error';
import { FlashMessageService } from './flash-message.service';

export const ADMIN_SIGN_IN_URL = '/admin/sign-in';
export const ADMIN_SESSION_EXPIRED_MESSAGE = 'Your session expired. Sign in again.';

const SIGNED_OUT: AdminSession = { authenticated: false };

/**
 * The Operator's session as the management page sees it. The server owns the session (a cookie the script cannot read), so
 * this only remembers what the server last said; the anti-forgery header is added by Angular's XSRF support from the XSRF-TOKEN cookie.
 */
@Injectable({ providedIn: 'root' })
export class AdminSessionService {
  private readonly api = inject(ApiClient);
  private readonly router = inject(Router);
  private readonly flash = inject(FlashMessageService);
  private readonly state = signal<AdminSession | null>(null);

  /** What the server last said; null until the first check. */
  readonly session = this.state.asReadonly();
  readonly signedIn = computed(() => this.state()?.authenticated === true);
  readonly username = computed(() => this.state()?.username ?? null);

  /** The remembered session, asking the server only the first time. */
  async ensureLoaded(): Promise<AdminSession> {
    return this.state() ?? this.load();
  }

  async load(): Promise<AdminSession> {
    const session = await firstValueFrom(this.api.get<AdminSession>('/api/admin/session'));
    this.state.set(session);
    return session;
  }

  /** The password step. With an authenticator set up the server asks for a code instead and sends a challenge to send back with it. */
  async signIn(username: string, password: string): Promise<SignInStep> {
    try {
      this.state.set(await firstValueFrom(this.api.post<AdminSession>('/api/admin/login', { username, password })));
      return { kind: 'signedIn' };
    } catch (error) {
      const challenge = challengeOf(error);
      if (challenge) {
        return { kind: 'code', challenge };
      }
      throw error;
    }
  }

  /** The code step. */
  async completeCode(challenge: string, code: string): Promise<void> {
    this.state.set(await firstValueFrom(this.api.post<AdminSession>('/api/admin/login/totp', { challenge, code })));
  }

  /** Ends the session. The page forgets it even when the server cannot be reached, and goes to the sign-in page. */
  async signOut(): Promise<void> {
    try {
      // The server only accepts JSON bodies on POST, so an empty object stands in for no body.
      await firstValueFrom(this.api.post<AdminSession>('/api/admin/logout', {}));
    } finally {
      this.state.set(SIGNED_OUT);
      await this.router.navigateByUrl(ADMIN_SIGN_IN_URL);
    }
  }

  /** The server no longer knows the session (it timed out, or the server restarted): back to sign-in with a reason. */
  expire(): void {
    this.state.set(SIGNED_OUT);
    this.flash.show(ADMIN_SESSION_EXPIRED_MESSAGE);
    void this.router.navigateByUrl(ADMIN_SIGN_IN_URL);
  }

  /** Pipe every call to an admin endpoint through this: a 401 means the session is gone, so the page returns to sign-in. */
  guardExpiry<T>(source: Observable<T>): Observable<T> {
    return source.pipe(
      catchError((error: unknown) => {
        if (error instanceof ApiRequestError && error.status === 401 && error.hasCode(ERROR_CODES.unauthorized)) {
          this.expire();
        }
        return throwError(() => error);
      }),
    );
  }
}

function challengeOf(error: unknown): string | null {
  if (!(error instanceof ApiRequestError)) {
    return null;
  }
  return error.errors.find((item) => item.code === ERROR_CODES.adminTotpRequired)?.metadata?.['challenge'] ?? null;
}
